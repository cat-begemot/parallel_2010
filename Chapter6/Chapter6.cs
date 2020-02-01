using System;
using System.Collections.Generic;
using System.Linq;

namespace Chapter6
{
    class Chapter6
    {
        static void Main(string[] args)
        {
	        LinqAndPLinqQueriesUsingExtMethods();

	        Console.WriteLine("Press enter to finish...");
	        Console.ReadLine();
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
