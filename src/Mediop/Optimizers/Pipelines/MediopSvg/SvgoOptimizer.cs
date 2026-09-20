using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace Mediop.Optimizers.Pipelines.MediopSvg
{
	/// <summary>
	/// SVGO (https://github.com/svg/svgo), packaged as a Windows executable by
	/// https://github.com/Antonytm/svgo-executable. It streams over stdin/stdout rather than temp files,
	/// so it overrides the file based flow of the base class.
	/// </summary>
	public class SvgoOptimizer : CommandLineToolOptimizer
	{
		protected override void ProcessOptimizer(OptimizerArgs args)
		{
			using (var toolProcess = new Process())
			{
				toolProcess.StartInfo.FileName = ExePath;
				toolProcess.StartInfo.Arguments = AdditionalToolArguments;
				toolProcess.StartInfo.UseShellExecute = false;
				toolProcess.StartInfo.RedirectStandardInput = true;
				toolProcess.StartInfo.RedirectStandardOutput = true;
				toolProcess.StartInfo.RedirectStandardError = true;
				toolProcess.StartInfo.CreateNoWindow = true;

				// svgo resolves its config file relative to the working directory
				toolProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(ExePath) ?? string.Empty;

				var processOutput = new ConcurrentBag<string>();
				toolProcess.ErrorDataReceived += (sender, eventArgs) => processOutput.Add(eventArgs.Data);

				if (MediopLog.IsDebugEnabled) MediopLog.Debug($"Mediop: running \"{ExePath}\" {AdditionalToolArguments}");

				try
				{
					toolProcess.Start();
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException($"\"{ExePath}\" could not be started. See the inner exception for details.", ex);
				}

				// feed the SVG in, then close stdin to signal end of input
				var standardInput = toolProcess.StandardInput;
				args.Stream.CopyTo(standardInput.BaseStream);
				standardInput.Close();

				toolProcess.BeginErrorReadLine();

				args.Stream.Dispose();
				args.Stream = new MemoryStream();
				toolProcess.StandardOutput.BaseStream.CopyTo(args.Stream);
				args.IsOptimized = true;

				if (!toolProcess.WaitForExit(ToolTimeout))
				{
					try
					{
						toolProcess.Kill();
					}
					catch
					{
						// swallow: we want the timeout exception below
					}

					throw new InvalidOperationException($"\"{ExePath}\" took longer than {ToolTimeout}ms to run. Output: {string.Join(Environment.NewLine, processOutput)}");
				}

				if (toolProcess.ExitCode != 0)
				{
					throw new InvalidOperationException($"\"{ExePath}\" exited with unexpected exit code {toolProcess.ExitCode}. Output: {string.Join(Environment.NewLine, processOutput)}");
				}
			}
		}

		protected override string CreateToolArguments(string tempFilePath, string tempOutputPath)
		{
			return string.Empty;
		}
	}
}
