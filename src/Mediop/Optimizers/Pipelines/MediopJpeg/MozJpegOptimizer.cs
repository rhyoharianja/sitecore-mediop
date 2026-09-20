namespace Mediop.Optimizers.Pipelines.MediopJpeg
{
	/// <summary>
	/// mozjpeg - cjpeg for lossy re-encoding, jpegtran for lossless rewriting.
	/// Both accept "-outfile out in", so one class covers each.
	/// </summary>
	public class MozJpegOptimizer : CommandLineToolOptimizer
	{
		protected override string CreateToolArguments(string tempFilePath, string tempOutputPath)
		{
			return $"-outfile \"{tempOutputPath}\" \"{tempFilePath}\"";
		}
	}
}
