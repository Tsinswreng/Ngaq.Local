namespace Ngaq.Local.Domains.Word.Svc;

using Ngaq.Core.Shared.Word.Models.Learn_;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Tools;
using Ngaq.Core.Word.Models;
using Ngaq.Core.Word.Models.Weight;
using Ngaq.Local.Tools.Cmd;



public  partial class SvcWeightCalctr {
	public  partial class EErr_:EnumErr{
		public OldAppErr WeightCalculatorExeErr() => Mk(nameof(WeightCalculatorExeErr));
		public OldAppErr WordWeightSerializationErr() => Mk(nameof(WordWeightSerializationErr));
	}
	public EErr_ EErr = new();
	public str ExePath { get; set; } = "";//TODO 靈活配置

	public async Task<IEnumerable<IWordWeightResult>> CalcAsy(
		IEnumerable<IWordForLearn> Words
		,CT Ct
	){
		var Json = JSON.stringify(Words);
		var Res = await CmdRunner.Inst.RunCommandAsync(ExePath, Json);
		if(Res.ExitCode != 0){
			var Err = EErr.WeightCalculatorExeErr();
			Err.Args.Add(Res.ExitCode+"\n\n"+Res.StdErr+"\n\n"+Res.StdOut);
			throw Err.ToErrBase();
		}
		var JsonRes = Res.StdOut??"";
		var R = JSON.parse<IEnumerable<IWordWeightResult>>(JsonRes);
		if(R == null){
			throw EErr.WordWeightSerializationErr().ToErrBase();
		}
		return R;
	}

}
