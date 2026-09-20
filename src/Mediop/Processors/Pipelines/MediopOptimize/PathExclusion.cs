using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mediop.Processors.Pipelines.MediopOptimize
{
	/// <summary>
	/// Skips optimization for media under configured media paths. Wildcards (*) are supported.
	/// </summary>
	public class PathExclusion : MediopOptimizeProcessor
	{
		private readonly List<Regex> _excludedPaths = new List<Regex>();

		public void AddExclusion(string mediaPath)
		{
			if (string.IsNullOrWhiteSpace(mediaPath)) return;

			_excludedPaths.Add(new Regex(WildCardToRegular(mediaPath), RegexOptions.IgnoreCase | RegexOptions.Compiled));
		}

		protected override void ProcessOptimize(ProcessorArgs args)
		{
			if (IsExcluded(args.InputStream.MediaItem.MediaPath))
			{
				args.AbortPipeline();
			}
		}

		public bool IsExcluded(string mediaPath)
		{
			if (string.IsNullOrEmpty(mediaPath)) return false;

			foreach (var pattern in _excludedPaths)
			{
				if (pattern.IsMatch(mediaPath)) return true;
			}

			return false;
		}

		private static string WildCardToRegular(string value)
		{
			return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
		}
	}
}
