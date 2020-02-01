using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Chapter6
{
    class Chapter6
    {
        static void Main(string[] args)
        {
	        PLinqCustomAggregation();

	        Console.WriteLine("Press enter to finish...");
	        Console.ReadLine();
        }

		private static void PLinqCustomAggregation()
		{
			var sourceData = new int[5];
			for (int i = 0; i < sourceData.Length; i++)
				sourceData[i] = i;

			double aggregateResult =
				sourceData
					.AsParallel()
					.Aggregate(
						() => 0.0,
						(subtotal, item) => subtotal += Math.Pow(item, 2),
						(total, subtotal) => total + subtotal,
						total => total / 2);

			Console.WriteLine($"Result: {aggregateResult}");
		}

		private static void CancellingPLinqQuery()
		{
			var tokenSource = new CancellationTokenSource();
			var sourceData = new int[10_000_000];
			for (int i = 0; i < sourceData.Length; i++)
				sourceData[i] = i;

			IEnumerable<double> results =
				sourceData
					.AsParallel()
					.WithCancellation(tokenSource.Token)
					.Select(item => Math.Pow(item, 2));

			Task.Factory.StartNew(() =>
			{
				Thread.Sleep(500);
				tokenSource.Cancel();
				Console.WriteLine("Token source cancelled.");
			});

			try
			{
				foreach(var result in results)
					Console.WriteLine($"Result: {result}");
			}
			catch(OperationCanceledException)
			{
				Console.WriteLine("Caught cancellation exception.");
			}
		}

		private static void HandlingExceptions()
		{
			var sourceData = new int[10];
			for (int i = 0; i < sourceData.Length; i++)
				sourceData[i] = i;

			IEnumerable<double> results =
				sourceData
					.AsParallel()
					.Select(item =>
					{
						if (item == 0)
							throw new Exception();

						return Math.Pow(item, 2);
					});

			try
			{
				foreach(var result in results)
					Console.WriteLine($"Result is {result}");
			}
			catch(AggregateException aggException)
			{
				aggException.Handle(exception =>
				{
					Console.WriteLine($"Handled exception of type: {exception.GetType()}");
					return true;
				});
			}
		}

		#region Controlling Concurrency
		private static void ForcingParallelExecution()
		{
			var sourceData = new int[10];
			for (int i = 0; i < sourceData.Length; i++)
				sourceData[i] = i;

			var parResult = sourceData
				.AsParallel()
				.WithDegreeOfParallelism(Environment.ProcessorCount)
				.Select(item => Math.Pow(item, 2));

			foreach (var result in parResult)
				Console.WriteLine($"Parallel result: {result}");
		}
		#endregion

		private static void DeferredPLinqExecution()
		{
			var sourceData = new int[10];
			for (int i = 0; i < sourceData.Length; i++)
				sourceData[i] = i;

			Console.WriteLine("Defining PLINQ query");
			IEnumerable<double> result =
				sourceData
					.AsParallel()
					.Select(item =>
					{
						Console.WriteLine($"Processing item {item}");
						return Math.Pow(item, 2);
					})
					.ToArray();
			
			Console.WriteLine("Waiting...");
			Thread.Sleep(5000);

			Console.WriteLine("Accessing results");
			double total = 0;
			foreach (var item in result)
				total += item;
			Console.WriteLine($"Total: {total}");
		}

		private static void UsingForAllExtensionMethod()
		{
			var sourceData = new int[50];
			for (int i = 0; i < sourceData.Length; i++)
				sourceData[i] = i;

			sourceData
				.AsParallel()
				.Where(item => item %2 == 0)
				.ForAll(item => Console.WriteLine($"Item {item} with result {Math.Pow(item, 2)}"));
		}

		private static void CombiningOrderedAndUnorderedQueries()
		{
			var sourceData = new int[10_000];
			for (int i = 0; i < sourceData.Length; i++)
				sourceData[i] = i;

			var result =
					sourceData
						.AsParallel()
						.AsOrdered()
						.Take(10)
						.AsUnordered()
						.Select(item => new
						{
							selectValue = item,
							resultValue = Math.Pow(item, 2)
						});

			foreach(var item in result)
				Console.WriteLine($"Source:{item.selectValue}, with result: {item.resultValue}");
		}

        private static void PreservingOrderInParallelQuery()
        {
	        var sourceData = new int[10];
	        for (int i = 0; i < sourceData.Length; i++)
		        sourceData[i] = i;

	        var parResult = sourceData
		        .AsParallel()
		        .AsOrdered()
		        .Select(item => Math.Pow(item, 2));

	        foreach (var result in parResult)
		        Console.WriteLine($"Parallel result: {result}");
        }

		private static void PLinqFilteringQuery()
		{
			var sourceData = new int[100];
			for (int i = 0; i < sourceData.Length; i++)
				sourceData[i] = i;

			IEnumerable<double> resultByKeywords =
				from item in sourceData.AsParallel()
				where item % 2 == 0
				select Math.Pow(item, 2);

			foreach (var item in resultByKeywords)
				Console.WriteLine($"Result: {item}");

			Console.WriteLine("And result using extension methods...");

			IEnumerable<double> resultByExtMethods =
				sourceData
					.AsParallel()
					.Where(item => item % 2 == 0)
					.Select(item => Math.Pow(item, 2));

			foreach (var item in resultByExtMethods)
				Console.WriteLine($"Result: {item}");
		}

        private static void LinqAndPLinqQueriesUsingExtMethods()
		{
			var sourceData = new int[10];
			for (int i = 0; i < sourceData.Length; i++)
				sourceData[i] = i;

			var seqResult = sourceData.Select(item => Math.Pow(item, 2));

			foreach (var result in seqResult)
				Console.WriteLine($"Sequential result: {result}");

			var parResult = sourceData
								.AsParallel()
								.Select(item => Math.Pow(item, 2));

			foreach (var result in parResult)
				Console.WriteLine($"Parallel result: {result}");
		}

		private static void LinqAndPLinqQueries()
        {
	        var sourceData = new int[10];
	        for (int i = 0; i < sourceData.Length; i++)
		        sourceData[i] = i;

	        IEnumerable<double> seqResult =
		        from item in sourceData
		        select Math.Pow(item, 2);

			foreach(var result in seqResult)
				Console.WriteLine($"Sequential result: {result}");

			IEnumerable<double> parResult =
				from item in sourceData.AsParallel()
				select Math.Pow(item, 2);

			foreach (var result in parResult)
				Console.WriteLine($"Parallel result: {result}");
		}
    }
}
