using Sitecore;

namespace Mediop.Processors.Pipelines.MediopOptimize
{
	/// <summary>
	/// Honours the enableMediop="false" attribute on a site definition (also works for SXA site grouping items).
	/// </summary>
	public class DisableMediopForSite : MediopOptimizeProcessor
	{
		protected override void ProcessOptimize(ProcessorArgs args)
		{
			if (Context.Site?.Properties["enableMediop"] == "false")
			{
				args.AbortPipeline();
			}
		}
	}
}
