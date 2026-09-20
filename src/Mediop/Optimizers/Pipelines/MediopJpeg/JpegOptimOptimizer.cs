namespace Mediop.Optimizers.Pipelines.MediopJpeg
{
	/// <summary>
	/// jpegoptim, which optimizes the file in place. Lighter than mozjpeg but saves less.
	/// </summary>
	public class JpegOptimOptimizer : CommandLineToolOptimizer
	{
		protected override bool OptimizerUsesSeparateOutputFile => false;

		protected override string TempFileExtension => ".jpg";

		protected override string CreateToolArguments(string tempFilePath, string tempOutputPath)
		{
			return $"\"{tempFilePath}\"";
		}
	}
}
