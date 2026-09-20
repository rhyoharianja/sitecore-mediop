namespace Mediop.Optimizers.Pipelines.MediopPng
{
	/// <summary>
	/// PngOptimizerCL (http://psydk.org/pngoptimizer) - lossless and by far the fastest of the PNG tools.
	/// It rewrites the file in place and requires a .png extension.
	/// </summary>
	public class PngOptimizer : CommandLineToolOptimizer
	{
		protected override bool OptimizerUsesSeparateOutputFile => false;

		protected override string TempFileExtension => ".png";

		protected override string CreateToolArguments(string tempFilePath, string tempOutputPath)
		{
			return $"-file \"{tempFilePath}\"";
		}
	}
}
