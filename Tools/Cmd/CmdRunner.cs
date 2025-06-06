using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
namespace Ngaq.Local.Tools.Cmd;
public class CmdRunner {
	protected static CmdRunner? _Inst = null;
	public static CmdRunner Inst => _Inst??= new CmdRunner();

	public async Task<CmdResult> RunCommandAsync(
		string fileName, string arguments
	){
		var outputBuilder = new StringBuilder();
		var errorBuilder = new StringBuilder();

		using (Process process = new Process()) {
			process.StartInfo.FileName = fileName;      // 如 "cmd.exe" 或 "ping"
			process.StartInfo.Arguments = arguments;    // 参数字符串

			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.UseShellExecute = false;  // 必须设为 false 以重定向流
			process.StartInfo.CreateNoWindow = true;    // 不显示窗口

			// 异步读取输出
			process.OutputDataReceived += (sender, e) => {
				if (e.Data != null)
					outputBuilder.AppendLine(e.Data);
			};

			// 异步读取错误
			process.ErrorDataReceived += (sender, e) => {
				if (e.Data != null)
					errorBuilder.AppendLine(e.Data);
			};

			var tcs = new TaskCompletionSource<bool>();

			process.EnableRaisingEvents = true;
			process.Exited += (sender, e) => {
				tcs.SetResult(true);
			};

			process.Start();

			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			await tcs.Task; // 等待进程退出
			var R = new CmdResult(){
				ExitCode = process.ExitCode
				,StdOut = outputBuilder.ToString()
				,StdErr = errorBuilder.ToString()
			};


			//return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
			return R;
		}
	}
}
