using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Chapter3;

namespace Chapter4
{
    internal class Program
    {
        internal static void Main(string[] args)
        {
	        UsingBarrierClass();

			Console.WriteLine("Press enter to finish.");
			Console.ReadLine();
		}

		private static void UsingBarrierClass()
		{
			var accounts = new BankAccount[5];
			for (int i = 0; i < accounts.Length; i++)
				accounts[i] = new BankAccount();

			int totalBalance = 0;
			var barrier = new Barrier(5, myBarrier =>
			{
				totalBalance = 0;

				foreach (var account in accounts)
					totalBalance += account.Balance;

				Console.WriteLine($"Total balance: {totalBalance}");
			});

			var tasks = new Task[5];
			for (int i = 0; i < tasks.Length; i++)
			{
				tasks[i] = new Task(stateObject =>
				{
					var account = stateObject as BankAccount;
					var random = new Random();

					for (int j = 0; j < 1000; j++)
						account.Balance += random.Next(1, 100);

					Console.WriteLine($"Task {Task.CurrentId} phase {barrier.CurrentPhaseNumber} completed.");
					barrier.SignalAndWait();

					account.Balance -= (totalBalance - account.Balance) / 10;

					Console.WriteLine($"Task {Task.CurrentId} phase {barrier.CurrentPhaseNumber} completed.");
					barrier.SignalAndWait();
				}, accounts[i]);
			}

			foreach(var task in tasks)
				task.Start();

			Task.WaitAll(tasks);
		}

        #region Child tasks

        private static void SimpleChildTask()
		{
			var parentTask = new Task(() =>
			{
				var childTask = new Task(() =>
				{
					Console.WriteLine("Child task is running...");
					Thread.Sleep(1000);
					Console.WriteLine("Child task finished.");
					throw new Exception();
				}, TaskCreationOptions.AttachedToParent);

				childTask.ContinueWith(antecedent =>
				{
					Console.WriteLine("Continuation is running...");
					Thread.Sleep(1000);
					Console.WriteLine("Continuation finished.");
				}, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.OnlyOnFaulted);

				Console.WriteLine("Starting child task...");
				childTask.Start();
			});

			parentTask.Start();

			Console.WriteLine("Waiting for the parent task..");
			
			try
			{
				parentTask.Wait();
			}
			catch(AggregateException ex)
			{
				ex.Handle(innerException =>
				{
					Console.WriteLine($"Exception type: {innerException.GetType()}");

					return true;
				});
			}
			
			Console.WriteLine("Parent task finished.");
		}

        #endregion

        #region Continuation tasks

        private static void ExceptionInContinuationChains()
		{
			var gen1 = new Task(() =>
			{
				Console.WriteLine("First generation task.");
			});

			var gen2 = gen1.ContinueWith(antecedent =>
			{
				Console.WriteLine("Second generation task throws exception.");
				
				throw new Exception();
			});

			var gen3 = gen2.ContinueWith(antecedent =>
			{
				if (antecedent.Status == TaskStatus.Faulted &&
					antecedent.Exception.InnerException != null)
				{
					throw antecedent.Exception.InnerException;
				}
				
				Console.WriteLine("Third generation task.");
			});

			gen1.Start();
			
			try
			{
				gen3.Wait();
			}
			catch(AggregateException ex)
			{
				ex.Handle(inner =>
				{
					Console.WriteLine($"Handled exception of type: {inner.GetType()}");

					return true;
				});
			}
		}

		private static void CancellingContinuation()
		{
			var tokenSource = new CancellationTokenSource();
			
			var mainTask = new Task(() =>
			{
				Console.WriteLine("Antecedent is running...");
				tokenSource.Token.WaitHandle.WaitOne();
				tokenSource.Token.ThrowIfCancellationRequested();
			}, tokenSource.Token);

			var neverScheduledTask = mainTask.ContinueWith(antecedent =>
			{
				Console.WriteLine("This task will never be scheduled");

			}, tokenSource.Token);

			var badSelectiveTask = mainTask.ContinueWith(antecedent =>
			{
				Console.WriteLine("This task will never be scheduled.");
			}, tokenSource.Token, TaskContinuationOptions.OnlyOnCanceled, TaskScheduler.Current);

			var continuation = mainTask.ContinueWith(antecedent =>
			{
				Console.WriteLine("Continuation is running...");
			}, TaskContinuationOptions.OnlyOnCanceled);

			mainTask.Start();

			Console.WriteLine("Press enter to cancel the token");
			Console.ReadLine();
			tokenSource.Cancel();

			continuation.Wait();
		}

		private static void MultitaskContinuation()
		{
			var account = new BankAccount();
			var tasks = new Task[10];

			for (int i = 0; i < tasks.Length; i++)
			{
				tasks[i] = new Task<int>(stateObject =>
				{
					var isolatedAccount = stateObject as BankAccount;

					if (isolatedAccount == null)
						return -1;

					for (int j = 0; j < 1000; j++)
						isolatedAccount.Balance++;

					return isolatedAccount.Balance;
				}, account);
			}

			var continuation = Task.Factory.ContinueWhenAll(tasks, antecedents =>
			{
				foreach (Task<int> t in antecedents)
					account.Balance += t.Result;
			}, TaskContinuationOptions.OnlyOnRanToCompletion);

			foreach(var t in tasks)
				t.Start();

			continuation.Wait();

			Console.WriteLine($"Expected value: 10 000. Actual: {account.Balance}");
		}

		private static void OneToManyContinuation()
		{
			var rootTask = new Task<BankAccount>(() =>
			{
				var bankAccount = new BankAccount();

				for (int i = 0; i < 1000; i++)
					bankAccount.Balance++;

				return bankAccount;
			});

			rootTask
				.ContinueWith<int>((Task<BankAccount> antecedent) =>
				{
					Console.WriteLine($"Interim balance: {antecedent.Result.Balance}");

					return antecedent.Result.Balance * 2;
				}, TaskContinuationOptions.OnlyOnRanToCompletion)
				.ContinueWith((Task<int> antecedent) =>
				{
					Console.WriteLine($"Final balance 1: {antecedent.Result}");
				}, TaskContinuationOptions.OnlyOnRanToCompletion)
				.ContinueWith(Task =>
				{
					Console.WriteLine("Exception is occured.");
				}, TaskContinuationOptions.NotOnRanToCompletion);

			rootTask.Start();

			Task.WaitAll(rootTask);
		}

        private static void InitialExemaple()
        {
	        var task = new Task<BankAccount>(() =>
	        {
		        var account = new BankAccount();

		        for (int i = 0; i < 1000; i++)
		        {
			        account.Balance++;
		        }

		        return account;
	        });

	        var continuationTask = task.ContinueWith<int>((Task<BankAccount> antecedent) =>
	        {
		        Console.WriteLine($"Final balance: {antecedent.Result.Balance}");

				return antecedent.Result.Balance * 2;
	        });

	        task.Start();

	        Console.WriteLine($"Final balance: {continuationTask.Result}");
        }

        #endregion
    }
}
