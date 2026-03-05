namespace Ngaq.Local.Word.Dao;

using System.Diagnostics;
using System.Threading.Tasks;
using Ngaq.Core.Infra;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
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
		/* 
		var Head = HeadLangs.Select(x=>x.Head);
		var Lang = HeadLangs.Select(x=>x.Lang);
		var UserId = User.UserId;
		var Sql = T.SqlSplicer().Select(x=>x.Id).From().Where1()
		.AndEq(x=>x.Owner, y=>y.One(UserId))
		.AndEq(x=>x.Head, y=>y.Many(Head))
		.AndEq(x=>x.Lang, y=>y.Many(Lang))
		 */

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
//在這裏給z加個 ExeReader()方法、返回IResultReader對象 不用再傳參數
//也允許不用z.ExeReader()、直接和以前一樣z.SqlCmd.Args(...)  兼容舊寫法
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
