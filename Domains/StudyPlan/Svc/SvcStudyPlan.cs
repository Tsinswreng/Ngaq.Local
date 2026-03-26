using Ngaq.Core.Frontend.Kv;
using Ngaq.Core.Infra.IF;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.Kv.Svc;
using Ngaq.Core.Shared.StudyPlan.Models.Req;
using Ngaq.Core.Shared.StudyPlan.Models.Po.PreFilter;
using Ngaq.Core.Shared.StudyPlan.Models.Po.StudyPlan;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightArg;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightCalculator;
using Ngaq.Core.Shared.StudyPlan.Models.PreFilter;
using Ngaq.Core.Shared.StudyPlan.Svc;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Tools.Json;
using Ngaq.Local.Domains.StudyPlan.Dao;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;
using Ngaq.Core.Infra;
using Ngaq.Core.Word.Svc;
using Ngaq.Core.Shared.StudyPlan.Models;
using System.Text;
using System.Text.Json;
using Ngaq.Core.Tools;
using Ngaq.Core.Sys.Models;

namespace Ngaq.Local.Domains.StudyPlan.Svc;

public partial class SvcStudyPlan:ISvcStudyPlan{
	ISvcKv SvcKv;
	DaoStudyPlan DaoStudyPlan;
	ISqlCmdMkr SqlCmdMkr;
	IRepo<PoStudyPlan, IdStudyPlan> RepoStudyPlan;
	IRepo<PoWeightArg, IdWeightArg> RepoWeightArg;
	IRepo<PoWeightCalculator, IdWeightCalculator> RepoWeightCalculator;
	IRepo<PoPreFilter, IdPreFilter> RepoPreFilter;
	IRepo<PoKv, IdKv> RepoKv;
	IJsonSerializer JsonS;
	BoStudyPlan? CurBoStudyPlanCache = null;
	public SvcStudyPlan(
		ISvcKv SvcKv
		,DaoStudyPlan DaoStudyPlan
		,ISqlCmdMkr SqlCmdMkr
		,IRepo<PoStudyPlan, IdStudyPlan> RepoStudyPlan
		,IRepo<PoWeightArg, IdWeightArg> RepoWeightArg
		,IRepo<PoWeightCalculator, IdWeightCalculator> RepoWeightCalculator
		,IRepo<PoPreFilter, IdPreFilter> RepoPreFilter
		,IRepo<PoKv, IdKv> RepoKv
		,IJsonSerializer JsonSerializer
	){
		this.SvcKv = SvcKv;
		this.DaoStudyPlan = DaoStudyPlan;
		this. SqlCmdMkr = SqlCmdMkr;
		this.RepoStudyPlan = RepoStudyPlan;
		this.RepoWeightArg = RepoWeightArg;
		this.RepoWeightCalculator = RepoWeightCalculator;
		this.RepoPreFilter = RepoPreFilter;
		this.RepoKv = RepoKv;
		this.JsonS = JsonSerializer;
	}

