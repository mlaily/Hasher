using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;

namespace HashCLI
{
	class Program
	{
		static void Main(string[] args)
		{
			const string help =
@"Valid inputs:
 - file path [hash algorithm name]
  => returns the file hash as a hexadecimal string.
  the default algorithm is md5.
 - file path [hash to test against (hexadecimal string)]
  => calculates and compares the hash of the file against the provided hash.
  the algorithm is automatically determined based on the length of the string.";
			if (args.Length == 0)
			{
				Console.WriteLine("HashCLI - version {0}\n{1}", Assembly.GetExecutingAssembly().GetName().Version.ToString(), help);
				return;
			}
			string filePath = null;
			string hashToTestAgainst = null;
			HashType hashType = HashType.Unknown;
			string hashTypeName = "";

			//arguments detection
			for (int i = 0; i < args.Length; i++)
			{
				if (HashTypeNames.Any(x => x.Value.ToLowerInvariant() == args[i].ToLowerInvariant()))
				{
					//the user requested a specific algorithm by its name
					hashTypeName = args[i];
				}
				else
				{
					//check next for a hash string
					//for md5: 128 bits is 16 bytes. with 2 char per byte in hexa. hence the "* 4"
					int hashTypeBits = args[i].Length * 4;
					if (Enum.IsDefined(typeof(HashType), hashTypeBits) //the length corresponds to a hash string
						&& Regex.IsMatch(args[i], @"^[0-9A-Fa-f]*$")) // the string is a valid hexadecimal number
					{
						hashToTestAgainst = args[i];
						hashType = (HashType)hashTypeBits;
					}
					//check if it's a valid file
					else if (System.IO.File.Exists(args[i]))
					{
						filePath = args[i];
					}
					//if the arg is not an algorithm, not a hash, and not a valid file, it is ignored
				}
			}

			if (filePath == null) //if the path was set, it is valid
			{
				Console.WriteLine("A valid file path is needed.");
				return;
			}

			if (hashType == HashType.Unknown)
			{
				string hashTypeNameToLower = hashTypeName.ToLowerInvariant();
				var match = HashTypeNames.FirstOrDefault(x => x.Value.ToLowerInvariant() == hashTypeNameToLower);
				if (match.Key != HashType.Unknown)
				{
					hashType = match.Key;
				}
				else
				{
					hashType = HashType.MD5;
				}
			}
			//on peut commencer...
			AsyncFileHasher asyncHash = new AsyncFileHasher(HashAlgorithm.Create(HashTypeNames[hashType]));
			asyncHash.FileHashingProgress += new AsyncFileHasher.FileHashingProgressHandler(asyncHash_FileHashingProgress);

			using (System.IO.FileStream fs = new FileStream(filePath, FileMode.Open))
			{
				asyncHash.ComputeHash(fs);
			}

			bool isOk = asyncHash.ToString() == hashToTestAgainst;
			Console.WriteLine();
			if (hashToTestAgainst == null)
			{
				Console.WriteLine("Result: {0}", asyncHash.ToString());
			}
			else
			{
				Console.WriteLine("Result: {0}\nReference:  {1}\nCalculated: {2}", isOk ? "OK" : "FAIL", hashToTestAgainst, asyncHash.ToString());
			}

		}

		private static void asyncHash_FileHashingProgress(object sender, AsyncFileHasher.FileHashingProgressArgs e)
		{
			int lineLength = 0;
			int totalTime = (int)(DateTime.Now - e.StartTime).TotalSeconds;
			if (totalTime <= 0)
			{
				totalTime = 1;
			}
			Console.Write("\r");
			lineLength += WriteCLIPercentage((int)((double)e.TotalBytesRead / (double)e.Size * 100.0));
			string moreInfos = string.Format(" {0}/{1} @{2}/s", HumanReadableLength(e.TotalBytesRead), HumanReadableLength(e.Size), HumanReadableLength(e.TotalBytesRead / totalTime));
			lineLength += moreInfos.Length;
			int padding = Console.BufferWidth - lineLength - 1;
			if (padding > 0)
			{
				Console.Write(new string(' ', padding));
			}
			Console.Write(moreInfos);
		}

		private static string HumanReadableLength(long length)
		{
			long _length = length;
			long[] limits = new long[] { 1024L * 1024 * 1024 * 1024 * 1024, 1024L * 1024 * 1024 * 1024, 1024L * 1024 * 1024, 1024L * 1024, 1024L };
			string[] units = new string[] { "PB", "TB", "GB", "MB", "KB" };

			for (int i = 0; i < limits.Length; i++)
			{
				if (_length >= limits[i])
				{
					double value = ((double)_length / limits[i]);
					if (value < 1000)
					{
						return String.Format("{0:#0.0}" + units[i], value);
					}
					else
					{
						return String.Format("{0:#0}" + units[i], value);
					}
				}
			}
			return String.Format("{0}B", _length);
		}

		private static int WriteCLIPercentage(int percent)
		{
			const int baseLength = 50;
			const int fullLenght = 52;
			int percentByTwo = (int)Math.Max(0, Math.Min(Math.Floor(percent / 2.0), baseLength));
			StringBuilder dots = new StringBuilder();
			dots.Append("[");
			for (int i = 1; i <= percentByTwo; i++)
			{
				dots.Append(".");
			}
			for (int i = percentByTwo; i < baseLength; i++)
			{
				dots.Append(" ");
			}
			dots.Append("]");
			string percentage = string.Format("{0}%", percent);
			int halves = (fullLenght / 2) - percentage.Length / 2;
			int alignRight = percentage.Length % 2;
			string formatted = dots.ToString(0, halves) + percentage + dots.ToString(fullLenght - halves + alignRight, halves - alignRight);
			Console.Write(formatted);
			return formatted.Length;
		}

		private static readonly Dictionary<HashType, string> HashTypeNames = new Dictionary<HashType, string>()
		{
			{ HashType.Unknown, "Unknown"},
			{ HashType.MD5, "MD5"},
			{ HashType.SHA1, "SHA1"},
			{ HashType.SHA256, "SHA256"},
			{ HashType.SHA384, "SHA384"},
			{ HashType.SHA512, "SHA512"},
		};

		public enum HashType
		{
			Unknown = 0,
			MD5 = 128,
			SHA1 = 160,
			SHA256 = 256,
			SHA384 = 384,
			SHA512 = 512,
		}

	}

}
