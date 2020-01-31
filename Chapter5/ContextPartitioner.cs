using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Chapter5
{
	public class ContextPartitioner : Partitioner<WorkItem>
	{
//		protected WorkItem[] dataItems;
//		protected int targetSum;
//		private long sharedStartIndex = 0;
//		private object lockObj = new object();
//		private EnumerableSource enumSource;
//
//		public ContextPartitioner(WorkItem[] data, int target)
//		{
//			dataItems = data;
//			targetSum = target;
//			enumSource = new EnumerableSource(this);
//		}
//
//		public override bool SupportsDynamicPartitions => true;
//
//		public override IList<IEnumerator<WorkItem>> GetPartitions(int partitionCount)
//		{
//			IList<IEnumerator<WorkItem>> partitionsList = new List<IEnumerator<WorkItem>>();
//		}
		public override IList<IEnumerator<WorkItem>> GetPartitions(int partitionCount)
		{
			throw new NotImplementedException();
		}
	}
}