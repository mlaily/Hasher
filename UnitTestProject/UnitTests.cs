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
	}
}
