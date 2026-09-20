using System;
using System.IO;

namespace Mediop.Optimizers
{
	/// <summary>
	/// Base class for every optimizer. It buffers the incoming stream so a failed - or counter-productive -
	/// optimization can always fall back to the original bytes.
	/// Implementations own the input stream: once consumed, dispose it.
	/// </summary>
	public abstract class OptimizerProcessor
	{
		public virtual void Process(OptimizerArgs args)
		{
			if (!ValidateInputStream(args)) return;

			// the flag belongs to this processor, not to the ones that ran before it
			args.Skipped = false;

			// unseekable streams show up when serving an unresized image straight from the blob field
			if (!args.Stream.CanSeek)
			{
				var bufferedStream = new MemoryStream();
				args.Stream.CopyTo(bufferedStream);

				args.Stream.Dispose();
				args.Stream = bufferedStream;
			}

			var originalStream = new MemoryStream();

			try
			{
				args.Stream.Seek(0, SeekOrigin.Begin);
				args.Stream.CopyTo(originalStream);
				originalStream.Seek(0, SeekOrigin.Begin);

				args.Stream.Seek(0, SeekOrigin.Begin);

				ProcessOptimizer(args);

				if (args.Skipped)
				{
					// the input stream was never replaced, so just rewind it for the next processor
					args.IsOptimized = false;
					args.Stream.Seek(0, SeekOrigin.Begin);
					originalStream.Dispose();
					return;
				}

				ValidateReturnStream(args, originalStream);
			}
			catch (Exception ex)
			{
				args.IsOptimized = false;
				args.Stream?.Dispose();
				args.Stream = originalStream;
				MediopLog.Error($"Mediop: unable to optimize {args.MediaPath} due to a processing error. It will be served unchanged.", ex);
			}
		}

		protected virtual bool ValidateInputStream(OptimizerArgs args)
		{
			return args.Stream != null && args.Stream.CanRead;
		}

		protected virtual void ValidateReturnStream(OptimizerArgs args, Stream originalStream)
		{
			args.IsOptimized = false;

			if (args.Stream == null)
			{
				args.AddMessage($"{GetType().Name} returned a null stream, which usually means it failed. Keeping the original stream.");
				args.Stream = originalStream;
				return;
			}

			if (args.Stream.Length == 0)
			{
				args.AddMessage($"{GetType().Name} returned a zero length stream, which usually means it failed. Keeping the original stream.");
				args.Stream.Dispose();
				args.Stream = originalStream;
				return;
			}

			if (args.Stream is FileStream) throw new InvalidOperationException($"{GetType().Name} returned a FileStream, which would leave orphaned files on disk once disposed. Return a pre-buffered MemoryStream instead.");
			if (!args.Stream.CanSeek) throw new InvalidOperationException($"{GetType().Name} returned a non seekable stream. Buffer it into a MemoryStream first.");

			if (!args.Stream.CanRead)
			{
				args.AddMessage($"{GetType().Name} returned a non readable stream, which usually means it disposed the input stream and then failed. Keeping the original stream.");
				args.Stream = originalStream;
				return;
			}

			// rewind so the next optimizer in the pipeline gets a clean stream
			args.Stream.Seek(0, SeekOrigin.Begin);

			if (args.Stream.Length > originalStream.Length)
			{
				args.AddMessage($"{GetType().Name}: optimization made the file larger ({args.Stream.Length} vs {originalStream.Length} bytes). Using the original instead.");
				args.Stream.Dispose();
				args.Stream = originalStream;
			}
			else if (args.Stream.Length == originalStream.Length)
			{
				args.AddMessage($"{GetType().Name}: optimization produced the same file size ({args.Stream.Length} bytes). Using the original instead.");
				args.Stream.Dispose();
				args.Stream = originalStream;
			}
			else
			{
				originalStream.Dispose();
				args.IsOptimized = true;
			}
		}

		protected abstract void ProcessOptimizer(OptimizerArgs args);
	}
}
