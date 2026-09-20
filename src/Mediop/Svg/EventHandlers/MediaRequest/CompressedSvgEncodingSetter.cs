using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Sitecore.Events;
using Sitecore.Resources.Media;

namespace Mediop.Svg.EventHandlers.MediaRequest
{
	/// <summary>
	/// Adds Content-Encoding: gzip when the stored SVG has been gzipped by GzipSvgData, so the browser
	/// knows to inflate it. Only relevant when gzipping is enabled.
	/// </summary>
	public class CompressedSvgEncodingSetter
	{
		public void OnMediaRequest(object sender, EventArgs args)
		{
			if (!(args is SitecoreEventArgs sitecoreEventArgs) || !sitecoreEventArgs.Parameters.Any()) return;

			if (!(sitecoreEventArgs.Parameters[0] is Sitecore.Resources.Media.MediaRequest request)) return;

			var media = MediaManager.GetMedia(request.MediaUri);

			if (media == null || !"svg".Equals(media.Extension, StringComparison.OrdinalIgnoreCase)) return;

			using (var stream = media.GetStream(request.Options))
			{
				if (stream == null || stream.Length < 3) return;

				var header = new byte[3];
				stream.Stream.Read(header, 0, 3);

				// gzip magic number, see RFC 1952
				if (header[0] != 0x1f || header[1] != 0x8b || header[2] != 0x08) return;

				SetContentEncoding(request.InnerRequestBase.RequestContext.HttpContext.Response);
			}
		}

		private static void SetContentEncoding(HttpResponseBase response)
		{
			if (HasGzipContentEncoding(response.Headers)) return;

			response.AddHeader("Content-Encoding", "gzip");
		}

		private static bool HasGzipContentEncoding(NameValueCollection headers)
		{
			return headers.AllKeys.Any(key => key.Equals("content-encoding", StringComparison.OrdinalIgnoreCase))
				&& "gzip".Equals(headers["content-encoding"], StringComparison.OrdinalIgnoreCase);
		}
	}
}
