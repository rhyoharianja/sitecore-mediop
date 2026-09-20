namespace Mediop.NextGenFormats.Pipelines
{
	/// <summary>
	/// Adds an extension to the supported set when the Accept header mentions its MIME type.
	/// One processor per format, configured in mediopGetSupportedFormats.
	/// </summary>
	public class CheckSupport
	{
		public virtual string Extension { get; set; }

		public void Process(SupportedFormatsArgs args)
		{
			if (string.IsNullOrEmpty(args.Input) || string.IsNullOrEmpty(Extension)) return;

			if (args.Input.IndexOf($"{args.Prefix}{Extension}", System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				args.Extensions.Add(Extension);
			}
		}
	}
}
