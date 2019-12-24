using System;
using System.Threading.Tasks;

namespace Chapter2
{
    public class Program
    {
        public static void Main(string[] args)
        {
	        var task1 = new Task<int>(() =>
	        {
		        int sum = 0;
		        for (int i = 0; i < 100; i++)
			        sum += i;

		        return sum;
	        });

			task1.Start();
			Console.WriteLine($"Result 1: {task1.Result}");

			var task2 = new Task<int>(obj =>
			{
				int sum = 0;
				int max = (int)obj;
				for (int i = 0; i < max; i++)
					sum += i;

				return sum;
			}, 100);

			task2.Start();
			Console.WriteLine($"Result 2: {task2.Result}");

	        Console.WriteLine("Main method complete. Press enter to finish.");
	        Console.ReadLine();
        }

        private static void PrintMessage(object message)
        {
	        Console.WriteLine($"Message: {message}");
        }
    }
}
