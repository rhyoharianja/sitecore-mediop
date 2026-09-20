using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Mediop.Invokers.MediaCacheAsync
{
	/// <summary>
	/// Bounded work queue that runs optimization on a small, fixed set of background threads.
	/// The bound is the point: on a busy delivery server a burst of cold media requests must never
	/// spawn an unbounded pile of image encoders competing with request threads for CPU. When the queue
	/// is full the work is dropped and the visitor simply keeps the unoptimized image - the next request
	/// for it will try again.
	/// Replaces the TPL Dataflow ActionBlock the upstream project uses, which drops a NuGet dependency.
	/// </summary>
	public sealed class OptimizationQueue : IDisposable
	{
		private readonly BlockingCollection<Action> _queue;
		private readonly Thread[] _workers;
		private long _droppedCount;
		private bool _disposed;

		public OptimizationQueue(int maxConcurrentThreads, int maxQueueLength)
		{
			if (maxConcurrentThreads < 1) maxConcurrentThreads = 1;
			if (maxQueueLength < 1) maxQueueLength = 1;

			MaxConcurrentThreads = maxConcurrentThreads;
			MaxQueueLength = maxQueueLength;

			_queue = new BlockingCollection<Action>(maxQueueLength);
			_workers = new Thread[maxConcurrentThreads];

			for (var i = 0; i < maxConcurrentThreads; i++)
			{
				_workers[i] = new Thread(Work)
				{
					// background threads do not keep the app pool alive during a recycle
					IsBackground = true,
					Name = $"Mediop optimizer {i + 1}",
					Priority = ThreadPriority.BelowNormal
				};

				_workers[i].Start();
			}
		}

		public int MaxConcurrentThreads { get; }

		public int MaxQueueLength { get; }

		public int QueueLength => _queue.Count;

		public long DroppedCount => Interlocked.Read(ref _droppedCount);

		/// <summary>Queues work. Returns false when the queue is full or shutting down.</summary>
		public bool TryEnqueue(Action work)
		{
			if (work == null) throw new ArgumentNullException(nameof(work));

			if (_disposed) return false;

			try
			{
				if (_queue.TryAdd(work)) return true;
			}
			catch (ObjectDisposedException)
			{
				return false;
			}
			catch (InvalidOperationException)
			{
				// queue was completed while we were adding; treat as a drop
				return false;
			}

			var dropped = Interlocked.Increment(ref _droppedCount);

			// one line per drop would flood the log during a burst, so report on a curve
			if (dropped == 1 || dropped % 100 == 0)
			{
				MediopLog.Warn($"Mediop: optimization queue is full ({MaxQueueLength} items); skipping optimization for this request. {dropped} skipped so far. Raise Mediop.Async.MaxQueueLength or Mediop.Async.MaxConcurrentThreads if this keeps happening.");
			}

			return false;
		}

		private void Work()
		{
			foreach (var work in _queue.GetConsumingEnumerable())
			{
				try
				{
					work();
				}
				catch (Exception ex)
				{
					// an escaping exception on a background thread takes the whole app pool down with it
					MediopLog.Error("Mediop: unhandled exception on the optimization queue.", ex);
				}
			}
		}

		public void Dispose()
		{
			if (_disposed) return;

			_disposed = true;

			_queue.CompleteAdding();

			foreach (var worker in _workers)
			{
				worker.Join(TimeSpan.FromSeconds(5));
			}

			_queue.Dispose();
		}
	}
}
