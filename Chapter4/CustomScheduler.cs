using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chapter4
{
	public class CustomScheduler : TaskScheduler, IDisposable
	{
		private BlockingCollection<Task> _taskQueue;
		private Thread[] _threads;

		public CustomScheduler(int concurrency)
		{
			_taskQueue = new BlockingCollection<Task>();
			_threads = new Thread[concurrency];

			for (int i = 0; i < _threads.Length; i++)
			{
				(_threads[i] = new Thread(() =>
				{
					foreach (var task in _taskQueue.GetConsumingEnumerable())
					{
						TryExecuteTask(task);
					}
				})).Start();
			}
		}

		protected override void QueueTask(Task task)
		{
			if (task.CreationOptions.HasFlag(TaskCreationOptions.LongRunning))
				new Thread(() => TryExecuteTask(task)).Start();
			else
				_taskQueue.Add(task);
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			if (_threads.Contains(Thread.CurrentThread))
				return TryExecuteTask(task);
			else
				return false;
		}

		public override int MaximumConcurrencyLevel => _threads.Length;

		protected override IEnumerable<Task> GetScheduledTasks() => _taskQueue.ToArray();

		public void Dispose()
		{
			_taskQueue.CompleteAdding();

			foreach (var thread in _threads)
				thread.Join();
		}
	}
}
