//Copyright (c) 2013 Melvyn Laily

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Hasher
{
	class Program
	{
		static void Main(string[] args)
		{
			const string help =
@"Valid inputs:
 - filePath [hash algorithm name]
  => returns the file hash as a hexadecimal string.
  the default algorithm is md5.
 - filePath [hash to test against (hexadecimal string)]
  => calculate and compare the hash of the file against the provided hash.
  the algorithm is automatically determined based on the length of the string.

If no file path is provided, the program will try to read data from the standard input stream.

Supported Algorithms:
 - MD5
 - SHA1
 - SHA256
 - SHA384
 - SHA512

NOTE: to force an argument to be interpreted as an input file path, put it between quotes.";
			if (args.Length == 0)
			{
				Console.WriteLine("C# Hash Utility version {0}\nBy Melvyn Laily - arcanesanctum.net\n\n{1}",
					Assembly.GetExecutingAssembly().GetName().Version, help);
				return;
			}
			string filePath = null;
			string hashToTestAgainst = null;
			HashType hashType = HashType.Unknown;
			string hashTypeName = null;
			bool readFromStandardInput = false;

			//arguments detection
			foreach (var argument in args)
			{
				if (HashTypeNames.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x.Value, argument)))
				{
					//the user requested a specific algorithm by its name
					hashTypeName = argument;
				}
				else
				{
					//check for a hash string
					//for md5: 128 bits is 16 bytes. with 2 char per byte in hexa. hence the "* 4"
					int hashTypeBits = argument.Length * 4;
					if (Enum.IsDefined(typeof(HashType), hashTypeBits) //the length corresponds to a hash string
						&& Regex.IsMatch(argument, @"^[0-9A-F]*$", RegexOptions.IgnoreCase)) // the string is a valid hexadecimal number
					{
						hashToTestAgainst = argument;
						hashType = (HashType)hashTypeBits;
					}
					else
					{
						//check if it's a valid file
						string withoutQuotes = argument.Trim(new char[] { '"' });
						if (System.IO.File.Exists(withoutQuotes))
						{
							filePath = withoutQuotes;
						}
						else
						{
							//if the arg is not an algorithm name, not a hash, and not a valid file, it is ignored.
							Console.WriteLine("Unexpected argument will be ignored: {0}", argument);
						}
					}
				}
			}

			if (filePath == null) //if the path is set, we already verified it's valid
			{
				readFromStandardInput = true;
			}

			//sanity check
			if (hashType != HashType.Unknown && hashTypeName != null)
			{
				Console.WriteLine("Warning: the provided hash string is not a valid hash for the specified algorithm ({0})!", hashTypeName);
				Console.WriteLine("The algorithm defined by the hash string ({0}) will take precedence", HashTypeNames[hashType]);
			}

			if (hashType == HashType.Unknown) //meaning no hash string was provided
			{
				var match = HashTypeNames.FirstOrDefault(x => StringComparer.OrdinalIgnoreCase.Equals(x.Value, hashTypeName));
				if (match.Key != HashType.Unknown)
				{
					hashType = match.Key;
				}
				else
				{
					hashType = HashType.MD5;
				}
			}

			//let's hash!
			AsyncFileHasher asyncHasher = new AsyncFileHasher(HashAlgorithm.Create(HashTypeNames[hashType]));
			asyncHasher.FileHashingProgress += new AsyncFileHasher.FileHashingProgressHandler(asyncHash_FileHashingProgress);

			if (readFromStandardInput)
			{
				using (var stdIn = Console.OpenStandardInput())
				{
					asyncHasher.ComputeHash(stdIn);
				}
			}
			else
			{
				using (System.IO.FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
				{
					asyncHasher.ComputeHash(fs);
				}
			}
		

			Console.WriteLine();
			string resultHash = asyncHasher.GetHashString();
			if (hashToTestAgainst == null)
			{
				Console.WriteLine("Result: {0}", resultHash);
			}
			else
			{
				bool isOk = StringComparer.OrdinalIgnoreCase.Equals(resultHash, hashToTestAgainst);
				Console.WriteLine("Result: {0}\nReference:  {1}\nCalculated: {2}", isOk ? "OK" : "FAIL", hashToTestAgainst, resultHash);
			}

		}

		private static void asyncHash_FileHashingProgress(object sender, FileHashingProgressArgs e)
		{
			int lineLength = 0;
			int totalTime = (int)(DateTime.Now - e.StartTime).TotalSeconds;
			if (totalTime <= 0)
			{
				totalTime = 1;
			}
			Console.Write("\r");
			//TODO: handle the case where e.Size is FileHashingProgressArgs.InvalidSize (the stream has no Length)
			var progressBar = GetProgressBar((int)((double)e.TotalBytesRead / (double)e.Size * 100f));
			Console.Write(progressBar);
			lineLength += progressBar.Length;
			string moreInfos = string.Format(" {0}/{1} @{2}/s", HumanReadableLength(e.TotalBytesRead), HumanReadableLength(e.Size), HumanReadableLength(e.TotalBytesRead / totalTime));
			lineLength += moreInfos.Length;
			int padding = Console.BufferWidth - lineLength - 1;
			if (padding > 0)
			{
				Console.Write(new string(' ', padding));
			}
			Console.Write(moreInfos);
		}

		private static string GetProgressBar(int percent)
		{
			const int baseLength = 50;
			const int fullLength = 52;
			int percentByTwo = (int)Math.Max(0, Math.Min(Math.Floor(percent / 2f), baseLength));
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
			int halves = (fullLength / 2) - percentage.Length / 2;
			int alignRight = percentage.Length % 2;
			return dots.ToString(0, halves) + percentage + dots.ToString(fullLength - halves + alignRight, halves - alignRight);
		}

		private static string HumanReadableLength(long length)
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

		private static readonly Dictionary<HashType, string> HashTypeNames = new Dictionary<HashType, string>()
		{
			{ HashType.Unknown, "Unknown" },
			{ HashType.MD5, "MD5" },
			{ HashType.SHA1, "SHA1" },
			{ HashType.SHA256, "SHA256" },
			{ HashType.SHA384, "SHA384" },
			{ HashType.SHA512, "SHA512" },
		};

		//The values are actually the number of output bits for the algorithms
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
