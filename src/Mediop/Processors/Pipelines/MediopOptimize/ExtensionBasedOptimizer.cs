using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mediop.Optimizers;
using Sitecore.Pipelines;

namespace Mediop.Processors.Pipelines.MediopOptimize
{
	/// <summary>
	/// Dispatches a media stream to a format specific optimizer pipeline based on its file extension.
	/// </summary>
	public class ExtensionBasedOptimizer : MediopOptimizeProcessor
	{
		private HashSet<string> _supportedExtensionsLookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public string Extensions
		{
			get => string.Join(",", _supportedExtensionsLookup);
			set => _supportedExtensionsLookup = new HashSet<string>(value.Split(',').Select(val => val.Trim(',', '.', '*', ' ')), StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>Name of the pipeline that holds the optimizers for these extensions, e.g. mediopOptimizeJpeg.</summary>
		public string Pipeline { get; set; }

		protected override void ProcessOptimize(ProcessorArgs args)
		{
			if (!_supportedExtensionsLookup.Contains(args.InputStream.Extension)) return;

			if (args.ResultStream == null)
			{
				// buffers the stream if it is not seekable (e.g. a SQL blob stream)
				args.InputStream.MakeStreamSeekable();
				args.InputStream.Stream.Seek(0, SeekOrigin.Begin);
			}

			var sourceStream = args.ResultStream ?? args.InputStream.Stream;

			var optimizerArgs = new OptimizerArgs(sourceStream, args.MediaOptions, args.InputStream.MediaItem.MediaPath);

			CorePipeline.Run(Pipeline, optimizerArgs);

			args.IsOptimized = optimizerArgs.IsOptimized;
			args.Extension = optimizerArgs.Extension;
			args.ResultStream = optimizerArgs.Stream;

			if (!string.IsNullOrEmpty(optimizerArgs.Message))
			{
				args.AddMessage(optimizerArgs.Message);
			}

			if (optimizerArgs.Aborted)
			{
				args.AbortPipeline();
			}
		}
	}
}
