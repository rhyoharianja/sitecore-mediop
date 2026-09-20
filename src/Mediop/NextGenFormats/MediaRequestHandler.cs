using System.Web;
using Sitecore.Resources.Media;

namespace Mediop.NextGenFormats
{
	/// <summary>
	/// Media handler that records the client's supported formats on the request before Sitecore resolves
	/// the media, so the optimizers can transcode to WebP/AVIF. Registered in web.config in place of
	/// Sitecore.Resources.Media.MediaRequestHandler.
	/// </summary>
	public class MediaRequestHandler : Sitecore.Resources.Media.MediaRequestHandler
	{
		protected override bool DoProcessRequest(HttpContext context, MediaRequest request, Media media)
		{
			request.AddCustomOptions(new HttpContextWrapper(context));

			return base.DoProcessRequest(context, request, media);
		}
	}
}
