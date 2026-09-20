using System;
using Mediop.Invokers.GetMediaStreamSync;
using Mediop.Invokers.MediaCacheAsync;
using Sitecore.Diagnostics;
using Sitecore.Resources.Media;

namespace Mediop.Svg.Pipelines.GetMediaStream
{
	/// <summary>
	/// Stops Sitecore from trying to resize an SVG as if it were a bitmap, which otherwise fills the log
	/// with GDI errors on every SVG request.
	/// </summary>
	public class SvgIgnorer
	{
		/// <summary>
		/// Optimize the SVG inline before aborting. Set this only when the async media cache strategy is
		/// off, since aborting the pipeline here means the async path never sees the SVG.
		/// </summary>
		public bool SynchronouslyOptimizeSvgs { get; set; }

		public void Process(GetMediaStreamPipelineArgs args)
		{
			Assert.ArgumentNotNull(args, nameof(args));

			if (!"image/svg+xml".Equals(args.MediaData.MimeType, StringComparison.OrdinalIgnoreCase)) return;

			if (SynchronouslyOptimizeSvgs && !(MediaManager.Cache is OptimizingMediaCache))
			{
				new OptimizeImage().Process(args);
			}

			args.AbortPipeline();
		}
	}
}
