namespace Mediop.Optimizers.Pipelines.MediopGif
{
	/// <summary>
	/// gifsicle - lossless GIF optimization that also keeps animation intact.
	/// </summary>
	public class GifsicleOptimizer : CommandLineToolOptimizer
	{
		protected override string TempFileExtension => ".gif";

		protected override string CreateToolArguments(string tempFilePath, string tempOutputPath)
		{
			return $"\"{tempFilePath}\" --output \"{tempOutputPath}\"";
		}
	}
}
