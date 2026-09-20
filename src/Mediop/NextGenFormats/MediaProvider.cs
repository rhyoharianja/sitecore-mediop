using System.Web;
using Sitecore.Data.Items;
using Sitecore.Links.UrlBuilders;
using Sitecore.Resources.Media;
using Sitecore.Web;

namespace Mediop.NextGenFormats
{
	/// <summary>
	/// Appends "extension=webp,avif" to generated media URLs so a CDN caches one variant per format set.
	/// Only needed when a CDN sits in front of delivery; without it the Accept header alone decides.
	/// </summary>
	// MediaUrlOptions is obsolete in 10.4, but the base class still declares the overload and Sitecore
	// itself still calls it, so the override has to stay until the platform drops it.
#pragma warning disable CS0612, CS0618, CS0672
	public class MediaProvider : Sitecore.Resources.Media.MediaProvider
	{
		public override string GetMediaUrl(MediaItem item, MediaUrlOptions options)
		{
			return AppendSupportedFormats(item, base.GetMediaUrl(item, options));
		}

		public override string GetMediaUrl(MediaItem item, MediaUrlBuilderOptions options)
		{
			return AppendSupportedFormats(item, base.GetMediaUrl(item, options));
		}

		protected virtual string AppendSupportedFormats(MediaItem item, string url)
		{
			if (url == null || !item.MimeType.StartsWith("image") || url.Contains("extension")) return url;

			var context = HttpContext.Current;
			if (context == null) return url;

			var extensions = new Helpers().GetSupportedFormats(new HttpContextWrapper(context));

			return string.IsNullOrEmpty(extensions) ? url : WebUtil.AddQueryString(url, "extension", extensions);
		}
	}
#pragma warning restore CS0612, CS0618, CS0672
}
