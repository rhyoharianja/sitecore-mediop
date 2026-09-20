using Sitecore.Resources.Media;

namespace Mediop.NextGenFormats.WebP
{
	/// <summary>
	/// Media type for .webp items uploaded straight into the media library. System.Drawing cannot read
	/// WebP, so metadata extraction is skipped rather than throwing on upload.
	/// </summary>
	public class WebPMedia : ImageMedia
	{
		public override Sitecore.Resources.Media.Media Clone()
		{
			return new WebPMedia();
		}

		protected override void UpdateImageMetaData(MediaStream mediaStream)
		{
			// intentionally empty: width/height cannot be read from WebP with System.Drawing
		}
	}
}
