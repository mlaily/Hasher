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
		public void TestHumanReadableLength()
		{
			//Trace.TraceInformation(Util.HumanReadableLength(long.MaxValue));
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
			Trace.TraceInformation(sw.Elapsed.ToString());
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
			Trace.TraceInformation(sw.Elapsed.ToString());
		}

		private static string HumanReadableLength_OldImplementation(long length)
		{
			Dictionary<long, string> units = new Dictionary<long, string>() 
			{
				{1024L * 1024 * 1024 * 1024 * 1024, "PB" },
				{1024L * 1024 * 1024 * 1024, "TB" },
				{1024L * 1024 * 1024, "GB" },
				{1024L * 1024, "MB" },
				{1024L, "KB" },
			};
			foreach (var unit in units)
			{
				if (length >= unit.Key)
				{
					double value = ((double)length / unit.Key);
					if (value < 1000)
					{
						return String.Format("{0:#0.0}" + unit.Value, value);
					}
					else
					{
						return String.Format("{0:#0}" + unit.Value, value);
					}
				}
			}
			return String.Format("{0}B", length);
		}
	}
}
