using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace MD5CLI
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.BufferWidth = 100;
			string filePath = string.Empty;
			string hashToTest = string.Empty;
			HashType hashType = HashType.Unknown;
			string hashTypeString = string.Empty;
			bool noCompare = false;
			//detection des parametres
			for (int i = 0; i < args.Length; i++)
			{
				if (System.IO.File.Exists(args[i]))
				{
					filePath = args[i];
				}
				else
				{
					if (args[i].Length == 32)
					{
						hashToTest = args[i];
						hashType = HashType.MD5;
					}
					else if (args[i].Length == 40)
					{
						hashToTest = args[i];
						hashType = HashType.SHA1;
					}
					else
					{
						hashTypeString = args[i];
					}
				}
			}
			if (!System.IO.File.Exists(filePath))
			{
				Console.WriteLine("Argument error. A valid file path is needed.");
				return;
			}
			if (hashType == HashType.Unknown)
			{
				noCompare = true;
				switch (hashTypeString.ToLowerInvariant())
				{
					case "md5":
						hashType = HashType.MD5;
						break;
					case "sha1":
						hashType = HashType.SHA1;
						break;
					default:
						Console.WriteLine("No valid hash algorithm found in command line.\nComputing md5 by default.");
						hashType = HashType.MD5;
						break;
				}
			}
			//on peut commencer...
			Console.WriteLine();
			ASyncFileHasher asyncHash = new ASyncFileHasher(HashAlgorithm.Create(string.Format("{0}", hashType)));
			asyncHash.FileHashingProgress += delegate(object sender, ASyncFileHasher.FileHashingProgressArgs e)
			{
				int totalTime = (int)(DateTime.Now - e.StartTime).TotalSeconds;
				if (totalTime <= 0)
				{
					totalTime = 1;
				}
				WriteCLIPercentage((int)((double)e.TotalBytesRead / (double)e.Size * 100.0));
				Console.Write(" {0}/{1} at {2}/s",HumanReadableLength(e.TotalBytesRead).PadLeft(7), HumanReadableLength(e.Size).PadLeft(7), HumanReadableLength(e.TotalBytesRead / totalTime).PadLeft(7));
			};

			using (System.IO.FileStream fs = new FileStream(filePath, FileMode.Open))
			{
				asyncHash.ComputeHash(fs);
			}

			bool isOk = asyncHash.ToString() == hashToTest;
			Console.WriteLine();
			Console.WriteLine("Result : {0} {1}", asyncHash.ToString(),
				noCompare ? "" : string.Format("compared to reference hash ({0}) \n{1} : {2}!", hashType, hashToTest, isOk ? "OK" : "FAIL"));
		}

		private static string HumanReadableLength(long length)
		{
			long _length = length;
			long[] limits = new long[] { 1125899906842624, 1099511627776, 1073741824, 1048576, 1024 };//1024*1024*1024*1024, 1024*1024*1024, 1024*1024...
			string[] units = new string[] { "PB", "TB", "GB", "MB", "KB" };

			for (int i = 0; i < limits.Length; i++)
			{
				if (_length >= limits[i])
					return String.Format("{0:#0.#0}" + units[i], ((double)_length / limits[i]));
			}
			return String.Format("{0}B", _length);
		}

		public static void WriteCLIPercentage(int percent)
		{
			int percentByTwo = (int)Math.Max(0, Math.Min(Math.Floor(percent / 2.0), 50));
			StringBuilder dots = new StringBuilder();
			dots.Append("[");
			for (int i = 1; i <= percentByTwo; i++)
			{
				dots.Append(".");
			}
			for (int i = percentByTwo; i < 50; i++)
			{
				dots.Append(" ");
			}
			dots.Append("]");

			Console.Write("\r{1}% {0}", dots, percent.ToString().PadLeft(3));
		}

		public enum HashType
		{
			Unknown,
			MD5,
			SHA1,
		}

	}

}
