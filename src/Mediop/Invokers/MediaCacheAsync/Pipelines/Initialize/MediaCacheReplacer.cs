using Sitecore.Configuration;
using Sitecore.Pipelines;
using Sitecore.Resources.Media;

namespace Mediop.Invokers.MediaCacheAsync.Pipelines.Initialize
{
	/// <summary>
	/// Swaps the Sitecore media cache for the optimizing one during initialize.
	/// </summary>
	public class MediaCacheReplacer
	{
		/// <summary>
		/// Background encoder threads. Defaults to 1: image encoders are CPU bound and run alongside
		/// request handling, so more threads mostly means slower pages.
		/// </summary>
		public int MaxConcurrentThreads { get; set; } = Settings.GetIntSetting("Mediop.Async.MaxConcurrentThreads", 1);

		/// <summary>How many pending optimizations to hold before dropping new ones.</summary>
		public int MaxQueueLength { get; set; } = Settings.GetIntSetting("Mediop.Async.MaxQueueLength", 500);

		public virtual void Process(PipelineArgs args)
		{
			var queue = new OptimizationQueue(MaxConcurrentThreads, MaxQueueLength);

			MediaManager.Cache = new OptimizingMediaCache(new MediaOptimizer(), queue);

			MediopLog.Info($"Mediop: installed the optimizing media cache for async optimization ({queue.MaxConcurrentThreads} worker threads, queue limit {queue.MaxQueueLength}).");
		}
	}
}