	public async Task<nil> SetCurStudyPlanId(
		IDbUserCtx Ctx, IdStudyPlan StudyPlanId, CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(ctx)=>{
			var key = KeysClientKv.CurStudyPlanId+"";
			var owner = Ctx.UserCtx.UserId;
			var oldKv = await SvcKv.BatGetByOwnerEtKStr(
				ctx
				,ToolAsyE.ToAsyE([(owner, key)])
				,Ct
			).FirstOrDefaultAsync(Ct);

			if(oldKv is null){
				var kv = new PoKv{
					Owner = owner,
				}.SetStrStr(key, StudyPlanId+"");
				await RepoKv.BatAdd(ctx, ToolAsyE.ToAsyE([kv]), Ct);
				return NIL;
			}

			oldKv.Owner = owner;
			oldKv.SetStrStr(key, StudyPlanId+"");
			await RepoKv.BatUpdById(ctx, ToolAsyE.ToAsyE([oldKv]), Ct);
			return NIL;
		});
	}

	public IAsyncEnumerable<T> EnsureOwner<T>(
		IdUser UserId
		,IAsyncEnumerable<T> Pos
	)
		where T:I_Owner
	{
		Pos = Pos.Select(x=>{
			x.Owner = UserId;
			return x;
		});
		return Pos;
	}
	
	public async Task<nil> BatAddPreFilter(
		IDbUserCtx Ctx
		, IAsyncEnumerable<PoPreFilter> Pos
		,CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(ctx)=>{
			Pos = EnsureOwner(Ctx.UserCtx.UserId, Pos);
			await RepoPreFilter.BatAdd(ctx, Pos, Ct);
			return NIL;
		});
	}
	
	public async Task<nil> BatAddWeightArg(
		IDbUserCtx Ctx
		, IAsyncEnumerable<PoWeightArg> Pos
		,CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(ctx)=>{
			Pos = EnsureOwner(Ctx.UserCtx.UserId, Pos);
			await RepoWeightArg.BatAdd(ctx, Pos, Ct);
			return NIL;
		});
	}
	
	public async Task<nil> BatAddWeightCalculator(
		IDbUserCtx Ctx
		, IAsyncEnumerable<PoWeightCalculator> Pos
		,CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(ctx)=>{
			Pos = EnsureOwner(Ctx.UserCtx.UserId, Pos);
			await RepoWeightCalculator.BatAdd(ctx, Pos, Ct);
			return NIL;
		});
	}

	public async Task<IPageAsyE<PoStudyPlan>> PageStudyPlan(
		IDbUserCtx Ctx
		,ReqPageStudyPlan Req
		,CT Ct
	){
		Ctx.DbFnCtx??=new DbFnCtx();
		return await DaoStudyPlan.PageStudyPlan(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Req, Ct);
	}

	public async Task<IPageAsyE<PoPreFilter>> PagePreFilter(
		IDbUserCtx Ctx
		,ReqPagePreFilter Req
		,CT Ct
	){
		Ctx.DbFnCtx??=new DbFnCtx();
		return await DaoStudyPlan.PagePreFilter(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Req, Ct);
	}

	public async Task<IPageAsyE<PoWeightArg>> PageWeightArg(
		IDbUserCtx Ctx
		,ReqPageWeightArg Req
		,CT Ct
	){
		Ctx.DbFnCtx??=new DbFnCtx();
		return await DaoStudyPlan.PageWeightArg(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Req, Ct);
	}

	public async Task<IPageAsyE<PoWeightCalculator>> PageWeightCalculator(
		IDbUserCtx Ctx
		,ReqPageWeightCalculator Req, CT Ct
	){
		Ctx.DbFnCtx??=new DbFnCtx();
		return await DaoStudyPlan.PageWeightCalculator(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Req, Ct);
	}
	
	
	public async Task<IdStudyPlan?> GetCurStudyPlanId(IDbUserCtx Ctx, CT Ct){
		Ctx.DbFnCtx ??= new DbFnCtx();
		var kv = await SvcKv.BatGetByOwnerEtKStr(
			Ctx.DbFnCtx
			,ToolAsyE.ToAsyE([(Ctx.UserCtx.UserId, KeysClientKv.CurStudyPlanId+"")])
			,Ct
		).FirstOrDefaultAsync(Ct);
		if(kv is null || string.IsNullOrEmpty(kv.VStr)){
			return null;
		}
		if(IdStudyPlan.TryParse(kv.VStr, out var id)){
			return id;
		}
		return null;
	}

	public async Task<IWeightCalctr?> GetCurWeightCalctr(IDbUserCtx Ctx, CT Ct) {
		var bo = await GetCurBoStudyPlan(Ctx, Ct);
		return bo?.WeightCalctr;
	}

	public async Task<JnStudyPlan?> GetCurJnStudyPlan(IDbUserCtx Ctx, CT Ct) {
		Ctx.DbFnCtx ??= new DbFnCtx();
		var curStudyPlanId = await GetCurStudyPlanId(Ctx, Ct);
		if(curStudyPlanId is not IdStudyPlan studyPlanId || studyPlanId.IsNullOrDefault()){
			return null;
		}

		var jnStudyPlan = await DaoStudyPlan.BatGetStudyPlanById(
			Ctx.DbFnCtx
			,ToolAsyE.ToAsyE([studyPlanId])
			,Ct
		).FirstOrDefaultAsync(Ct);
		if(jnStudyPlan is null){
			return null;
		}

		if(jnStudyPlan.StudyPlan.Owner != Ctx.UserCtx.UserId){
			return null;
		}

		if(jnStudyPlan.PreFilter is null && !jnStudyPlan.StudyPlan.PreFilterId.IsNullOrDefault()){
			jnStudyPlan.PreFilter = await RepoPreFilter.BatGetByIdWithDel(
				Ctx.DbFnCtx
				,ToolAsyE.ToAsyE([jnStudyPlan.StudyPlan.PreFilterId])
				,Ct
			).FirstOrDefaultAsync(Ct);
		}
		if(jnStudyPlan.WeightCalculator is null && !jnStudyPlan.StudyPlan.WeightCalculatorId.IsNullOrDefault()){
			jnStudyPlan.WeightCalculator = await RepoWeightCalculator.BatGetByIdWithDel(
				Ctx.DbFnCtx
				,ToolAsyE.ToAsyE([jnStudyPlan.StudyPlan.WeightCalculatorId])
				,Ct
			).FirstOrDefaultAsync(Ct);
		}
		if(jnStudyPlan.WeightArg is null && !jnStudyPlan.StudyPlan.WeightArgId.IsNullOrDefault()){
			jnStudyPlan.WeightArg = await RepoWeightArg.BatGetByIdWithDel(
				Ctx.DbFnCtx
				,ToolAsyE.ToAsyE([jnStudyPlan.StudyPlan.WeightArgId])
				,Ct
			).FirstOrDefaultAsync(Ct);
		}

		if(jnStudyPlan.PreFilter is { } poPreFilter && poPreFilter.Owner != Ctx.UserCtx.UserId){
			jnStudyPlan.PreFilter = null;
		}
		if(jnStudyPlan.WeightCalculator is { } poWeightCalculator && poWeightCalculator.Owner != Ctx.UserCtx.UserId){
			jnStudyPlan.WeightCalculator = null;
		}
		if(jnStudyPlan.WeightArg is { } poWeightArg && poWeightArg.Owner != Ctx.UserCtx.UserId){
			jnStudyPlan.WeightArg = null;
		}

		return jnStudyPlan;
	}

	public async Task<BoStudyPlan?> GetCurBoStudyPlan(IDbUserCtx Ctx, CT Ct) {
		Ctx.DbFnCtx ??= new DbFnCtx();
		var curStudyPlanId = await GetCurStudyPlanId(Ctx, Ct);
		if(curStudyPlanId is not IdStudyPlan studyPlanId || studyPlanId.IsNullOrDefault()){
			CurBoStudyPlanCache = null;
			return null;
		}

		if(
			CurBoStudyPlanCache?.PoStudyPlan is { } cachedPoStudyPlan
			&& cachedPoStudyPlan.Id == studyPlanId
		){
			var latestPoStudyPlan = await RepoStudyPlan.BatGetByIdWithDel(
				Ctx.DbFnCtx
				,ToolAsyE.ToAsyE([studyPlanId])
				,Ct
			).FirstOrDefaultAsync(Ct);

			if(
				latestPoStudyPlan is not null
				&& latestPoStudyPlan.Owner == Ctx.UserCtx.UserId
				&& latestPoStudyPlan.BizUpdatedAt == cachedPoStudyPlan.BizUpdatedAt
			){
				return CurBoStudyPlanCache;
			}
		}

		var jnStudyPlan = await GetCurJnStudyPlan(Ctx, Ct);
		if(jnStudyPlan is null){
			CurBoStudyPlanCache = null;
			return null;
		}

		var boStudyPlan = new BoStudyPlan{
			PoStudyPlan = jnStudyPlan.StudyPlan,
			PoPreFilter = jnStudyPlan.PreFilter,
			PoWeightArg = jnStudyPlan.WeightArg,
			PoWeightCalculator = jnStudyPlan.WeightCalculator,
		};

		if(
			boStudyPlan.PoPreFilter is { } poPreFilter
			&& poPreFilter.Type == EPreFilterType.Json
			&& poPreFilter.Data is { Length: > 0 }
		){
			var json = Encoding.UTF8.GetString(poPreFilter.Data);
			if(!string.IsNullOrWhiteSpace(json)){
				boStudyPlan.PreFilter = JsonS.Parse<PreFilter>(json);
			}
		}

		if(
			boStudyPlan.PoWeightArg is { } poWeightArg
			&& poWeightArg.Type == EWeightArgType.Json
			&& poWeightArg.Data is { Length: > 0 }
		){
			var json = Encoding.UTF8.GetString(poWeightArg.Data);
			if(!string.IsNullOrWhiteSpace(json)){
				boStudyPlan.WeightArg = ParseJsonObjDict(json);
			}
		}

		boStudyPlan.WeightCalctr = MakeWeightCalctr(boStudyPlan.PoWeightCalculator);
		CurBoStudyPlanCache = boStudyPlan;
		return boStudyPlan;
	}

	IWeightCalctr? MakeWeightCalctr(PoWeightCalculator? PoWeightCalculator){
		if(PoWeightCalculator is null){
			return null;
		}
		if(PoWeightCalculator.Type == EWeightCalculatorType.Js){
			if(PoWeightCalculator.Data is not { Length: > 0 }){
				return null;
			}
			var jsCode = Encoding.UTF8.GetString(PoWeightCalculator.Data);
			if(string.IsNullOrWhiteSpace(jsCode)){
				return null;
			}
			return new JsWeightCalctr(JsonS, jsCode);
		}
		return null;
	}

	static IDictionary<str, obj?> ParseJsonObjDict(str Json){
		using var doc = JsonDocument.Parse(Json);
		if(doc.RootElement.ValueKind != JsonValueKind.Object){
			return new Dictionary<str, obj?>();
		}
		return (Dictionary<str, obj?>)JsonElementToObj(doc.RootElement)!;
	}

	static obj? JsonElementToObj(JsonElement Element){
		switch(Element.ValueKind){
			case JsonValueKind.Object:{
				var dict = new Dictionary<str, obj?>();
				foreach(var p in Element.EnumerateObject()){
					dict[p.Name] = JsonElementToObj(p.Value);
				}
				return dict;
			}
			case JsonValueKind.Array:{
				var list = new List<obj?>();
				foreach(var x in Element.EnumerateArray()){
					list.Add(JsonElementToObj(x));
				}
				return list;
			}
			case JsonValueKind.String:
				return Element.GetString();
			case JsonValueKind.Number:
				if(Element.TryGetInt64(out var i64v)){
					return i64v;
				}
				if(Element.TryGetDouble(out var f64v)){
					return f64v;
				}
				return Element.ToString();
			case JsonValueKind.True:
				return true;
			case JsonValueKind.False:
				return false;
			case JsonValueKind.Null:
				return null;
			default:
				return Element.ToString();
		}
	}


