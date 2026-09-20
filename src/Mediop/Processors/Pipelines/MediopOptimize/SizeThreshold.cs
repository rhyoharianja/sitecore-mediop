namespace Mediop.Processors.Pipelines.MediopOptimize
{
	/// <summary>
	/// Skips media outside a configured size window. Tiny files rarely shrink enough to pay for the
	/// process spawn, and very large sources can tie up a worker for seconds on end - both matter on CD,
	/// where the optimizer shares CPU with page rendering.
	/// </summary>
	public class SizeThreshold : MediopOptimizeProcessor
	{
		/// <summary>Media smaller than this (bytes) is left alone. 0 disables the lower bound.</summary>
		public long MinimumSizeInBytes { get; set; }

		/// <summary>Media larger than this (bytes) is left alone. 0 disables the upper bound.</summary>
		public long MaximumSizeInBytes { get; set; }

		protected override void ProcessOptimize(ProcessorArgs args)
		{
			var size = args.InputStream.Length;

			if (MinimumSizeInBytes > 0 && size < MinimumSizeInBytes)
			{
				args.AbortPipeline();
				return;
			}

			if (MaximumSizeInBytes > 0 && size > MaximumSizeInBytes)
			{
				MediopLog.Debug($"Mediop: skipping {args.InputStream.MediaItem.MediaPath} because it is {size} bytes, over the {MaximumSizeInBytes} byte limit.");
				args.AbortPipeline();
			}
		}
	}
}
