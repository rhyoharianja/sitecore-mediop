using System.Web;
using Sitecore.Pipelines;
using Sitecore.XA.Foundation.MediaRequestHandler.Pipelines.MediaRequestHandler;

namespace Mediop.NextGenFormats
{
	/// <summary>
	/// SXA variant of the handler. SXA replaces the media handler with its own, so on an SXA site
	/// register this one instead - it keeps the mediaRequestHandler pipeline intact.
	/// </summary>
	public class MediaRequestHandlerXA : Sitecore.XA.Foundation.MediaRequestHandler.MediaRequestHandler
	{
		protected override bool DoProcessRequest(HttpContext context)
		{
			var mediaRequestHandlerArgs = new MediaRequestHandlerArgs(context);

			CorePipeline.Run("mediaRequestHandler", mediaRequestHandlerArgs, failIfNotExists: false);

			if (mediaRequestHandlerArgs.Aborted)
			{
				return mediaRequestHandlerArgs.Result;
			}

			mediaRequestHandlerArgs.Request.AddCustomOptions(new HttpContextWrapper(context));

			return DoProcessRequest(mediaRequestHandlerArgs.Context, mediaRequestHandlerArgs.Request, mediaRequestHandlerArgs.Media);
		}
	}
}
