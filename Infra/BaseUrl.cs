using Ngaq.Core.Infra;
using Ngaq.Core.Infra.Cfg;
using Tsinswreng.CsCfg;
using Tsinswreng.CsCore;

namespace Ngaq.Local.Infra;

public  partial class BaseUrl(
	ICfgAccessor CfgAccessor
):I_GetBaseUrl{
	[Impl]
	public str GetBaseUrl(){
		var V = LocalCfgItems.ServerBaseUrl.GetFrom(CfgAccessor);

		return V??"";
	}
}
