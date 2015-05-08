using Hasher;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceTests
{
	class Program
	{
		static void Main(string[] args)
		{
			HumanReadableLength_PerformanceTest_Parallel();
			//HumanReadableLength_PerformanceTest();
		}

		const long every = 1000000000;
		const long iterations = 100000000 * every;

		static void HumanReadableLength_PerformanceTest_Parallel()
		{
			var sw = new Stopwatch();
			sw.Start();
			foreach (var item in TestRange().AsParallel().Select(x => Util.ToHumanReadableString(x))) ;
			sw.Stop();
			Console.WriteLine($"New implementation: {sw.Elapsed}");
			sw.Restart();
			foreach (var item in TestRange().AsParallel().Select(x => HumanReadableLength_OldImplementation(x))) ;
			sw.Stop();
			Console.WriteLine($"Old implementation: {sw.Elapsed}");
		}

		static IEnumerable<long> TestRange()
		{
			for (long i = 0; i < iterations; i += every)
			{
				yield return i;
			}
		}

		static void HumanReadableLength_PerformanceTest()
		{
			var sw = new Stopwatch();
			sw.Start();
			for (long i = 0; i < iterations; i += every)
			{
				var result = Util.ToHumanReadableString(i);
			}
			sw.Stop();
			Console.WriteLine($"New implementation: {sw.Elapsed}");
			sw.Restart();
			for (long i = 0; i < iterations; i += every)
			{
				var result = HumanReadableLength_OldImplementation(i);
			}
			sw.Stop();
			Console.WriteLine($"Old implementation: {sw.Elapsed}");
		}

		private static readonly Dictionary<double, string> units = new Dictionary<double, string>()
			{
				{1024L * 1024 * 1024 * 1024 * 1024 * 1024, "EB" },
				{1024L * 1024 * 1024 * 1024 * 1024, "PB" },
				{1024L * 1024 * 1024 * 1024, "TB" },
				{1024L * 1024 * 1024, "GB" },
				{1024L * 1024, "MB" },
				{1024L, "KB" },
			};
		private static string HumanReadableLength_OldImplementation(long length)
		{
			foreach (var unit in units)
			{
				if (length >= unit.Key)
				{
					double value = (length / unit.Key);
					var format = value <= 999 ? "{0:#0.0}{1}" : "{0:#0}{1}";
					return string.Format(format, value, unit.Value);
				}
			}
			return $"{length}B";
		}
	}
}
