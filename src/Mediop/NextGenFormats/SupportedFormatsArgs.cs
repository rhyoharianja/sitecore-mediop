using Sitecore.Collections;
using Sitecore.Pipelines;

namespace Mediop.NextGenFormats
{
	public class SupportedFormatsArgs : PipelineArgs
	{
		public SupportedFormatsArgs()
		{
			Extensions = new Set<string>();
		}

		/// <summary>Extensions the client advertised support for, filled in by the pipeline.</summary>
		public Set<string> Extensions { get; set; }

		/// <summary>Raw Accept header value.</summary>
		public string Input { get; set; }

		/// <summary>MIME prefix to match against, e.g. "image/".</summary>
		public string Prefix { get; set; }
	}
}
