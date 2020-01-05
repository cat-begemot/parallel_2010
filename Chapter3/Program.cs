using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chapter3
{
	internal partial class Program
    {
	    public static void Main(string[] args)
        {
	        UsingConcurrentDictionary();	

			Console.WriteLine("Press enter to finish.");
            Console.ReadLine();
        }

		#region Collection

		private static void UsingConcurrentDictionary()
		{
			var bankAccount = new BankAccount();
			var sharedCollection = new ConcurrentDictionary<object, int>();
			var tasks = new Task<int>[10];

			for (int i = 0; i < tasks.Length; i++)
			{
				sharedCollection.TryAdd(i, bankAccount.Balance);

				tasks[i] = new Task<int>(keyObj =>
				{
					int currentValue;

					for (int j = 0; j < 1000; j++)
					{
						sharedCollection.TryGetValue(keyObj, out currentValue);
						sharedCollection.TryUpdate(keyObj, currentValue++, currentValue);
					}
					
					var isValueGot = sharedCollection.TryGetValue(keyObj, out var result);

					if (isValueGot)
						return result;
		
					throw new Exception(string.Format($"No data item available for key {keyObj}"));
				}, i);

				tasks[i].Start();
			}

			foreach (var task in tasks)
				bankAccount.Balance += task.Result;

			Console.WriteLine($"Expected value: 10 000. Actual value: {bankAccount.Balance}");
		}

		private static void UsingConcurrentBag()
		{
			var sharedCollection = new ConcurrentBag<int>();
			for (int i = 0; i < 1000; i++)
			{
				sharedCollection.Add(i);
			}

			int itemCount = 0;
			var tasks = new Task[10];

			for (int i = 0; i < tasks.Length; i++)
			{
				tasks[i] = new Task(() =>
				{
					while (sharedCollection.Count > 0)
					{
						int item;
						bool isItemGot = sharedCollection.TryTake(out item);

						if (isItemGot)
							Interlocked.Increment(ref itemCount);
					}
				});

				tasks[i].Start();
			}

			Task.WaitAll(tasks);

			Console.WriteLine($"Items of {sharedCollection.GetType()} processed: {itemCount}");
		}

		private static void UsingConcurrentStack()
		{
			var sharedCollection = new ConcurrentStack<int>();
			for (int i = 0; i < 1000; i++)
			{
				sharedCollection.Push(i);
			}

			int itemCount = 0;
			var tasks = new Task[10];

			for (int i = 0; i < tasks.Length; i++)
			{
				tasks[i] = new Task(() =>
				{
					while (sharedCollection.Count > 0)
					{
						int item;
						bool isItemGot = sharedCollection.TryPop(out item);

						if (isItemGot)
							Interlocked.Increment(ref itemCount);
					}
				});

				tasks[i].Start();
			}

			Task.WaitAll(tasks);

			Console.WriteLine($"Items of {sharedCollection.GetType()} processed: {itemCount}");
		}

		private static void UsingConcurrentQueue()
	    {
			var sharedQueue = new ConcurrentQueue<int>();
			for (int i = 0; i < 1000; i++)
			{
				sharedQueue.Enqueue(i);
			}
			
			int itemCount = 0;
			var tasks = new Task[10];

			for (int i = 0; i < tasks.Length; i++)
			{
				tasks[i] = new Task(() =>
				{
					while(sharedQueue.Count > 0)
					{
						int item;
						bool isItemGot = sharedQueue.TryDequeue(out item);
						
						if(isItemGot)
							Interlocked.Increment(ref itemCount);
					}
				});
				
				tasks[i].Start();
			}

			Task.WaitAll(tasks);

			Console.WriteLine($"Items processed: {itemCount}");
	    }

	    #endregion

		#region Shared Data

		private static void UsingReaderWriterLockSlim()
		{
			var rwLock = new ReaderWriterLockSlim();
			var tokenSource = new CancellationTokenSource();
			var tasks = new Task[5];

			for (int i = 0; i < 5; i++)
			{
				tasks[i] = new Task(() =>
				{
					while(true)
					{
						rwLock.EnterReadLock();
						Console.WriteLine($"Read lock acquired - count: {rwLock.CurrentReadCount}");
						tokenSource.Token.WaitHandle.WaitOne(1000);

						rwLock.ExitReadLock();
						Console.WriteLine($"Read lock released - count: {rwLock.CurrentReadCount}");

						tokenSource.Token.ThrowIfCancellationRequested();
					}
				}, tokenSource.Token);

				tasks[i].Start();
			}

			Console.WriteLine("Press enter to acquire write lock.");
			Console.ReadLine();

			Console.WriteLine("Requesting write lock.");
			rwLock.EnterWriteLock();
			Console.WriteLine("Write lock acquired.");
			Console.WriteLine("Press enter to release write lock.");
			Console.ReadLine();
			rwLock.ExitWriteLock();

			tokenSource.Token.WaitHandle.WaitOne(2000);
			tokenSource.Cancel();

			try
			{
				Task.WaitAll();
			}
			catch(AggregateException)
			{ }
		}

		private static void MultipleLocks()
		{
			var bankAccount1 = new BankAccount();
			var bankAccount2 = new BankAccount();

			var mutex1 = new Mutex();
			var mutex2 = new Mutex();

			var task1 = new Task(() =>
			{
				for (int i = 0; i < 1000; i++)
				{
					bool lockAcquired = mutex1.WaitOne();
					try
					{
						bankAccount1.Balance += 1;
					}
					finally
					{
						if (lockAcquired)
							mutex1.ReleaseMutex();
					}
				}				
			});
			var task2 = new Task(() =>
			{
				for (int i = 0; i < 1000; i++)
				{
					bool lockAcquired = mutex2.WaitOne();
					try
					{
						bankAccount2.Balance += 2;
					}
					finally
					{
						if (lockAcquired)
							mutex2.ReleaseMutex();
					}
				}
			});
			var task3 = new Task(() =>
			{
				for (int i = 0; i < 1000; i++)
				{
					bool lockAcquired = Mutex.WaitAll(new WaitHandle[] { mutex1, mutex2 });
					try
					{
						bankAccount1.Balance--;
						bankAccount2.Balance++;
					}
					finally
					{
						if (lockAcquired)
						{
							mutex1.ReleaseMutex();
							mutex2.ReleaseMutex();
						}
					}
				}
			});

			task1.Start();
			task2.Start();
			task3.Start();

			Task.WaitAll(task1, task2, task3);

			Console.WriteLine($"Account1: {bankAccount1.Balance}.\nAccount2: {bankAccount2.Balance}");
		}

		private static void UsingMutex()
		{
			var account = new BankAccount();
			var mutex = new Mutex();
			var tasks = new Task[10];

			for (var i = 0; i < 10; i++)
			{
				tasks[i] = new Task(() =>
				{
					var startBalance = account.Balance;
					var localBalance = startBalance;

					for (var j = 0; j < 1000; j++)
					{
						var lockAcquired = mutex.WaitOne();
						try
						{
							account.Balance++;
						}
						finally
						{
							if (lockAcquired)
								mutex.ReleaseMutex();
						}
					}
				});

				tasks[i].Start();
			}

			Task.WaitAll(tasks);

			Console.WriteLine($"Expected value: -10 000. Balance: {account.Balance}");
		}

		private static void UsingSpinLock()
		{
			var account = new BankAccount();
			var tasks = new Task[10];
			var spinLock = new SpinLock();

			for (var i = 0; i < 10; i++)
			{
				tasks[i] = new Task(() =>
				{
					var startBalance = account.Balance;
					var localBalance = startBalance;

					for (var j = 0; j < 1000; j++)
					{
						var lockAcquired = false;
						try
						{
							spinLock.Enter(ref lockAcquired);
							account.Balance++;
						}
						finally
						{
							if (lockAcquired)
								spinLock.Exit();
						}
					}
				});

				tasks[i].Start();
			}

			Task.WaitAll(tasks);

			Console.WriteLine($"Expected value: 10 000. Balance: {account.Balance}");
		}

		private static void ConvergentIsolation()
		{
			var account = new BankAccount();
			var tasks = new Task[10];

			for (var i = 0; i < 10; i++)
			{
				tasks[i] = new Task(() =>
				{
					var startBalance = account.Balance;
					var localBalance = startBalance;

					for (var j = 0; j < 1000; j++)
						localBalance++;

					var sharedData = Interlocked.CompareExchange(
						ref account.Balance, localBalance, startBalance);

					if (sharedData == startBalance)
						Console.WriteLine("Shared data updated OK");
					else
						Console.WriteLine("Shared data changed");
				});

				tasks[i].Start();
			}

			Task.WaitAll(tasks);

			Console.WriteLine($"Expected value: -10 000. Balance: {account.Balance}");
		}

		private static void UsingInterlockingOperations()
		{
			var account = new BankAccount();
			var incrementTasks = new Task[5];
			var decrementTasks = new Task[5];

			for (var i = 0; i < 5; i++)
			{
				incrementTasks[i] = new Task(() =>
				{
					for (var j = 0; j < 1000; j++)
						Interlocked.Increment(ref account.Balance);
				});

				incrementTasks[i].Start();
			}

			for (var i = 0; i < 5; i++)
			{
				decrementTasks[i] = new Task(() =>
				{
					for (var j = 0; j < 1000; j++)
					{
						Interlocked.Decrement(ref account.Balance);
						Interlocked.Decrement(ref account.Balance);
					}
				});

				decrementTasks[i].Start();
			}

			Task.WaitAll(incrementTasks);
			Task.WaitAll(decrementTasks);

			Console.WriteLine($"Expected value: -5 000. Balance: {account.Balance}");
		}

	    private static void TwoCriticalRegions()
	    {
		    var account = new BankAccount();
		    var incrementTasks = new Task[5];
		    var decrementTasks = new Task[5];
		    var lockObject = new object();

		    for (var i = 0; i < 5; i++)
		    {
			    incrementTasks[i] = new Task(() =>
			    {
				    for (var j = 0; j < 1000; j++)
					    lock (lockObject)
						    account.Balance++;
			    });

			    incrementTasks[i].Start();
		    }

		    for (var i = 0; i < 5; i++)
		    {
			    decrementTasks[i] = new Task(() =>
			    {
				    for (var j = 0; j < 1000; j++)
					    lock (lockObject)
					    {
						    account.Balance--;
						    account.Balance--;
					    }
			    });

			    decrementTasks[i].Start();
		    }

			Task.WaitAll(incrementTasks);
			Task.WaitAll(decrementTasks);

		    Console.WriteLine($"Expected value: -5 000. Balance: {account.Balance}");
	    }

	    private static void ApplyingLockKeyword()
		{
			var account = new BankAccount();
			var tasks = new Task[10];
			var lockObject = new object();

			for (var i = 0; i < 10; i++)
			{
				tasks[i] = new Task(() =>
				{
					for (var j = 0; j < 1000; j++)
						lock (lockObject)
							account.Balance++;
				});

				tasks[i].Start();
			}

			Task.WaitAll(tasks);

			Console.WriteLine($"Expected value: 10000. Balance: {account.Balance}");
		}

	    private static void Initial()
        {
	        var account = new BankAccount();
	        var tasks = new Task<int>[10];
	        var tls = new ThreadLocal<int>(() =>
	        {
		        Console.WriteLine($"Value factory called for value: {account.Balance}");

		        return account.Balance;
	        });

	        for (var i = 0; i < 10; i++)
	        {
		        tasks[i] = new Task<int>(() =>
		        {
			        for (var j = 0; j < 1000; j++)
				        tls.Value++;

			        return tls.Value;
		        });

		        tasks[i].Start();
	        }

	        for (var i = 0; i < 10; i++)
		        account.Balance += tasks[i].Result;

	        Console.WriteLine($"Expected value: 10000. Balance: {account.Balance}");
        }

	    #endregion
    }
}
