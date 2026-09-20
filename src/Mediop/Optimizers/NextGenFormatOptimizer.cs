using Mediop.NextGenFormats;

namespace Mediop.Optimizers
{
	/// <summary>
	/// Base for optimizers that transcode into a next generation format (WebP, AVIF, JPEG XL).
	/// These only run when the requesting browser advertised support for the format, and once one of
	/// them succeeds the rest of the pipeline is aborted - the following optimizers only understand
	/// the original format.
	/// </summary>
	public abstract class NextGenFormatOptimizer : CommandLineToolOptimizer
	{
		/// <summary>Output extension, e.g. "webp". Also the token matched against the Accept header.</summary>
		public abstract string Extension { get; }

		/// <summary>True skips passing resize arguments to the tool (e.g. gif2webp cannot resize).</summary>
		public bool DisableResizing { get; set; }

		public override void Process(OptimizerArgs args)
		{
			if (!args.MediaOptions.CheckSupportOfExtension(Extension)) return;

			base.Process(args);

			if (!args.IsOptimized) return;

			args.Extension = Extension;
			args.AbortPipeline();
		}

		protected override string GetMediaSpecificArguments(OptimizerArgs args)
		{
			if (DisableResizing) return null;

			var transformationOptions = args.MediaOptions?.GetTransformationOptions();

			if (transformationOptions == null || !transformationOptions.ContainsResizing()) return null;

			return CreateResizeArguments(transformationOptions.Size.Width, transformationOptions.Size.Height);
		}

		protected abstract string CreateResizeArguments(int width, int height);
	}
}
