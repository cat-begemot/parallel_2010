using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace Chapter5
{
    internal class Program
    {
        public static void Main(string[] args)
        {
	        UsingBreakInParallelLoop();

	        Console.WriteLine("Press enter to finish...");
	        Console.ReadLine();
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
