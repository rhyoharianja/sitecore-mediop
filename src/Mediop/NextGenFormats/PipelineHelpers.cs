using Sitecore.Pipelines;

namespace Mediop.NextGenFormats
{
	public class PipelineHelpers
	{
		public virtual string RunGetSupportedFormatsPipeline(string[] acceptTypes)
		{
			var args = new SupportedFormatsArgs
			{
				Input = string.Join(",", acceptTypes),
				Prefix = "image/"
			};

			CorePipeline.Run("mediopGetSupportedFormats", args);

			return string.Join(",", args.Extensions);
		}
	}
}
