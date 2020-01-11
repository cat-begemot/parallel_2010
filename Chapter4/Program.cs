using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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
	        UsingCustomScheduler();
			
			Console.WriteLine("Press enter to finish.");
			Console.ReadLine();
		}

		private static void UsingCustomScheduler()
		{
			int procCount = System.Environment.ProcessorCount;
			var scheduler = new CustomScheduler(procCount);

			Console.WriteLine($"Custom shceduler Id: {scheduler.Id}");
			Console.WriteLine($"Default scheduler Id: {TaskScheduler.Default.Id}");

			var tokenSource = new CancellationTokenSource();

			var task1 = new Task(() =>
			{
				Console.WriteLine($"task1 {Task.CurrentId} executed by scheduler {TaskScheduler.Current.Id}");

				Task.Factory.StartNew(() =>
				{
					Console.WriteLine($"task1-1 {Task.CurrentId} executed by scheduler {TaskScheduler.Current.Id}");
				});

				Task.Factory.StartNew(() =>
				{
					Console.WriteLine($"task1-2 {Task.CurrentId} executed by scheduler {TaskScheduler.Current.Id}");
				}, tokenSource.Token, TaskCreationOptions.None, TaskScheduler.Default);
			});

			task1.Start(scheduler);

			task1.ContinueWith(antecedent =>
			{
				Console.WriteLine($"task3 {Task.CurrentId} executed by scheduler {TaskScheduler.Current.Id}");
			});

			task1.ContinueWith(antecedent =>
			{
				Console.WriteLine($"task4 {Task.CurrentId} executed by scheduler {TaskScheduler.Current.Id}");
			}, scheduler);
		}

		#region Producer/Consumer Pattern

		private static void UsingMultipleBlockingCollectionInstances()
		{
			var bc1 = new BlockingCollection<string>();
			var bc2 = new BlockingCollection<string>();
			var bc3 = new BlockingCollection<string>();

			var bc1And2 = new [] { bc1, bc2 };
			var bcAll = new [] { bc1, bc2, bc3 };

			var tokenSource = new CancellationTokenSource();

			for (int i = 0; i < 5; i++)
			{
				Task.Factory.StartNew(() =>
				{
					while (!tokenSource.IsCancellationRequested)
					{
						var message = string.Format($"Message from task {Task.CurrentId}");
						
						BlockingCollection<string>.AddToAny(bc1And2, message, tokenSource.Token);
						tokenSource.Token.WaitHandle.WaitOne(1000);
					}
				}, tokenSource.Token);
			}

			for (int j = 0; j < 3; j++)
			{
				Task.Factory.StartNew(() =>
				{
					while (!tokenSource.IsCancellationRequested)
					{
						int bcId =
							BlockingCollection<string>
								.TakeFromAny(bcAll, out var item, tokenSource.Token);

						Console.WriteLine($"From collection {bcId}: {item}");
					}
				}, tokenSource.Token);
			}

			Console.WriteLine("Press enter to cancel tasks...");
			Console.ReadLine();
			tokenSource.Cancel();
		}

		private static void ProducerConsumerPatter()
		{
			var blockingCollection = new BlockingCollection<Deposit>();

			var producers = new Task[3];
			for (int i = 0; i < producers.Length; i++)
			{
				producers[i] = Task.Factory.StartNew(() =>
				{
					for (int j = 0; j < 20; j++)
					{
						var deposit = new Deposit() { Amount = 100 };
						blockingCollection.Add(deposit);
					}
				});
			}

			Task.Factory.ContinueWhenAll(producers, antecedents =>
			{
				Console.WriteLine("Signalling production end.");
				blockingCollection.CompleteAdding();
			});

			var bankAccount = new BankAccount();
			var consumer = Task.Factory.StartNew(() =>
			{
				while (!blockingCollection.IsCompleted)
				{
					if (blockingCollection.TryTake(out var deposit))
						bankAccount.Balance += deposit.Amount;
				}

				Console.WriteLine($"Final balance: {bankAccount.Balance}");
			});

			consumer.Wait();
		}

		#endregion

		#region Primitives

		private static void UseSemaphoreSlim()
		{
			var semaphore = new SemaphoreSlim(2);
			var tokenSource = new CancellationTokenSource();

			for (int i = 0; i < 10; i++)
			{
				Task.Factory.StartNew(() =>
				{
					while (true)
					{
						semaphore.Wait(tokenSource.Token);
						Console.WriteLine($"Task {Task.CurrentId} released.");
					}
				}, tokenSource.Token);
			}

			var signallingTask = Task.Factory.StartNew(() =>
			{
				while (!tokenSource.Token.IsCancellationRequested)
				{
					tokenSource.Token.WaitHandle.WaitOne(500);
					semaphore.Release(2);

					Console.WriteLine("Semaphore released.");
				}
			}, tokenSource.Token);

			Console.WriteLine("Press enter to cancel tasks");
			Console.ReadLine();
			tokenSource.Cancel();
		}

		private static void UsingManualResetEventSlim()
		{
			var manualResetEvent = new ManualResetEventSlim();
			var tokenSource = new CancellationTokenSource();

			var waitingTask = Task.Factory.StartNew(() =>
			{
				while (true)
				{
					manualResetEvent.Wait(tokenSource.Token);
					Console.WriteLine("Waiting task is active.");
				}
			}, tokenSource.Token);

			var signalingTask = Task.Factory.StartNew(() =>
			{
				var random = new Random();
				while (!tokenSource.Token.IsCancellationRequested)
				{
					tokenSource.Token.WaitHandle.WaitOne(random.Next(500, 1000));
					manualResetEvent.Set();
					Console.WriteLine("Event set");
					tokenSource.Token.WaitHandle.WaitOne(random.Next(500, 1000));
					manualResetEvent.Reset();
					Console.WriteLine("Event reset!");
				}

				tokenSource.Token.ThrowIfCancellationRequested();
			}, tokenSource.Token);

			Console.WriteLine("Press enter to cancel tasks");
			Console.ReadLine();

			tokenSource.Cancel();

			try
			{
				Task.WaitAll(waitingTask, signalingTask);
			}
			catch(AggregateException)
			{ }
		}

        private static void UsingCountDownEventPrimitive()
		{
			var cdEvent = new CountdownEvent(5);
			var random = new Random();
			var tasks = new Task[6];

			for (int i = 0; i < tasks.Length - 1; i++)
			{
				tasks[i] = new Task(() =>
				{
					Thread.Sleep(random.Next(1000, 3000));
					cdEvent.Signal();
					Console.WriteLine($"Task {Task.CurrentId} signaling event.");
				});
			}

			tasks[5] = new Task(() =>
			{
				Console.WriteLine("Rendezvous task waiting...");
				cdEvent.Wait();
				Console.WriteLine("Event has been set");
			});

			foreach(var task in tasks)
				task.Start();

			Task.WaitAll(tasks);
		}

		private static void UsingBarrierPrimitive()
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

		#endregion

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
