using System;
using System.ComponentModel;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Chapter2
{
    public class Program
    {
        public static void Main(string[] args)
        {
	        LazyTaskExecution();

			Console.WriteLine("Main method complete. Press enter to finish.");
	        Console.ReadLine();
        }

		private static void LazyTaskExecution()
		{
			var taskBody = new Func<string>(() =>
			{
				Console.WriteLine("Task body working...");
				return "Task Result";
			});

			var lazyData = new Lazy<Task<string>>(() => Task.Factory.StartNew(taskBody));
			Console.WriteLine($"Calling lazy variable: {lazyData.Value.Result}");

			var lazyData2 = new Lazy<Task<string>>(() => Task.Factory.StartNew(() =>
			{
				Console.WriteLine("Task body working...");
				return "Task Result 2";
			}));

			Console.WriteLine($"Calling lazy variable: {lazyData2.Value.Result}");
		}

		private static void CustomEscalationPolicy()
		{
			TaskScheduler.UnobservedTaskException += (object sender, UnobservedTaskExceptionEventArgs eventArgs) =>
			{
				eventArgs.SetObserved();
				eventArgs.Exception.Handle(ex =>
				{
					Console.WriteLine($"Exception type: {ex.GetType()}");
					return true;
				});
			};

			var task1 = new Task(() => throw new NullReferenceException());
			Console.WriteLine(task1.Status);
			var task2 = new Task(() => throw new ArgumentOutOfRangeException());

			task1.Start();
			Console.WriteLine(task1.Status);
			task2.Start();
			Console.WriteLine(task1.Status);
			while (!task1.IsCompleted || !task2.IsCompleted)
				Thread.Sleep(500);
			Console.WriteLine(task1.Status);
		}

		private static void ReadingTaskProperties()
		{
			var tokenSource = new CancellationTokenSource();

			var task1 = new Task(() => throw new NullReferenceException());

			var task2 = new Task(() =>
			{
				tokenSource.Token.WaitHandle.WaitOne(-1);
				throw new OperationCanceledException();
			}, tokenSource.Token);

			task1.Start();
			task2.Start();
			tokenSource.Cancel();

			try
			{
				Task.WaitAll(task1, task2);
			}
			catch(AggregateException)
			{ }

			Console.WriteLine($"Task #1 completed: {task1.IsCompleted}");
			Console.WriteLine($"Task #1 faulted: {task1.IsFaulted}");
			Console.WriteLine($"Task #1 cancelled: {task1.IsCanceled}");
			Console.WriteLine(task1.Exception);

			Console.WriteLine();

			Console.WriteLine($"Task #2 completed: {task2.IsCompleted}");
			Console.WriteLine($"Task #2 faulted: {task2.IsFaulted}");
			Console.WriteLine($"Task #2 cancelled: {task2.IsCanceled}");
			Console.WriteLine(task1.Exception);
		}

		private static void UsingIterativeExceptionHandler()
		{
			var tokenSource = new CancellationTokenSource();
			var token = tokenSource.Token;

			var task1 = new Task(() =>
			{
				token.WaitHandle.WaitOne(-1);
				throw new OperationCanceledException();
			}, token);

			var task2 = new Task(() => throw new NullReferenceException());

			task1.Start();
			task2.Start();

			tokenSource.Cancel();

			try
			{
				Task.WaitAll(task1, task2);
			}
			catch(AggregateException ex)
			{
				ex.Handle(inner =>
				{
					if (inner is OperationCanceledException)
					{
						Console.WriteLine(inner.GetType());
						return true;
					}

					if(inner is NullReferenceException)
					{
						Console.WriteLine(inner.GetType());
						return true;
					}

					Console.WriteLine($"Do not know about this kind of exception: {inner.GetType()}");
					return false;
				});
			}
		}

		private static void BasicExceptionHandling()
		{
			var task1 = new Task(()=>
			{
				var exception = new ArgumentOutOfRangeException();
				exception.Source = "task1";
				throw exception;
			});

			var task2 = new Task(()=>
			{
				throw new NullReferenceException();
			});

			var task3 = new Task(()=>
			{
				Console.WriteLine("Hello from task3!");
			});

			task1.Start();
			task2.Start();
			task3.Start();

			try
			{
				Task.WaitAll(task1, task2, task3);
			}
			catch (AggregateException e)
			{
				foreach(var inner in e.InnerExceptions)
					Console.WriteLine($"Exception type: {inner.GetType()} from {inner.Source}");
			}
		}

		private static void WaitForSingleTask()
		{
			var tokenSource = new CancellationTokenSource();
			var token = tokenSource.Token;

			var task = CreateTask(token);
			task.Start();
			Console.WriteLine("Waiting for task complete");
			task.Wait();
			Console.WriteLine("Task completed.");

			task = CreateTask(token);
			task.Start();
			Console.WriteLine("Waiting 2 secs for task to complete");
			bool completed = task.Wait(2000);
			Console.WriteLine($"Wait ended - task completed: {completed}");

			task = CreateTask(token);
			task.Start();
			Console.WriteLine("Wait 2 secs for task to complete.");
			completed = task.Wait(2000, token);
			Console.WriteLine($"Wait ended - tasl completed: {completed}, task cancelled: {task.IsCanceled}");
		}

		private static Task CreateTask(CancellationToken token)
		{
			return new Task(() =>
			{
				for (int i = 0; i < 5; i++)
				{
					token.ThrowIfCancellationRequested();
					Console.WriteLine($"Task - Int value {i}");
					token.WaitHandle.WaitOne(1000);
				}
			});
		}

        private static void PuttingTaskToSleep()
        {
			var tokenSource = new CancellationTokenSource();
			var token = tokenSource.Token;
			var task = new Task(() =>
			{
				for (int i = 0; i < int.MaxValue; i++)
				{
					var cancelled = token.WaitHandle.WaitOne(10000);
					Console.WriteLine($"Task value: {i}. Cancelled? {cancelled}");

					if(cancelled)
						throw new OperationCanceledException();
				}
			});
			task.Start();

			Console.WriteLine("Press enter to cancell token.");
			Console.ReadLine();

			tokenSource.Cancel();
        }

        private static void UseIsCancelledProperty()
        {
	        var tokenSource1 = new CancellationTokenSource();
	        var token1 = tokenSource1.Token;
	        var task1 = new Task(() =>
	        {
		        for (int i = 0; i < int.MaxValue; i++)
		        {
					token1.ThrowIfCancellationRequested();
			        Console.WriteLine($"Task1 - Int value: {i}");
		        }
	        }, token1);

	        var tokenSource2 = new CancellationTokenSource();
	        var token2 = tokenSource2.Token;
	        var task2 = new Task(() =>
	        {
		        for (int i = 0; i < int.MaxValue; i++)
		        {
			        token2.ThrowIfCancellationRequested();
			        Console.WriteLine($"Task2 - Int value: {i}");
		        }
	        }, token2);

	        task1.Start();
	        task2.Start();

	        tokenSource2.Cancel();

			Console.WriteLine($"Is Task1 cancelled? {task1.IsCanceled}");
			Console.WriteLine($"Is Task2 cancelled? {task2.IsCanceled}");
        }

		private static void CancellationMonitoringWithWaitHandle()
		{
			var tokenSource = new CancellationTokenSource();
			var token = tokenSource.Token;
			var task1 = new Task(() =>
			{
				for (int i = 0; i < int.MaxValue; i++)
				{
					if (token.IsCancellationRequested)
					{
						Console.WriteLine("Task cancel detected.");
						throw new OperationCanceledException();
					}
					else
					{
						Console.WriteLine($"Int value: {i}");
					}
				}
			}, token);

			var task2 = new Task(() =>
			{
				token.WaitHandle.WaitOne();
				Console.WriteLine(">>>>> Wait handle released.");
			});

			Console.WriteLine("Press enter to start task.");
			Console.WriteLine("Press enter again to cancel task.");
			Console.ReadLine();

			task1.Start();
			task2.Start();
			Console.ReadLine();

			Console.WriteLine("Cancelling task.");
			tokenSource.Cancel();
		}

        private static void MonitoringCancellationWithDelegate()
        {
			var tokenSource = new CancellationTokenSource();
			var token = tokenSource.Token;
			var task = new Task(() =>
			{
				for (int i = 0; i < int.MaxValue; i++)
				{
					if (token.IsCancellationRequested)
					{
						Console.WriteLine("Task cancel detected.");
						throw new OperationCanceledException();
					}
					else
					{
						Console.WriteLine($"Int value: {i}");
					}
				}
			}, token);

			token.Register(() =>
			{
				Console.WriteLine(">>>> Delegate was invoked.\n");
			});

			Console.WriteLine("Press enter to start task.");
			Console.WriteLine("Press enter again to cancel task.");

			task.Start();

			Console.ReadLine();
			Console.WriteLine("Cancelling task.");
			tokenSource.Cancel();
		}

        private static void CreateCancellableTaskAndPollingToCheckCancellation()
        {
	        var tokenSource = new CancellationTokenSource();
	        var token = tokenSource.Token;
			var task = new Task(() =>
			{
				for (int i = 0; i < int.MaxValue; i++)
				{
					if (token.IsCancellationRequested)
					{
						Console.WriteLine("Task cancel detected.");
						throw new OperationCanceledException();
					}
					else
					{
						Console.WriteLine($"Int value: {i}");
					}
				}
			}, token);

			Console.WriteLine("Press enter to start task.");
			Console.WriteLine("Press enter again to cancel task.");

			task.Start();

			Console.ReadLine();
			Console.WriteLine("Cancelling task.");
			tokenSource.Cancel();
        }

        private static void TwoParallelTaskInvoke()
        {
	        var task1 = new Task<int>(() =>
	        {
		        int sum = 0;
		        for (int i = 0; i < 100; i++)
			        sum += i;

		        return sum;
	        }, TaskCreationOptions.None);

	        task1.Start();
	        Console.WriteLine($"Result 1: {task1.Result}");

	        var task2 = new Task<int>(obj =>
	        {
		        int sum = 0;
		        int max = (int)obj;
		        for (int i = 0; i < max; i++)
			        sum += i;

		        return sum;
	        }, 100, TaskCreationOptions.None);

	        task2.Start();
	        Console.WriteLine($"Result 2: {task2.Result}");
		}

        private static void PrintMessage(object message)
        {
	        Console.WriteLine($"Message: {message}");
        }
    }
}
