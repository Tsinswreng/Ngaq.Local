using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Tools;
using Ngaq.Core.Word.Models;
using Ngaq.Core.Word.Models.Learn_;
using Ngaq.Core.Word.Models.Weight;
using Ngaq.Local.Tools.Cmd;

namespace Ngaq.Local.Word.Svc;



public class SvcWeightCalctr {
	public class EErr_:EnumErr{
		public IAppErr WeightCalculatorExeErr() => Mk(nameof(WeightCalculatorExeErr));
		public IAppErr WordWeightSerializationErr() => Mk(nameof(WordWeightSerializationErr));
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
			Err.Msg = Res.ExitCode+"\n\n"+Res.StdErr+"\n\n"+Res.StdOut;
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
