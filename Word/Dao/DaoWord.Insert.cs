namespace Ngaq.Local.Word.Dao;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Local.Db;
using Ngaq.Core.Word.Models.Po.Learn;
using Ngaq.Core.Word.Models;
using Tsinswreng.CsTools;
using Ngaq.Core.Word.Models.Po.Word;
using Ngaq.Core.Word.Models.Po.Kv;
public partial class DaoSqlWord{


	public async Task<Func<
		IEnumerable<JnWord>
		,CT
		,Task<nil>
	>> FnInsertJnWords(
		IDbFnCtx? Ctx
		,CT ct
	) {
		var InsertPoWords = await RepoWord.FnInsertMany(Ctx, ct);
		var InsertPoKvs = await RepoKv.FnInsertMany(Ctx, ct);
		var InsertPoLearns = await RepoLearn.FnInsertMany(Ctx, ct);

		var Fn = async(
			IEnumerable<JnWord> JnWords
			,CT ct
		)=>{
			u64 BatchSize = 0xfff;
			await using var PoWords = new BatchListAsy<PoWord, nil>(async(list, ct)=>{
				await InsertPoWords(list,ct);
				return NIL;
			}, BatchSize);
			await using var PoKvs = new BatchListAsy<PoWordProp, nil>(async(e, ct)=>{
				await InsertPoKvs(e, ct);
				return NIL;
			}, BatchSize);
			await using var PoLearns = new BatchListAsy<PoWordLearn, nil>(async(e, ct)=>{
				await InsertPoLearns(e, ct);
				return NIL;
			}, BatchSize);
			foreach (var JWord in JnWords) {
				JWord.EnsureForeignId();
				await PoWords.Add(JWord.Word, ct);
				foreach (var Prop in JWord.Props) {
					await PoKvs.Add(Prop, ct);
				}
				foreach (var Learn in JWord.Learns) {
					await PoLearns.Add(Learn, ct);
				}
			}
			await PoWords.End(ct);
			await PoKvs.End(ct);
			await PoLearns.End(ct);
			return NIL;
		};
		return Fn;
	}

	public async Task<Func<
		IdWord? //斯批PoWordProp 屬于哪個詞
		,IEnumerable<PoWordProp>
		,CT
		,Task<nil>
	>> FnInsertPoKvs(
		IDbFnCtx? Ctx
		,CT Ct
	){
		var UpdUpd = await FnTriggerOnRootAfterUpd(Ctx,Ct);
		var InsertMany = await RepoKv.FnInsertMany(Ctx, Ct);
		return async(WordId,PoKvs,Ct)=>{
			await InsertMany(PoKvs, Ct);
			if(WordId is not null){
				await UpdUpd(WordId.Value, Ct);
			}
			return NIL;
		};
	}

	public async Task<Func<
		IdWord?
		,IEnumerable<PoWordLearn>
		,CT
		,Task<nil>
	>> FnInsertPoLearns(
		IDbFnCtx? Ctx
		,CT Ct
	){
		var UpdUpd = await FnTriggerOnRootAfterUpd(Ctx, Ct);
		var InsertMany = await RepoLearn.FnInsertMany(Ctx, Ct);
		return async(WordId, PoLearns, Ct)=>{
			await InsertMany(PoLearns, Ct);
			if(WordId is not null){
				await UpdUpd(WordId.Value, Ct);
			}
			return NIL;
		};
	}
}
