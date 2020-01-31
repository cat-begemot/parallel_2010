using System.Threading;

namespace Chapter5
{
	public class WorkItem
	{
		public int WorkDuration { get; set; }

		public  void PerformWork()
		{
			Thread.Sleep(WorkDuration);
		}
	}
}