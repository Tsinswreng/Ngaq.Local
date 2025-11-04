using Ngaq.Core.Frontend.ImgBg;
using Ngaq.Core.Infra.Cfg;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Infra.Url;
using Ngaq.Core.Tools;
using Tsinswreng.CsCfg;
using Tsinswreng.CsTools;

namespace Ngaq.Local.ImplFrontend;

public  partial class SvcImg:IImgGetter{

	public IList<str> GalleryDirs{get;set;} = [];
	public IList<str> FilePaths{get;set;} = new List<str>();
	public IList<u64> Order = new List<u64>();
	protected u64 Index{get;set;}=0;
	public ICfgAccessor CfgAccessor{ get; set; } = AppCfg.Inst;
	public I_GetBaseDir BaseDir = BaseDirMgr.Inst;

//TODO 若此中拋異常且無catch則初始化DI旹則崩 宜傳異常置前端
	public SvcImg(){
try{
var CfgDir = ItemsAppCfg.GalleryDirs.GetFrom(CfgAccessor)??[];
		foreach(var _Dir in CfgDir){
			if(_Dir is str s && !str.IsNullOrEmpty(s)){
				var Dir = s;
				Dir = BaseDir.Combine(Dir);
				if(Directory.Exists(s)){
					GalleryDirs.Add(s);
				}
			}
		}
		foreach(var DirInCfg in GalleryDirs){
			foreach (var file in Directory.EnumerateFiles(DirInCfg, "*.*", SearchOption.AllDirectories)){
				FilePaths.Add(file);
			}
		}
		Order = ToolRandom.RandomArrU64(0, (u64)FilePaths.Count-1, (u64)FilePaths.Count);
}
catch (System.Exception e){
	ItemsErr.Word.BackgroundImageServiceFailedToInit.ToErr().AddDebugArgs(e);
}
	}

	protected str? NextFilePath(){
		if(Index >= (u64)FilePaths.Count){
			Index = 0;
		}
		if(Order.Count == 0){
			return null;
		}
		var IndexInFiles = Order[(i32)Index];
		var FilePath = FilePaths[(int)IndexInFiles];
		Index++;
		return FilePath;
	}

	public ITypedObj FilePathToTypedObj(str? FilePath){
		if(str.IsNullOrEmpty(FilePath)){
			return new TypedObj();
		}
		Stream stream = File.OpenRead(FilePath);
		var R = new TypedObj{
			Type = typeof(Stream)
			,Data = stream
			,TypeCode = (i64)IImgGetter.EType.Stream
		};
		return R;
	}

	public virtual IEnumerable<ITypedObj> GetN(u64 n){
		for(u64 i=0;i<n;i++){
			var FilePath = NextFilePath();
			yield return FilePathToTypedObj(FilePath);
		}
	}

}
