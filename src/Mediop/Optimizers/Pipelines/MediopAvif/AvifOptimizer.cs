namespace Mediop.Optimizers.Pipelines.MediopAvif
{
	/// <summary>
	/// avifenc from libavif. Smaller than WebP but noticeably slower to encode, so on CD keep it
	/// behind the async strategy.
	/// </summary>
	public class AvifOptimizer : NextGenFormatOptimizer
	{
		public override string Extension => "avif";

		protected override string CreateToolArguments(string tempFilePath, string tempOutputPath)
		{
			return $"\"{tempFilePath}\" \"{tempOutputPath}\"";
		}

		/// <summary>avifenc has no resize switch; Sitecore has already resized the source by this point.</summary>
		protected override string CreateResizeArguments(int width, int height)
		{
			return null;
		}
	}
}