#if false
public async Task<Func<
		IUserCtx, IdStudyPlan
		,CT, Task<BoStudyPlan?>
	>> FnGetStudyPlanById(IDbFnCtx Ctx, CT Ct){
		var fnPreFilter = await RepoPreFilter.FnSlctOneById(Ctx, Ct);
		var fnWeightArg = await RepoWeightArg.FnSlctOneById(Ctx, Ct);
		var fnWeightCalculator = await RepoWeightCalculator.FnSlctOneById(Ctx, Ct);
		var fnStudyPlan = await RepoStudyPlan.FnSlctOneById(Ctx, Ct);

		return async (UserCtx, StudyPlanId, Ct)=>{
			var studyPlan = await fnStudyPlan(StudyPlanId, Ct);
			if(studyPlan == null){
				return null;
			}
			var R = new BoStudyPlan();
			R.PoStudyPlan = studyPlan;

			// Get PreFilter
			if(!studyPlan.PreFilterId.IsNullOrDefault()){
				var poPreFilter = await fnPreFilter(studyPlan.PreFilterId, Ct);
				if(poPreFilter != null && poPreFilter.Data != null){
					var json = Encoding.UTF8.GetString(poPreFilter.Data);
					R.PreFilter = JSON.parse<PreFilter>(json);
				}
				R.PoPreFilter = poPreFilter;
			}

			// Get WeightArg
			if(!studyPlan.WeightArgId.IsNullOrDefault()){
				var poWeightArg = await fnWeightArg(studyPlan.WeightArgId, Ct);
				R.PoWeightArg = poWeightArg;
				if(poWeightArg != null && poWeightArg.Data != null){
					var json = Encoding.UTF8.GetString(poWeightArg.Data);
					var dict = ToolJson.JsonStrToDict(json);
					R.WeightArg = new JsonNode(dict);
				}
			}

			// Get WeightCalculator
			if(!studyPlan.WeightCalculatorId.IsNullOrDefault()){
				R.PoWeightCalculator = await fnWeightCalculator(studyPlan.WeightCalculatorId, Ct);
				// TODO: Create WeightCalctr from PoWeightCalculator if needed
			}

			return R;
		};
	}

#endif
}
