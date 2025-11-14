namespace Ngaq.Local.TsNgaq;

using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Learn_;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Model.Po.Word;
using E = Ngaq.Local.TsNgaq.TsNgaqEntities;
using Ngaq.Core.Shared.Base.Models.Po;

public class TsNgaqEntities{
	//置于內部類中減LSP提示
	//字段名與數據庫ʸʹ名 同、勿緟名ᵣ
	public class TsNgaqPoBase{
		public i64 id{get;set;}
		public str belong{get;set;} = "";
		public i64 ct{get;set;}
		public i64 mt{get;set;}
	}
	public class textWord:TsNgaqPoBase{
		public str text{get;set;} = "";
	}
#pragma warning disable CS8981
	public class property:TsNgaqPoBase{
		public i64 wid{get;set;}
		public str text{get;set;} = "";
	}
	public class learn:TsNgaqPoBase{
		public i64 wid{get;set;}
	}

	public class TsJnWord{
		public TsJnWord(textWord textWord,IList<property> propertys,IList<learn> learns){
			this.textWord = textWord;
			this.propertys = propertys;
			this.learns = learns;
		}
		public textWord textWord{get;set;}
		public IList<property> propertys{get;set;} = new List<property>();
		public IList<learn> learns{get;set;} = new List<learn>();
	}



	public static ELearn ConvLearnResult(str TsNgaqLearnResult){
		var R = (TsNgaqLearnResult)switch{
			"add"=>ELearn.Add,
			"rmb"=>ELearn.Rmb,
			"fgt"=>ELearn.Fgt,
			_ => throw new ArgumentException("Invalid learn result"),
		};
		return R;
	}

	public static str ConvProp(str Old){
		var K = KeysProp.Inst;
		var Warn=(str s)=>{
			Console.WriteLine($"WarningTag: {s}");
			return s;
		};
		string R = Old switch{
			"mean" => K.description,
			nameof(K.tag) => K.tag,
			"annotation" => K.note,
			nameof(K.source) => K.source,
			"pronounce" => K.pronunciation,
			nameof(K.alias) => K.alias,
			nameof(K.weight) => K.weight,
			nameof(K.usage) => K.usage,
			nameof(K.example) => K.example,
			nameof(K.relation) => K.relation,
			"ref" => K.Ref,
			_ => Warn(Old),
		};
		return R;
	}

	public static JnWord ToNewJnWord(
		E.TsJnWord Old
	){
		var R = new JnWord();
		return ToNewJnWord(Old, ref R);
		//return R;
	}

	public static JnWord ToNewJnWord(
		E.TsJnWord Old
		,ref JnWord R
	){
		R??= new JnWord();
		static IBizCreateUpdateTime ConvBizTime(E.TsNgaqPoBase Old, IBizCreateUpdateTime PoBase){
			PoBase.BizCreatedAt = Old.ct;
			PoBase.BizUpdatedAt = Old.mt;
			return PoBase;
		}
		static PoWord ToPoWord(E.textWord Old, ref PoWord R){
			R??= new PoWord();
			ConvBizTime(Old, R);
			R.Head = Old.text;
			R.Lang = Old.belong;
			return R;
		}

		static PoWordLearn ToPoLearn(E.learn Old, IdWord WordId, ref PoWordLearn R){
			R??= new PoWordLearn();
			ConvBizTime(Old, R);
			R.WordId = WordId;
			R.LearnResult = ConvLearnResult(Old.belong);
			return R;
		}

		static PoWordProp ToPoProp(E.property Old, IdWord WordId, ref PoWordProp R){
			R??= new PoWordProp();
			ConvBizTime(Old, R);
			R.WordId = WordId;
			R.KStr = ConvProp(Old.belong);
			R.VStr = Old.text;
			return R;
		}

		{
			var o = R.Word;
			ToPoWord(Old.textWord, ref o);
			R.Word = o;
		}
		foreach(var OldLearn in Old.learns){
			var NeoPoLearn = new PoWordLearn();
			ToPoLearn(OldLearn, R.Word.Id, ref NeoPoLearn);
			R.Learns.Add(NeoPoLearn);
		}

		foreach(var OldProp in Old.propertys){
			var NeoProp = new PoWordProp();
			ToPoProp(OldProp, R.Word.Id, ref NeoProp);
			R.Props.Add(NeoProp);
		}
		return R;
	}



}

