namespace Mediop.Optimizers.Pipelines.MediopWebP
{
	/// <summary>
	/// cwebp (and gif2webp for animated GIFs) from libwebp.
	/// </summary>
	public class WebPOptimizer : NextGenFormatOptimizer
	{
		public override string Extension => "webp";

		protected override string CreateToolArguments(string tempFilePath, string tempOutputPath)
		{
			return $"\"{tempFilePath}\" -o \"{tempOutputPath}\"";
		}

		protected override string CreateResizeArguments(int width, int height)
		{
			return $"-resize {width} {height}";
		}
	}
}
