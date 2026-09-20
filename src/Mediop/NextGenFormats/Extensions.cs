using System.Web;
using Sitecore.Resources.Media;

namespace Mediop.NextGenFormats
{
	public static class Extensions
	{
		private static readonly Helpers Helpers = new Helpers();

		/// <summary>Carries the formats this client supports into MediaOptions so optimizers can read them.</summary>
		public static void AddCustomOptions(this MediaRequest request, HttpContextBase context)
		{
			var customExtension = Helpers.GetCustomOptions(context);

			if (!string.IsNullOrEmpty(customExtension))
			{
				request.Options.CustomOptions["extension"] = customExtension;
			}
		}

		public static bool CheckSupportOfExtension(this HttpContextBase context, string extension)
		{
			return Helpers.CheckSupportedFormat(context, extension);
		}

		public static bool CheckSupportOfExtension(this MediaOptions mediaOptions, string extension)
		{
			return Helpers.ContainsExtension(mediaOptions?.CustomOptions["extension"], extension);
		}
	}
}
