using Mediop.Optimizers;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Sitecore.Resources.Media;

namespace Mediop.NextGenFormats.WebP
{
	/// <summary>
	/// Generates thumbnails for WebP media by running cwebp, since Sitecore's GDI based generator
	/// cannot decode WebP.
	/// </summary>
	public class WebPThumbnailGenerator : ThumbnailGenerator
	{
		public override MediaStream GetStream(MediaData mediaData, TransformationOptions options)
		{
			Assert.ArgumentNotNull(mediaData, nameof(mediaData));
			Assert.ArgumentNotNull(options, nameof(options));

			var stream = mediaData.GetStream();
			if (stream == null) return null;

			options = GetOptions(mediaData, options);

			using (stream)
			{
				return GetImageStream(stream, options);
			}
		}

		private MediaStream GetImageStream(MediaStream stream, TransformationOptions options)
		{
			var mediaOptions = new MediaOptions
			{
				AllowStretch = options.AllowStretch,
				BackgroundColor = options.BackgroundColor,
				IgnoreAspectRatio = options.IgnoreAspectRatio,
				Scale = options.Scale,
				Width = options.Size.Width,
				Height = options.Size.Height,
				MaxWidth = options.MaxSize.Width,
				MaxHeight = options.MaxSize.Height
			};

			mediaOptions.CustomOptions["extension"] = "webp";

			var args = new OptimizerArgs(stream.Stream, mediaOptions, stream.MediaItem.MediaPath);

			CorePipeline.Run("mediopOptimizeWebP", args);

			return args.IsOptimized ? new MediaStream(args.Stream, args.Extension, stream.MediaItem) : null;
		}

		private TransformationOptions GetOptions(MediaData mediaData, TransformationOptions options)
		{
			options = options.Clone();

			if (options.Size.IsEmpty && options.Scale == 0.0f)
			{
				options.Size = MediaManager.Config.GetThumbnailSize(mediaData.Extension);
			}

			options.PreserveResolution = false;

			return options;
		}
	}
}
