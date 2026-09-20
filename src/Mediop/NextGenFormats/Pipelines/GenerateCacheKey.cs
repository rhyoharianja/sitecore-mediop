using System.Web;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Pipelines.Response.RenderRendering;

namespace Mediop.NextGenFormats.Pipelines
{
	/// <summary>
	/// Varies the HTML rendering cache key by format support. Without this, a cached rendering built for
	/// a WebP capable browser would be served to one that cannot read WebP. Only needed with the CDN
	/// media provider, which bakes the extension into the markup.
	/// </summary>
	public class GenerateCacheKey : RenderRenderingProcessor
	{
		public virtual string Extension { get; set; }

		public override void Process(RenderRenderingArgs args)
		{
			Assert.ArgumentNotNull(args, nameof(args));

			if (args.Rendered || !args.Cacheable || !Helpers.CdnEnabled) return;

			var context = HttpContext.Current;
			if (context == null) return;

			var extensionSupport = new HttpContextWrapper(context).CheckSupportOfExtension(Extension);

			args.CacheKey += $"_#{Extension}:{extensionSupport}";
		}
	}
}
