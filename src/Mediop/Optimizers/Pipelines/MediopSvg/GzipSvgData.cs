using System.IO;
using System.IO.Compression;

namespace Mediop.Optimizers.Pipelines.MediopSvg
{
	/// <summary>
	/// Gzips the SVG so it lands in the media cache already compressed. Only worth enabling when IIS
	/// dynamic compression is off; pair it with CompressedSvgEncodingSetter so the browser gets the
	/// Content-Encoding header.
	/// </summary>
	public class GzipSvgData : OptimizerProcessor
	{
		protected override void ProcessOptimizer(OptimizerArgs args)
		{
			var output = new MemoryStream();

			using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
			{
				args.Stream.CopyTo(gzip);
			}

			args.Stream.Dispose();

			output.Seek(0, SeekOrigin.Begin);
			args.Stream = output;
			args.IsOptimized = true;
		}
	}
}
