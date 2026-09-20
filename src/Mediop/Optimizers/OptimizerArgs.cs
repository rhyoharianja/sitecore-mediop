using System.IO;
using Sitecore.Pipelines;
using Sitecore.Resources.Media;

namespace Mediop.Optimizers
{
	public class OptimizerArgs : PipelineArgs
	{
		public Stream Stream { get; set; }

		public MediaOptions MediaOptions { get; }

		public bool IsOptimized { get; set; }

		/// <summary>
		/// Set by an optimizer that decided there was nothing worth doing - for instance a lossy tool
		/// that could not beat the original. It means "no result, and that is fine", as opposed to a
		/// failure, so nothing is logged about it.
		/// </summary>
		public bool Skipped { get; set; }

		/// <summary>Set when the optimizer changed the output format, e.g. "webp".</summary>
		public string Extension { get; set; }

		public string MediaPath { get; set; }

		public OptimizerArgs(Stream inputStream)
		{
			IsOptimized = false;
			Stream = inputStream;
		}

		public OptimizerArgs(Stream inputStream, MediaOptions options, string mediaPath) : this(inputStream)
		{
			MediaOptions = options;
			MediaPath = mediaPath;
		}
	}
}
