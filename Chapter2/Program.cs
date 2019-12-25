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
	        PuttingTaskToSleep();


			Console.WriteLine("Main method complete. Press enter to finish.");
	        Console.ReadLine();
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
