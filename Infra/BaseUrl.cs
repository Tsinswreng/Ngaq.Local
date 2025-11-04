namespace Ngaq.Local.Infra;

using Ngaq.Core.Infra.Cfg;
using Ngaq.Core.Infra.Url;
using Tsinswreng.CsCfg;
using Tsinswreng.CsCore;



public partial class BaseUrl(
	ICfgAccessor CfgAccessor
):I_GetBaseUrl{
	[Impl]
	public str GetBaseUrl(){
		var V = ItemsAppCfg.ServerBaseUrl.GetFrom(CfgAccessor);

		return V??"";
	}
}
