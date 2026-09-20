using System;
using System.IO;
using System.Reflection;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Resources.Media;
using Sitecore.Sites;

namespace Mediop.Invokers.MediaCacheAsync
{
	/// <summary>
	/// Media cache that optimizes images on their way into the cache, on a background thread.
	/// The first visitor to request a given size gets the unoptimized (but resized) image immediately;
	/// everyone after them is served the optimized version from cache. This is the strategy to use on
	/// content delivery, because no visitor ever waits for an encoder to run.
	/// </summary>
	public class OptimizingMediaCache : MediaCache
	{
		private static readonly MethodInfo AddToActiveListMethod = typeof(MediaCache).GetMethod("AddToActiveList", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly MethodInfo RemoveFromActiveListMethod = typeof(MediaCache).GetMethod("RemoveFromActiveList", BindingFlags.Instance | BindingFlags.NonPublic);

		private readonly MediaOptimizer _optimizer;
		private readonly OptimizationQueue _queue;

		public OptimizingMediaCache(MediaOptimizer optimizer, OptimizationQueue queue)
		{
			Assert.ArgumentNotNull(optimizer, nameof(optimizer));
			Assert.ArgumentNotNull(queue, nameof(queue));

			_optimizer = optimizer;
			_queue = queue;
		}

		public override bool AddStream(Media media, MediaOptions options, MediaStream stream, out MediaStream cachedStream)
		{
			Assert.ArgumentNotNull(media, nameof(media));
			Assert.ArgumentNotNull(options, nameof(options));
			Assert.ArgumentNotNull(stream, nameof(stream));

			cachedStream = null;

			if (!CanCache(media, options)) return false;

			if (string.IsNullOrEmpty(media.MediaData.MediaId)) return false;

			if (!stream.Stream.CanRead)
			{
				MediopLog.Warn($"Mediop: cannot optimize {media.MediaData.MediaItem.MediaPath} because the cache was handed a non readable stream.");
				return false;
			}

			// buffer the stream in case it is a SQL blob stream, which cannot be read twice
			stream.MakeStreamSeekable();
			stream.Stream.Seek(0, SeekOrigin.Begin);

			// Sitecore streams this to the client while we optimize in the background
			cachedStream = stream;

			// the background thread has no Sitecore context of its own, and without a site context the
			// media cache refuses to store anything
			var currentSite = Context.Site;

			if (_queue.TryEnqueue(() => Optimize(currentSite, media, options, stream))) return true;

			// queue full: nothing is cached this time round, so the next request for this image retries
			cachedStream = null;

			return false;
		}

		private void Optimize(SiteContext currentSite, Media media, MediaOptions options, MediaStream stream)
		{
			var mediaItem = media.MediaData.MediaItem;

			MediaStream originalMediaStream = null;
			MediaStream backupMediaStream = null;
			MediaStream optimizedMediaStream = null;

			try
			{
				using (new SiteContextSwitcher(currentSite))
				{
					// another thread may have beaten us to it
					if (Contains(media, options)) return;

					// re-read from the media item rather than holding a second copy of the response in memory
					using (var mediaItemStream = media.GetStream(options))
					{
						if (mediaItemStream == null) return;

						var originalStream = new MemoryStream();
						mediaItemStream.CopyTo(originalStream);
						originalStream.Seek(0, SeekOrigin.Begin);

						originalMediaStream = new MediaStream(originalStream, stream.Extension, mediaItem);

						// optimization disposes the original stream, so keep a copy to cache if it fails
						var backupStream = new MemoryStream();
						originalStream.CopyTo(backupStream);
						backupStream.Seek(0, SeekOrigin.Begin);
						originalStream.Seek(0, SeekOrigin.Begin);

						backupMediaStream = new MediaStream(backupStream, stream.Extension, mediaItem);
					}

					optimizedMediaStream = _optimizer.Process(originalMediaStream, options);

					if (optimizedMediaStream == null)
					{
						MediopLog.Debug($"Mediop: {mediaItem.MediaPath} was not optimized (media type or path exclusion); caching it unchanged.");
					}

					var cacheRecord = CreateCacheRecord(media, options, optimizedMediaStream ?? backupMediaStream);

					AddToActiveList(cacheRecord);

					try
					{
						cacheRecord.Persist();
					}
					finally
					{
						RemoveFromActiveList(cacheRecord);
					}
				}
			}
			catch (Exception ex)
			{
				MediopLog.Error($"Mediop: exception on the background thread while optimizing {mediaItem.MediaPath}.", ex);
			}
			finally
			{
				originalMediaStream?.Dispose();
				backupMediaStream?.Dispose();
				optimizedMediaStream?.Dispose();
			}
		}

		// the active list lets Sitecore stream media to the client while it is still being written to
		// cache. The rest of MediaCache is virtual, but these two are private, hence the reflection.
		protected virtual void AddToActiveList(MediaCacheRecord record)
		{
			if (AddToActiveListMethod != null) AddToActiveListMethod.Invoke(this, new object[] { record });
			else MediopLog.Error("Mediop: could not find MediaCache.AddToActiveList via reflection. Mediop may not be fully compatible with this Sitecore version, though this only costs a performance optimization.");
		}

		protected virtual void RemoveFromActiveList(MediaCacheRecord record)
		{
			if (RemoveFromActiveListMethod != null) RemoveFromActiveListMethod.Invoke(this, new object[] { record });
			else MediopLog.Error("Mediop: could not find MediaCache.RemoveFromActiveList via reflection. Mediop may not be fully compatible with this Sitecore version, though this only costs a performance optimization.");
		}
	}
}
