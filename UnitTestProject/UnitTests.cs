using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Hasher;
using System.Collections.Generic;

namespace UnitTestProject
{
	[TestClass]
	public class UnitTests
	{
		[TestMethod]
		public void HumanReadableLength_ResultCheck()
		{
			Assert.AreEqual("0.0B", Util.ToHumanReadableString(0));
			Assert.AreEqual("1.0B", Util.ToHumanReadableString(1));
			Assert.AreEqual("2.0B", Util.ToHumanReadableString(2));
			Assert.AreEqual("999.0B", Util.ToHumanReadableString(999));
			Assert.AreEqual("1000B", Util.ToHumanReadableString(1000));
			Assert.AreEqual("1.0KB", Util.ToHumanReadableString(1024));
			Assert.AreEqual("1.0KB", Util.ToHumanReadableString(1025));
		}

		[TestMethod]
		public void HumanReadableLength_PerformanceTest()
		{
			var sw = new Stopwatch();
			long every = 100000000000;
			long iterations = 10000000 * every;
			sw.Start();
			for (long i = 0; i < iterations; i += every)
			{
				var result = Util.ToHumanReadableString(i);
				//if (i % every == 0)
				//{
				//	Trace.TraceInformation("iteration {0}", i);
				//}
			}
			sw.Stop();
			Trace.TraceInformation($"New implementation: {sw.Elapsed}");
			sw.Restart();
			for (long i = 0; i < iterations; i += every)
			{
				var result = HumanReadableLength_OldImplementation(i);
				//if (i % every == 0)
				//{
				//	Trace.TraceInformation("iteration {0}", i);
				//}
			}
			sw.Stop();
			Trace.TraceInformation($"Old implementation: {sw.Elapsed}");
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
					if (value <= 999) // The output is formatted so that it's never more than 6 chars long.
					{
						return $"{value:#0.0}" + unit.Value;
					}
					else
					{
						return $"{value:#0}" + unit.Value;
					}
				}
			}
			return $"{length}B";
		}
	}
}
