using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Chapter7
{
    internal class Program
    {
        internal static void Main(string[] args)
        {
            MeasureParallelLoopConcurrency();

            Console.WriteLine("Press enter to finish...");
            Console.ResetColor();
        }

        private static void MeasureParallelLoopConcurrency()
        {
//			Execution results:
//
//	        Concurrency 1: per - iteration time is 4207 ms
//	        Concurrency 2: per - iteration time is 2572 ms
//	        Concurrency 3: per - iteration time is 2001 ms
//	        Concurrency 4: per - iteration time is 1893 ms
//	        Concurrency 5: per - iteration time is 2020 ms
//	        Concurrency 6: per - iteration time is 2040 ms
//	        Concurrency 7: per - iteration time is 1886 ms
//	        Concurrency 8: per - iteration time is 1955 ms
//	        Concurrency 9: per - iteration time is 1926 ms
//	        Concurrency 10: per - iteration time is 1909 ms
//	        Concurrency 11: per - iteration time is 1961 ms
//	        Concurrency 12: per - iteration time is 1932 ms
//	        Concurrency 13: per - iteration time is 1925 ms
//	        Concurrency 14: per - iteration time is 1915 ms
//	        Concurrency 15: per - iteration time is 1971 ms
//	        Concurrency 16: per - iteration time is 1936 ms

            var random = new Random();
            var sourceData = new int[100_000_000];

            for (int i = 0; i < sourceData.Length; i++)
            {
                sourceData[i] = random.Next(0, int.MaxValue);
            }

            int numberOfIteration = 10;
            int maxDegreeOfConcurrency = 16;

            var lockObject = new object();

            for (int concurrency = 1; concurrency <= maxDegreeOfConcurrency; concurrency++)
            {
                var stopWatch = Stopwatch.StartNew();
                var options = new ParallelOptions() {MaxDegreeOfParallelism = concurrency};

                for (int interation = 0; interation < numberOfIteration; interation++)
                {
                    double result = 0;

                    Parallel.ForEach(
                        sourceData, options,
                        () => 0.0,
                        (int value, ParallelLoopState loopState, long index, double localTotal) =>
                        {
                            return localTotal + Math.Pow(value, 2);
                        },
                        localTotal =>
                        {
                            lock (lockObject)
                                result += localTotal;
                        });
                }

                stopWatch.Stop();

                var elapsedTime = stopWatch.ElapsedMilliseconds / numberOfIteration;
                Console.WriteLine($"Concurrency {concurrency}: per-iteration time is {elapsedTime} ms");
            }
        }
    }
}
