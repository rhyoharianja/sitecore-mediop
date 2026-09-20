using System;
using System.Diagnostics;
using Mediop.Processors;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Sitecore.Resources.Media;

namespace Mediop
{
	/// <summary>
	/// Entry point for optimization: takes a media stream, runs the mediopOptimize pipeline over it
	/// and hands back an optimized stream. Returns null when nothing could be optimized, which lets the
	/// caller keep the original stream untouched.
	/// </summary>
	public class MediaOptimizer
	{
		public virtual MediaStream Process(MediaStream stream, MediaOptions options)
		{
			Assert.ArgumentNotNull(stream, nameof(stream));
			Assert.ArgumentNotNull(options, nameof(options));

			if (!stream.AllowMemoryLoading)
			{
				MediopLog.Error($"Mediop: media is larger than the maximum size allowed for in-memory processing, skipping. Media item: {stream.MediaItem.Path}");
				return null;
			}

			var stopwatch = Stopwatch.StartNew();
			var args = new ProcessorArgs(stream, options);

			try
			{
				CorePipeline.Run("mediopOptimize", args);
			}
			catch (Exception exception)
			{
				MediopLog.Error($"Mediop: unable to optimize {stream.MediaItem.MediaPath} due to a processing error. It will be served unchanged.", exception);
				return null;
			}

			stopwatch.Stop();

			if (args.ResultStream != null && args.ResultStream.CanRead)
			{
				if (args.Message.Length > 0)
				{
					MediopLog.Info($"Mediop: messages occurred while optimizing {stream.MediaItem.MediaPath}: {args.Message.Trim()}");
				}

				var extension = args.Extension ?? stream.Extension;

				if (args.IsOptimized)
				{
					MediopLog.Info($"Mediop: optimized {stream.MediaItem.MediaPath}.{stream.MediaItem.Extension} [requested: {GetDimensions(options)} {args.Statistics.SizeBefore} bytes] [final: {args.Statistics.SizeAfter} bytes] [saved {args.Statistics.BytesSaved} bytes / {args.Statistics.PercentageSaved:p}] [took {stopwatch.ElapsedMilliseconds}ms] [extension {extension}]");
				}

				return new MediaStream(args.ResultStream, extension, stream.MediaItem);
			}

			if (!string.IsNullOrWhiteSpace(args.Message))
			{
				MediopLog.Warn($"Mediop: unable to optimize {stream.MediaItem.MediaPath}.{stream.MediaItem.Extension} because {args.Message.Trim()}");
			}

			// No message means nothing in the mediopOptimize pipeline claimed this file - e.g. a PDF or an
			// excluded path. That is not an error, so we stay quiet.
			return null;
		}

		protected virtual string GetDimensions(MediaOptions options)
		{
			if (options.MaxHeight == 0 && options.MaxWidth == 0 && options.Height == 0 && options.Width == 0) return string.Empty;

			var result = string.Empty;

			if (options.Width > 0) result = options.Width + "w";
			else if (options.MaxWidth > 0) result = options.MaxWidth + "mw";

			if (result.Length > 0 && (options.Height > 0 || options.MaxHeight > 0)) result += " x ";

			if (options.Height > 0) result += options.Height + "h";
			else if (options.MaxHeight > 0) result += options.MaxHeight + "mh";

			if (options.Thumbnail) result += " (thumb)";

			return result;
		}
	}
}
