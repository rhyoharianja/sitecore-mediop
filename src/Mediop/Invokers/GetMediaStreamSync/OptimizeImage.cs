using Sitecore.Diagnostics;
using Sitecore.Resources.Media;

namespace Mediop.Invokers.GetMediaStreamSync
{
	/// <summary>
	/// Optimizes media as it is served, before it reaches the media cache.
	/// Every response is optimized, including the first one, at the cost of making that first visitor
	/// wait for the encoder. Use this when a CDN pulls each image exactly once; otherwise prefer the
	/// async media cache strategy.
	/// </summary>
	public class OptimizeImage
	{
		private readonly MediaOptimizer _optimizer;

		public OptimizeImage() : this(new MediaOptimizer())
		{
		}

		protected OptimizeImage(MediaOptimizer optimizer)
		{
			Assert.ArgumentNotNull(optimizer, nameof(optimizer));

			_optimizer = optimizer;
		}

		public void Process(GetMediaStreamPipelineArgs args)
		{
			Assert.ArgumentNotNull(args, nameof(args));

			// thumbnails and the content editor are not worth the CPU
			if (args.Options.Thumbnail) return;
			if (Sitecore.Context.Site?.Name == "shell") return;

			var outputStream = args.OutputStream;
			if (outputStream == null) return;

			if (!outputStream.AllowMemoryLoading)
			{
				MediopLog.Warn($"Mediop: media is larger than the maximum size allowed for in-memory processing, skipping. Media item: {outputStream.MediaItem.Path}");
				return;
			}

			var optimizedOutputStream = _optimizer.Process(outputStream, args.Options);

			if (optimizedOutputStream == null || outputStream.Stream == optimizedOutputStream.Stream)
			{
				MediopLog.Debug($"Mediop: {outputStream.MediaItem.MediaPath} was not optimized (media type or path exclusion).");
				return;
			}

			outputStream.Dispose(); // thread safe, will not double dispose

			args.OutputStream = optimizedOutputStream;

			if (optimizedOutputStream.Extension == "webp" || optimizedOutputStream.Extension == "avif" || optimizedOutputStream.Extension == "jxl")
			{
				// the rest of the pipeline would try to treat the output as the original format and fail
				args.AbortPipeline();
			}
		}
	}
}
