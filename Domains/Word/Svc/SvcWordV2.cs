namespace Ngaq.Local.Domains.Word.Svc;

using Ngaq.Core.Infra;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Shared.StudyPlan.Models.PreFilter;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Learn_;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Word.Dao;
using Tsinswreng.CsErr;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;

public partial class SvcWordV2(
	ISqlCmdMkr SqlCmdMkr
	,DaoWordV2 DaoWordV2
	,IRepo<PoWord, IdWord> RepoWord
	,IRepo<PoWordProp, IdWordProp> RepoProp
	,IRepo<PoWordLearn, IdWordLearn> RepoLearn
):ISvcWordV2
{
	ISqlCmdMkr SqlCmdMkr = SqlCmdMkr;
	DaoWordV2 DaoWordV2 = DaoWordV2;
	IRepo<PoWord, IdWord> RepoWord = RepoWord;
	IRepo<PoWordProp, IdWordProp> RepoProp = RepoProp;
	IRepo<PoWordLearn, IdWordLearn> RepoLearn = RepoLearn;

	public IAsyncEnumerable<JnWord> GetWordsToLearn(
		IDbUserCtx Ctx, CT Ct
	){
		return GetWordsToLearn(Ctx, null, Ct);
	}

	public IAsyncEnumerable<JnWord> GetWordsToLearn(
		IDbUserCtx Ctx, PreFilter? Prefilter, CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		_ = Prefilter; // TODO: 待學習方案模塊完成後接入預篩選
		return DaoWordV2.GetWordsByOwner(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Ct);
	}

	public async Task<nil> BatAddNewLearnRecord(
		IDbUserCtx Ctx
		,IAsyncEnumerable<PoWordLearn> PoWordLearnAsyE, CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await using var Batch = new BatchCollector<PoWordLearn, nil>(async(Learns, Ct)=>{
				var DistinctIds = DistinctWordIds(Learns.Select(x=>x.WordId));
				if(DistinctIds.Count == 0){
					return NIL;
				}
				await EnsureOwner(DbCtx, Ctx.UserCtx.UserId, DistinctIds, Ct);
				await RepoLearn.BatAdd(DbCtx, ToAsyE(Learns), Ct);
				await DaoWordV2.BatAltWordAfterUpd(DbCtx, ToAsyE(DistinctIds), Ct);
				return NIL;
			});

			await foreach(var One in PoWordLearnAsyE){
				await Batch.Add(One, Ct);
			}
			await Batch.End(Ct);
			return NIL;
		});
	}
	
	public Task<nil> BatAddNewWordToLearn(
		IDbUserCtx Ctx,
		IAsyncEnumerable<JnWord> Words, CT Ct
	){
		return SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await using var Batch = new BatchCollector<JnWord, nil>(async(WordBatch, Ct)=>{
				if(WordBatch.Count == 0){
					return NIL;
				}

				// 1) 先把同批次內同(Head,Lang)的輸入合併，避免同批次重複插入
				var Merged = new List<MergedWord>(WordBatch.Count);
				var KeyToIndex = new Dictionary<Head_Lang, int>();
				foreach(var Src in WordBatch){
					var Key = new Head_Lang(Src.Head, Src.Lang);
					if(!KeyToIndex.TryGetValue(Key, out var i)){
						var Neo = new JnWord{
							Word = new PoWord{
								Id = Src.Word.Id,
								Owner = Ctx.UserCtx.UserId,
								Head = Src.Head,
								Lang = Src.Lang,
								StoredAt = Src.StoredAt,
								BizCreatedAt = Src.BizCreatedAt,
								BizUpdatedAt = Src.BizUpdatedAt,
							},
						};
						Neo.Props = [];
						Neo.Learns = [];
						Merged.Add(new MergedWord(Key, Neo));
						i = Merged.Count - 1;
						KeyToIndex[Key] = i;
					}
					foreach(var P in Src.Props){
						Merged[i].Word.Props.Add(P);
					}
				}

				// 2) 批量查詢這批(Head,Lang)是否已存在
				var ExistingIds = DaoWordV2.BatGetIdByOwnerHeadLang(
					DbCtx,
					Ctx.UserCtx.UserId,
					ToAsyE(Merged.Select(x=>x.Key)),
					Ct
				);
				var Pos = 0;
				await foreach(var Id in ExistingIds){
					if(Pos >= Merged.Count){
						break;
					}
					Merged[Pos].ExistingId = Id;
					Pos++;
				}

				// 3) 準備需要新增的根詞、props、learns 與需要touch的詞id
				var NeoAggs = new List<JnWord>();
				var NeoProps = new List<PoWordProp>();
				var NeoLearns = new List<PoWordLearn>();
				var TouchIds = new List<IdWord>();

				var ExistingIdList = Merged
					.Where(x=>x.ExistingId is not null)
					.Select(x=>x.ExistingId!.Value)
					.ToList();
				var ExistingById = new Dictionary<IdWord, JnWord>();
				if(ExistingIdList.Count > 0){
					var ExistingWords = DaoWordV2.BatGetJnWordByIdWithDel(DbCtx, ToAsyE(ExistingIdList), Ct);
					var i = 0;
					await foreach(var Exi in ExistingWords){
						if(i >= ExistingIdList.Count){
							break;
						}
						var Id = ExistingIdList[i];
						if(Exi is not null){
							ExistingById[Id] = Exi;
						}
						i++;
					}
				}

				foreach(var One in Merged){
					if(One.ExistingId is null){
						// 新詞：直接新增聚合；Add 記錄數量 = 全部新詞中的 description 數
						One.Word.Owner = Ctx.UserCtx.UserId;
						One.Word.EnsureForeignId();
						NeoAggs.Add(One.Word);
						AppendAddLearns(NeoLearns, One.Word.Id, One.Word.Props);
						continue;
					}

					var ExiId = One.ExistingId.Value;
					if(!ExistingById.TryGetValue(ExiId, out var ExiWord)){
						throw ItemsErr.Word.__And__IsNotSameUserWord.ToErr(ExiId, Ctx.UserCtx.UserId);
					}

					// 舊詞：僅插入真正新來的props；Add 記錄數量 = 新增進庫的 description props 數
					var Known = new HashSet<PropFingerprint>();
					foreach(var P in ExiWord.Props){
						Known.Add(MkPropFingerprint(P));
					}

					var AddedAny = false;
					foreach(var P in One.Word.Props){
						var Fp = MkPropFingerprint(P);
						if(!Known.Add(Fp)){
							continue;
						}
						var NeoP = new PoWordProp{
							Id = P.Id,
							WordId = ExiId,
							KType = P.KType,
							KStr = P.KStr,
							KI64 = P.KI64,
							VType = P.VType,
							VStr = P.VStr,
							VI64 = P.VI64,
							VF64 = P.VF64,
							VBinary = P.VBinary,
							BizCreatedAt = P.BizCreatedAt,
							BizUpdatedAt = P.BizUpdatedAt,
						};
						NeoProps.Add(NeoP);
						if(IsDescription(NeoP)){
							NeoLearns.Add(new PoWordLearn{
								WordId = ExiId,
								LearnResult = ELearn.Add,
								BizCreatedAt = NeoP.BizCreatedAt,
							});
						}
						AddedAny = true;
					}

					if(AddedAny){
						TouchIds.Add(ExiId);
					}
				}

				if(NeoAggs.Count > 0){
					await RepoWord.BatAddAgg<JnWord>(DbCtx, ToAsyE(NeoAggs), Ct);
				}
				if(NeoProps.Count > 0){
					await RepoProp.BatAdd(DbCtx, ToAsyE(NeoProps), Ct);
				}
				if(NeoLearns.Count > 0){
					await RepoLearn.BatAdd(DbCtx, ToAsyE(NeoLearns), Ct);
				}
				var DistinctTouched = DistinctWordIds(TouchIds);
				if(DistinctTouched.Count > 0){
					await DaoWordV2.BatAltWordAfterUpd(DbCtx, ToAsyE(DistinctTouched), Ct);
				}

				return NIL;
			});

			await foreach(var One in Words){
				await Batch.Add(One, Ct);
			}
			await Batch.End(Ct);
			return NIL;
		});
	}
	public async Task<nil> SoftDelJnWordInId(
		IDbUserCtx Ctx,
		IAsyncEnumerable<IdWord> Ids, CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await using var Batch = new BatchCollector<IdWord, nil>(async(IdBatch, Ct)=>{
				var DistinctIds = DistinctWordIds(IdBatch);
				if(DistinctIds.Count == 0){
					return NIL;
				}
				await EnsureOwner(DbCtx, Ctx.UserCtx.UserId, DistinctIds, Ct);
				await RepoWord.SoftDelAggInId<JnWord>(DbCtx, ToAsyE(DistinctIds), Ct);
				return NIL;
			});

			await foreach(var One in Ids){
				await Batch.Add(One, Ct);
			}
			await Batch.End(Ct);
			return NIL;
		});
	}

	async Task<nil> EnsureOwner(
		IDbFnCtx Ctx
		,IdUser UserId
		,IReadOnlyList<IdWord> Ids
		,CT Ct
	){
		var Got = DaoWordV2.BatGetPoWordByIdWithDel(Ctx, ToAsyE(Ids), Ct);
		var i = 0;
		await foreach(var Po in Got){
			if(i >= Ids.Count){
				break;
			}
			var Id = Ids[i];
			if(Po is null || Po.Owner != UserId){
				throw ItemsErr.Word.__And__IsNotSameUserWord.ToErr(Id, UserId);
			}
			i++;
		}

		if(i != Ids.Count){
			throw ItemsErr.Word.__And__IsNotSameUserWord.ToErr(UserId);
		}
		return NIL;
	}

	static List<IdWord> DistinctWordIds(IEnumerable<IdWord> Ids){
		var Set = new HashSet<IdWord>();
		var R = new List<IdWord>();
		foreach(var Id in Ids){
			if(!Set.Add(Id)){
				continue;
			}
			R.Add(Id);
		}
		return R;
	}

	static async IAsyncEnumerable<T> ToAsyE<T>(IEnumerable<T> Src){
		foreach(var One in Src){
			yield return One;
		}
		await Task.CompletedTask;
	}

	static bool IsDescription(PoWordProp Prop){
		return Prop.KStr == KeysProp.Inst.description;
	}

	static void AppendAddLearns(
		IList<PoWordLearn> Out
		,IdWord WordId
		,IEnumerable<PoWordProp> Props
	){
		foreach(var P in Props){
			if(!IsDescription(P)){
				continue;
			}
			Out.Add(new PoWordLearn{
				WordId = WordId,
				LearnResult = ELearn.Add,
				BizCreatedAt = P.BizCreatedAt,
			});
		}
	}

	static PropFingerprint MkPropFingerprint(PoWordProp P){
		var Binary = P.VBinary is null ? null : Convert.ToBase64String(P.VBinary);
		return new PropFingerprint(
			P.KType,
			P.KStr,
			P.KI64,
			P.VType,
			P.VStr,
			P.VI64,
			P.VF64,
			Binary
		);
	}

	sealed class MergedWord(Head_Lang Key, JnWord Word){
		public Head_Lang Key { get; } = Key;
		public JnWord Word { get; } = Word;
		public IdWord? ExistingId { get; set; }
	}

	record struct PropFingerprint(
		EKvType KType,
		str? KStr,
		i64 KI64,
		EKvType VType,
		str? VStr,
		i64 VI64,
		f64 VF64,
		str? VBinaryBase64
	);
}
