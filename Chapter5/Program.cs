using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Chapter5
{
	internal class Program
    {
        public static void Main(string[] args)
        {
	        UsingOrderablePartitioner();

	        Console.WriteLine("Press enter to finish...");
	        Console.ReadLine();
        }

		#region Custom Partioning Strategy

		private static void UsingContextPartitionerClass()
		{
//			var random = new Random();
//			var sourceData = new WorkItem[10_000];
//
//			for (int i = 0; i < sourceData.Length; i++)
//			{
//				sourceData[i] = new WorkItem() {WorkDuration = random.Next(1, 11)};
//			}
//
//			Partitioner<WorkItem> cPartitioner = new ContextPartitioner(sourceData, 100);
//
//			Parallel.ForEach(cPartitioner, item => { item.PerformWork(); });
		}
		#endregion

		private static void UsingOrderablePartitioner()
        {
	        IList<string> sourceData = new List<string>()
	        {
		        "an", "apple", "a", "day", "keeps", "the", "doctor", "away"
	        };
			string[] resultData = new string[sourceData.Count];

			OrderablePartitioner<string> op = Partitioner.Create(sourceData);

			Parallel.ForEach(op, (string item, ParallelLoopState loopState, long index) =>
			{
				if (item == "apple")
					item = "apricot";
				resultData[index] = item;
			});

			for (int i = 0; i < resultData.Length; i++)
			{
				Console.WriteLine($"Item {i}: {resultData[i]}.");
			}
        }

        private static void UsingChunkingPartioner()
        {
	        double[] resultData = new double[10_000_000];
	        OrderablePartitioner<Tuple<int, int>> chunkPart = Partitioner.Create(0, resultData.Length, 10_000);

	        Parallel.ForEach(chunkPart, chunkRange =>
	        {
		        for (int i = chunkRange.Item1; i < chunkRange.Item2; i++)
		        {
			        resultData[i] = Math.Pow(i, 2);
		        }
	        });
        }

		delegate void ProcessValue(int value);
		static double[] resultData = new double[10_000_000];

		private static void ComputeResultValue(int indexValue) =>
			resultData[indexValue] = Math.Pow(indexValue, 2);

		private static void ParallelLoopWithVerySmallBody()
        {
			Parallel.For(9, resultData.Length, (int index) =>
			{
				resultData[index] = Math.Pow(index, 2);
			});

			Parallel.For(0, resultData.Length, delegate(int index)
			{
				resultData[index] = Math.Pow((double)index, 2);
			});

			var pDel = new ProcessValue(ComputeResultValue);
			var pAction = new Action<int>(pDel);
			Parallel.For(0, resultData.Length, pAction);
        }

		private static void MixingSynchronousAndParallelLoop()
		{
			var rnd = new Random();
			int itemsPerMonth = 100_000;
			var sourceData = new Transaction[12 * itemsPerMonth];

			for (int i = 0; i < 12 * itemsPerMonth; i++)
			{
				sourceData[i] = new Transaction() {Amount = rnd.Next(-450, 500)};
			}

			var monthlyBalance = new int[12];

			for (int currentMonth = 0; currentMonth < 12; currentMonth++)
			{
				Parallel.For(currentMonth * itemsPerMonth, (currentMonth + 1) * itemsPerMonth, new ParallelOptions(),
					() => 0, (index, loopState, tlsBalance) =>
					{
						return tlsBalance += sourceData[index].Amount;
					},
					tlsBalance => monthlyBalance[currentMonth] += tlsBalance);

				if (currentMonth > 0)
					monthlyBalance[currentMonth] += monthlyBalance[currentMonth - 1];
			}

			for (int i = 0; i < monthlyBalance.Length; i++)
			{
				Console.WriteLine($"Month {i} - Balance: {monthlyBalance[i]}");
			}
		}

        private static void ParallelForEachLoopWithTLS()
        {
	        int matchedWords = 0;
	        var lockObject = new object();
	        var dataItems = new string[]
	        {
		        "an", "apple", "a", "day", "keeps", "the", "doctor", "away"
	        };

	        Parallel.ForEach(dataItems, () => 0, (string item, ParallelLoopState loopState, int tlsValue) =>
	        {
		        foreach(var ch in item)
					if (ch == 'a')
						tlsValue++;
		        return tlsValue;
	        }, tlsValue =>
	        {
		        lock (lockObject)
			        matchedWords += tlsValue;
	        });

	        Console.WriteLine($"Matches 'a': {matchedWords}");
        }

        private static void ParallelLoopWithTLS()
        {
	        int total = 0;

	        Parallel.For(0, 100, () => 0, (int index, ParallelLoopState loopState, int tlsValue) =>
	        {
		        tlsValue += 1;
		        return tlsValue;
	        }, value => Interlocked.Add(ref total, value));

	        Console.WriteLine($"Total: {total}");
        }

        private static void CancellingParallelLoop()
        {
			var tokenSource = new CancellationTokenSource();

			Task.Factory.StartNew(() =>
			{
				Thread.Sleep(2000);
				tokenSource.Cancel();

				Console.WriteLine("Token cancelled.");
			});

			ParallelOptions loopOptions = new ParallelOptions() {CancellationToken = tokenSource.Token};
			try
			{
				Parallel.For(0, Int64.MaxValue, loopOptions, index =>
				{
					double result = Math.Pow(index, 3);
					Console.WriteLine($"Index {index}, result {result}");
					Thread.Sleep(500);
				});
			}
			catch (OperationCanceledException)
			{
				Console.WriteLine("Caught cancellation exception...");
			}
        }

		private static void UsingParallelLoopResultStructure()
		{
			ParallelLoopResult loopResult = Parallel.For(0, 10, (int index, ParallelLoopState loopState) =>
			{
				if (index == 2)
					loopState.Stop();
			});

			Console.WriteLine("Loop Result:");
			Console.WriteLine($"Is completed: {loopResult.IsCompleted}");
			Console.WriteLine($"Break value: {loopResult.LowestBreakIteration.HasValue}");
		}

        private static void UsingBreakInParallelLoop()
        {
	        ParallelLoopResult result = Parallel.For(0, 100, (int index, ParallelLoopState state) =>
	        {
		        double sqr = Math.Pow(index, 2);

		        if (sqr > 100)
		        {
			        Console.WriteLine($"Breaking on index {index}....");
			        state.Break();
		        }
		        else
		        {
			        Console.WriteLine($"Square value of {index} is {sqr}.");
		        }
	        });
        }

		private static void UsingStopInParallelLoop()
		{
			var dataItems = new List<string>()
			{
				"an", "apple", "a", "day", "keeps", "the", "doctor", "away"
			};

			Parallel.ForEach(dataItems, (string item, ParallelLoopState state) =>
			{
				if (item.Contains("k"))
				{
					Console.WriteLine($"Hits word with 'k' symbol: {item}.");
					state.Stop();
				}
				else
				{
					Console.WriteLine($"Miss: {item}");
				}
			});
		}

        private static void ParallelLoopWithOptions()
        {
	        var options = new ParallelOptions() {MaxDegreeOfParallelism = 1};

	        Parallel.For(0, 10, options, index =>
	        {
		        Console.WriteLine($"For Index {index} started...");
		        Thread.Sleep(500);
		        Console.WriteLine($"For Index {index} finished.");
	        });

	        var dataElements = new int[] {0, 2, 4, 6, 8};
	        Parallel.ForEach(dataElements, options, index =>
	        {
		        Console.WriteLine($"For Index {index} started...");
		        Thread.Sleep(500);
		        Console.WriteLine($"For Index {index} finished.");
	        });
        }

        private static void CreatingSteppingLoop()
        {
	        Parallel.ForEach(SteppedIterator(0, 10, 2), index =>
	        {
		        Console.WriteLine($"Index value {index}...");
	        });
        }

		private static IEnumerable<int> SteppedIterator(int startIndex, int endIndex, int stepSize)
		{
			for (int i = startIndex; i < endIndex; i += stepSize)
				yield return i;
		}

        private static void ProcessingCollectionUsingForEachLoop()
        {
	        var dataList = new List<string>()
		        {"the", "quick", "brown", "fox", "jumps", "etc"};

			Parallel.ForEach(dataList, item =>
			{
				Console.WriteLine($"Item {item} has {item.Length} character(s)");
			});
        }

        private static void BasicParallelLoop()
        {
	        Parallel.For(0, 10, index =>
	        {
		        Console.WriteLine($"Task ID {Task.CurrentId} processing index {index}...");
	        });
        }

		private static void PerformingActionsUsingInvoke()
		{
			Parallel.Invoke(
				() => Console.WriteLine("Action 1"),
				() => Console.WriteLine("Action 2"),
				() => Console.WriteLine("Action 3"));
			Console.WriteLine("Blocked 1st set...");

			var actions = new Action[3];
			actions[0] = new Action(() => Console.WriteLine("Action 4"));
			actions[1] = new Action(() => Console.WriteLine("Action 5"));
			actions[2] = new Action(() => Console.WriteLine("Action 6"));

			Parallel.Invoke(actions);
			Console.WriteLine("Blocked 2nd set...");

			// Create the same effect using tasks explicitly
			var parent = Task.Factory.StartNew(() =>
			{
				foreach (var action in actions)
					Task.Factory.StartNew(action, TaskCreationOptions.AttachedToParent);
			});
			parent.Wait();
			Console.WriteLine("Blocked 3rd set...");
		}

        public static void ParallelForLoop()
        {
	        var dataItems = new int[100];
	        var resultItems = new double[100];

	        for (int i = 0; i < dataItems.Length; i++)
	        {
		        dataItems[i] = i;
	        }

	        Parallel.For(0, dataItems.Length, (index) =>
	        {
				resultItems[index] = Math.Pow(dataItems[index], 2);
	        });
        }
    }
}
