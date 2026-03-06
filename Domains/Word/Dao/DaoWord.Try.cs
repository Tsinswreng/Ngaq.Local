namespace Ngaq.Local.Word.Dao;

using System.Diagnostics;
using System.Threading.Tasks;
using Ngaq.Core.Infra;
using Ngaq.Core.Infra.IF;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Word.Models.Po.Word;
using Ngaq.Local.Db.TswG;
using Tsinswreng.CsCore;
using Tsinswreng.CsPage;
using Tsinswreng.CsSqlHelper;
using Tsinswreng.CsTools;
using IStr_Any = System.Collections.Generic.IDictionary<str, obj?>;
using Str_Any = System.Collections.Generic.Dictionary<str, obj?>;

public partial class DaoWord{

	public async Task<IAsyncEnumerable<IdWord?>> BatSlctIdByOwnerHeadLangWithDel(
		IDbFnCtx Ctx, IUserCtx User, IEnumerable<Head_Lang> HeadLangs, CT Ct
	){
		var Sql = T.SqlSplicer().Select(x=>x.Id).From().Where1()
		.AndEq(x=>x.Owner, out var POwner)
		.AndEq(x=>x.Head, out var PHead)
		.AndEq(x=>x.Lang, out var PLang)
		;
		
		await using var batch = SqlCmdMkr.AutoBatch<Head_Lang, IAsyncEnumerable<IdWord?>>(
			Ctx, Sql,
			async(z, HeadLangs, Ct)=>{
				var Head = HeadLangs.Select(x=>x.Head);
				var Lang = HeadLangs.Select(x=>x.Lang);
				var UserId = User.UserId;
				var Args = ArgDict.Mk(T)
				.AddT(POwner, UserId)
				.AddManyT(PHead, Head)
				.AddManyT(PLang, Lang);
				var GotDicts = z.SqlCmd.Args(Args).AsyE1d(Ct).OrEmpty();
				return GotDicts.Select(x=>{//TODO 當此組 (Head,Lang)查不到數據旹 會返null否
					if(x is null){
						return null;
					}
					var ans = x[T.Memb(x=>x.Id)];
					return (IdWord?)IdWord.FromByteArr((u8[])ans!);
				});
			}
		);
		var R = batch.AllFlat(HeadLangs, Ct);
		return R;
	}

	/// Demonstrate new style: define sql and arg binding in one chain.
	public async Task<IAsyncEnumerable<IdWord?>> BatSlctIdByOwnerHeadLangWithDel_New(
		IDbFnCtx Ctx, IUserCtx User, IEnumerable<Head_Lang> HeadLangs, CT Ct
	){
		var Heads = HeadLangs.Select(x=>x.Head);
		var Lang = HeadLangs.Select(x=>x.Lang);
		//原來的寫法
		var Sql = T.SqlSplicer().Select(x=>x.Id).From().Where1()
		.AndEq(x=>x.Owner, y=>y.One(User.UserId))
		.AndEq(x=>x.Head, y=>y.Many(Heads))
		.AndEq(x=>x.Lang, y=>y.Many(Lang))
		;

		var GotDicts = SqlCmdMkr.RunSql(Ctx, Sql, Ct);
		return GotDicts.Select(x=>{
			if(x is null){
				return null;
			}
			var ans = x[T.Memb(x=>x.Id)];
			return (IdWord?)IdWord.FromByteArr((u8[])ans!);
		});
	}
}
