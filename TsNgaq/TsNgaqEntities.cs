namespace Ngaq.Local.TsNgaq;

public class TsNgaqEntities{
	//置于內部類中減LSP提示
	public class TsNgaqPoBase{
		public i64 id{get;set;}
		public str belong{get;set;} = "";
		public i64 ct{get;set;}
		public i64 mt{get;set;}
	}
	public class textWord:TsNgaqPoBase{
		public str text{get;set;} = "";
	}

	public class property:TsNgaqPoBase{
		public i64 wid{get;set;}
		public str text{get;set;} = "";
	}
	public class learn:TsNgaqPoBase{
		public i64 wid{get;set;}
	}

	public class JnWord{
		public JnWord(textWord textWord,IList<property> propertys,IList<learn> learns){
			this.textWord = textWord;
			this.propertys = propertys;
			this.learns = learns;
		}
		public textWord textWord{get;set;}
		public IList<property> propertys{get;set;} = new List<property>();
		public IList<learn> learns{get;set;} = new List<learn>();
	}
}
