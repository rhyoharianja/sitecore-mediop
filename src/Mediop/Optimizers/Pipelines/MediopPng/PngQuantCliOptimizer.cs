namespace Mediop.Optimizers.Pipelines.MediopPng
{
	/// <summary>
	/// pngquant - lossy palette quantization. Big savings on flat graphics and screenshots, so it
	/// runs before the lossless pass.
	/// </summary>
	public class PngQuantCliOptimizer : CommandLineToolOptimizer
	{
		private const int QualityTooLow = 99;
		private const int ResultWouldBeLarger = 98;

		protected override string TempFileExtension => ".png";

		/// <summary>
		/// Photographic PNGs routinely fail to quantize within the requested quality range, and pngquant
		/// reports that with an exit code rather than a zero byte file. That is a normal outcome, not an
		/// error, so it must not show up in the log as one.
		/// </summary>
		protected override bool IsSkipExitCode(int exitCode)
		{
			return exitCode == QualityTooLow || exitCode == ResultWouldBeLarger;
		}

		protected override string CreateToolArguments(string tempFilePath, string tempOutputPath)
		{
			return $"--force --output \"{tempOutputPath}\" -- \"{tempFilePath}\"";
		}
	}
}
