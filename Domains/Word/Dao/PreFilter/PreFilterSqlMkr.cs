using System.Text;
using Ngaq.Core.Shared.StudyPlan.Models.PreFilter;
using Ngaq.Core.Tools;
using Tsinswreng.CsCore;
using Tsinswreng.CsSqlHelper;
using Tsinswreng.CsTools;

namespace Ngaq.Local.Domains.Word.Dao.PreFilter;

public record ParamEtValue(
	IParam Param
	,obj? Value
	///
	,Type? ValueType = null
){

}

public class PreFilterSqlMkr {

	public PreFilterSqlMkr() {

	}

	///
	IParam MkParam(str Name) {
		throw new NotImplementedException();//這裏我稍後自己寫
	}

	ParamEtValue MkParamEtValue(str Name, object Value, Type? ValueType = null) {
		throw new NotImplementedException();//這裏我稍後自己寫
	}

	str Field(str Name){
		throw new NotImplementedException();//這裏我稍後自己寫
	}


	public void Mk(FilterItem FilterItem, str CodeField){
		var fld = Field(CodeField);
		var paramValuePairs = FilterItem.Values.Select(x=>{
			var paramN = ToolId.NewUlidUInt128();
			var param = MkParam("_"+ToolUInt128.ToLow64Base(paramN));
			var r = new ParamEtValue(Param: param, Value: x);
			return r;
		}).ToList();

		if(  EFilterOperationMode.IncludeAll.Eq(FilterItem.Operation)  ){
			var sql = $"AND {fld} IN ({paramValuePairs.ToSqlTuple()})";
		}
	}
}

public static class ExtnParamEtValue{
	extension(IList<ParamEtValue> z){
		/// -> (@_1ckmwnFJlBuEbAwKV_uv1, @_1cl9Ev-8L31Q96Rx-83Ws, ...)
		public str ToSqlTuple(){
			return str.Join(
				","
				,z.Select(x=>x.Param)
			);
		}
	}
}


#if false
把FilterItem轉成sql。 要用參數化。
創建命名參數就用MkParam方法。你直接用就可以了、裏面實現不用管。 IParam可以直接拼進sql裏

每個函數上都要加註釋。
註釋上能對入參和返回值舉例的 就盡量舉例

避免無關改動(包括代碼風格等)、讓文本diff儘量少
#endif
