namespace Mediop.Optimizers.Pipelines.MediopJxl
{
	/// <summary>
	/// cjxl from libjxl. Browser support is still thin, so this stays disabled by default.
	/// </summary>
	public class JxlOptimizer : NextGenFormatOptimizer
	{
		public override string Extension => "jxl";

		protected override string CreateToolArguments(string tempFilePath, string tempOutputPath)
		{
			return $"\"{tempFilePath}\" \"{tempOutputPath}\"";
		}

		protected override string CreateResizeArguments(int width, int height)
		{
			return null;
		}
	}
}
