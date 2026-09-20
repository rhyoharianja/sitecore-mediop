using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Web.Hosting;
using Sitecore.Configuration;

namespace Mediop.Optimizers
{
	/// <summary>
	/// Optimizes media by shelling out to a command line tool that reads and writes temp files.
	/// </summary>
	public abstract class CommandLineToolOptimizer : OptimizerProcessor
	{
		private string _pathToExe;
		private string _additionalToolArguments;
		private int? _toolTimeout;

		/// <summary>
		/// Path to the executable. A value starting with ~ or / is resolved against the site root,
		/// falling back to the app base directory when there is no hosting environment (e.g. unit tests).
		/// </summary>
		public virtual string ExePath
		{
			get => _pathToExe;
			set
			{
				if (value.StartsWith("~") || value.StartsWith("/"))
					_pathToExe = HostingEnvironment.MapPath(value) ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, value.TrimStart('/', '\\').Replace('/', '\\'));
				else
					_pathToExe = value;
			}
		}

		/// <summary>Extra arguments passed to the tool on top of the required input/output ones.</summary>
		public virtual string AdditionalToolArguments
		{
			get => _additionalToolArguments;
			set => _additionalToolArguments = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
		}

		/// <summary>Per-process timeout in ms. Defaults to the Mediop.ToolTimeout setting.</summary>
		public virtual int ToolTimeout
		{
			get => _toolTimeout ?? Settings.GetIntSetting("Mediop.ToolTimeout", 60000);
			set => _toolTimeout = value;
		}

		/// <summary>True prepends the additional arguments, false appends them after the generated ones.</summary>
		protected virtual bool PrependAdditionalArguments => true;

		/// <summary>False when the tool rewrites the input file in place instead of writing a separate output file.</summary>
		protected virtual bool OptimizerUsesSeparateOutputFile => true;

		/// <summary>Extension the temp files need, if the tool insists on one (e.g. ".png"). Null keeps .tmp.</summary>
		protected virtual string TempFileExtension => null;

		protected override void ProcessOptimizer(OptimizerArgs args)
		{
			var tempFilePath = GetTempFilePath();
			var tempOutputPath = OptimizerUsesSeparateOutputFile ? GetTempFilePath() : null;

			var arguments = GenerateArguments(args, tempFilePath, tempOutputPath);

			try
			{
				using (var fileStream = File.Create(tempFilePath))
				{
					args.Stream.CopyTo(fileStream);
				}

				var exitCode = ExecuteProcess(arguments, out var output);

				if (exitCode != 0)
				{
					if (!IsSkipExitCode(exitCode))
					{
						throw new InvalidOperationException($"\"{ExePath} {arguments}\" exited with unexpected exit code {exitCode}. Output: {output}");
					}

					// the tool ran fine and decided there was nothing to gain; keep the original bytes
					if (MediopLog.IsDebugEnabled) MediopLog.Debug($"Mediop: {GetType().Name} skipped {args.MediaPath} (exit code {exitCode}).");

					args.Skipped = true;
					return;
				}

				var outputPath = OptimizerUsesSeparateOutputFile ? tempOutputPath : tempFilePath;

				using (var fileStream = File.OpenRead(outputPath))
				{
					// the tool succeeded, so swap the input stream out for the optimized bytes
					args.Stream.Dispose();
					args.Stream = new MemoryStream();
					fileStream.CopyTo(args.Stream);
					args.IsOptimized = true;
				}
			}
			finally
			{
				DeleteTempFile(tempFilePath);
				DeleteTempFile(tempOutputPath);
			}
		}

		protected virtual string GenerateArguments(OptimizerArgs args, string tempFilePath, string tempOutputPath)
		{
			var arguments = CreateToolArguments(tempFilePath, tempOutputPath);

			arguments = Merge(arguments, AdditionalToolArguments);
			arguments = Merge(arguments, GetMediaSpecificArguments(args));

			return arguments;
		}

		private string Merge(string arguments, string extra)
		{
			if (string.IsNullOrWhiteSpace(extra)) return arguments;

			extra = extra.Trim();

			return PrependAdditionalArguments
				? $"{extra} {arguments.TrimEnd()}"
				: $"{arguments.TrimEnd()} {extra}";
		}

		/// <summary>Arguments derived from the request itself, such as resize dimensions. Null when not applicable.</summary>
		protected virtual string GetMediaSpecificArguments(OptimizerArgs args)
		{
			return null;
		}

		/// <summary>
		/// Exit codes that mean "ran fine, nothing worth changing" rather than "failed". Returning true
		/// for one keeps the original bytes without logging an error.
		/// </summary>
		protected virtual bool IsSkipExitCode(int exitCode)
		{
			return false;
		}

		/// <summary>
		/// Runs the tool and returns its exit code, with whatever it wrote to stdout and stderr in
		/// <paramref name="output" />. Throws only when the tool could not run at all, or hung.
		/// Judging the exit code is the caller's job, since what counts as failure is tool specific.
		/// </summary>
		protected virtual int ExecuteProcess(string arguments, out string output)
		{
			if (MediopLog.IsDebugEnabled) MediopLog.Debug($"Mediop: running \"{ExePath}\" {arguments}");

			using (var process = new Process())
			{
				process.StartInfo.FileName = ExePath;
				process.StartInfo.Arguments = arguments;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.CreateNoWindow = true;

				var processOutput = new ConcurrentBag<string>();
				process.OutputDataReceived += (sender, eventArgs) => processOutput.Add(eventArgs.Data);
				process.ErrorDataReceived += (sender, eventArgs) => processOutput.Add(eventArgs.Data);

				try
				{
					process.Start();
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException($"\"{ExePath} {arguments}\" could not be started. See the inner exception for details.", ex);
				}

				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				if (!process.WaitForExit(ToolTimeout))
				{
					try
					{
						process.Kill();
					}
					catch
					{
						// swallow: we want the timeout exception below, not a kill failure
					}

					throw new InvalidOperationException($"\"{ExePath} {arguments}\" took longer than {ToolTimeout}ms to run. Output: {string.Join(Environment.NewLine, processOutput)}");
				}

				output = string.Join(Environment.NewLine, processOutput);

				return process.ExitCode;
			}
		}

		protected abstract string CreateToolArguments(string tempFilePath, string tempOutputPath);

		protected virtual string GetTempFilePath()
		{
			try
			{
				var configuredPath = Settings.GetSetting("Mediop.TempFilePath");

				string path;

				if (!string.IsNullOrWhiteSpace(configuredPath))
				{
					Directory.CreateDirectory(configuredPath);
					path = Path.Combine(configuredPath, Path.GetRandomFileName());
				}
				else
				{
					path = Path.GetTempFileName();
				}

				if (TempFileExtension == null) return path;

				// GetTempFileName creates a zero byte file; drop it so we do not leak temp files (see Mediop issue #36)
				if (File.Exists(path)) File.Delete(path);

				return Path.ChangeExtension(path, TempFileExtension);
			}
			catch (IOException ioe)
			{
				throw new InvalidOperationException($"Error creating a temp file to optimize into. This happens when IIS cannot write to {Path.GetTempPath()}, or when the temp folder is full (65535 files).", ioe);
			}
		}

		private static void DeleteTempFile(string path)
		{
			if (string.IsNullOrEmpty(path)) return;

			try
			{
				if (File.Exists(path)) File.Delete(path);
			}
			catch (IOException ex)
			{
				MediopLog.Warn($"Mediop: could not delete temp file {path}.", ex);
			}
		}
	}
}
