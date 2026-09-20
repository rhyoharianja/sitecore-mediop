namespace Mediop.Processors.Pipelines.MediopOptimize
{
	public abstract class MediopOptimizeProcessor
	{
		public virtual void Process(ProcessorArgs args)
		{
			ProcessOptimize(args);
		}

		protected abstract void ProcessOptimize(ProcessorArgs args);
	}
}
