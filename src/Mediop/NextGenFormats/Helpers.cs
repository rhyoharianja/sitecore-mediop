using System;
using System.Linq;
using System.Web;
using Sitecore.Configuration;

namespace Mediop.NextGenFormats
{
	/// <summary>
	/// Works out which next generation formats the caller supports, from the Accept header or from an
	/// explicit "extension" query string value (used when a CDN sits in front of the delivery servers).
	/// </summary>
	public class Helpers
	{
		public virtual PipelineHelpers PipelineHelpers { get; set; } = new PipelineHelpers();

		/// <summary>
		/// Header to read instead of Accept. Some CDNs strip or normalize Accept, so they are configured
		/// to copy the original value into a custom header.
		/// </summary>
		public virtual string CustomAcceptHeaderName => Settings.GetSetting("Mediop.CDN.CustomAcceptHeaderName");

		public static bool CdnEnabled => Settings.GetBoolSetting("Mediop.CDN.Enabled", false);

		/// <summary>Comma separated list of supported extensions, e.g. "webp,avif". Empty when none.</summary>
		public virtual string GetSupportedFormats(HttpContextBase context)
		{
			var acceptTypes = context?.Request?.AcceptTypes ?? new string[0];

			if (acceptTypes.Any())
			{
				return PipelineHelpers.RunGetSupportedFormatsPipeline(acceptTypes);
			}

			var customAcceptHeader = GetCustomAcceptHeader(context);

			return string.IsNullOrEmpty(customAcceptHeader)
				? string.Empty
				: PipelineHelpers.RunGetSupportedFormatsPipeline(new[] { customAcceptHeader });
		}

		public virtual bool CheckSupportedFormat(HttpContextBase context, string extension)
		{
			var acceptTypes = context?.Request?.AcceptTypes ?? new string[0];

			if (acceptTypes.Any())
			{
				return ContainsMimeType(string.Join(",", acceptTypes), extension);
			}

			var customAcceptHeader = GetCustomAcceptHeader(context);

			return !string.IsNullOrEmpty(customAcceptHeader) && ContainsMimeType(customAcceptHeader, extension);
		}

		/// <summary>Matches against the extension list carried on MediaOptions.CustomOptions["extension"].</summary>
		public virtual bool ContainsExtension(string extensionList, string extension)
		{
			if (string.IsNullOrEmpty(extensionList) || string.IsNullOrEmpty(extension)) return false;

			return extensionList
				.Split(',')
				.Any(candidate => candidate.Trim().Equals(extension, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>Matches against a raw Accept header value, e.g. "image/webp,image/avif,*/*".</summary>
		public virtual bool ContainsMimeType(string acceptHeader, string extension)
		{
			if (string.IsNullOrEmpty(acceptHeader) || string.IsNullOrEmpty(extension)) return false;

			return acceptHeader.IndexOf($"image/{extension}", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		/// <summary>
		/// Extension list for this request: an explicit query string wins over the Accept header, so a
		/// CDN can pin a variant into its cache key.
		/// </summary>
		public virtual string GetCustomOptions(HttpContextBase context)
		{
			var requestExtension = context?.Request?.QueryString?["extension"];

			return !string.IsNullOrEmpty(requestExtension) ? requestExtension : GetSupportedFormats(context);
		}

		private string GetCustomAcceptHeader(HttpContextBase context)
		{
			var headerName = CustomAcceptHeaderName;

			return string.IsNullOrEmpty(headerName) ? null : context?.Request?.Headers?[headerName];
		}
	}
}
