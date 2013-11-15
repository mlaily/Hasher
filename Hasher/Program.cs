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
		private static Stream consoleInputStreamReference = null;
		static void Main(string[] args)
		{
			string filePath = null;
			string hashToTestAgainst = null;
			HashType hashType = HashType.Unknown;
			string hashTypeName = null;
			bool readFromStandardInput = false;

			int ignoredArgsCount = 0;

			consoleInputStreamReference = Console.OpenStandardInput();

			//arguments detection
			foreach (var argument in args)
			{
				var comparer = StringComparer.OrdinalIgnoreCase;
				if (comparer.Equals(argument, "/?") ||
					comparer.Equals(argument, "/help") ||
					comparer.Equals(argument, "-h") ||
					comparer.Equals(argument, "--help"))
				{
					DisplayHelp();
					return;
				}

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
							ignoredArgsCount++;
							Console.WriteLine("Unexpected argument will be ignored: {0}", argument);
						}
					}
				}
			}

			if (!Console.IsInputRedirected) //input directly from the user
			{
				if (args.Length - ignoredArgsCount == 0)
				{
					DisplayHelp();
					return;
				}
				else
				{
					Console.CancelKeyPress += (o, e) =>
					{
						e.Cancel = true;
						consoleInputStreamReference.Close();
					};
				}
			}

			if (filePath == null) //if the path is set, we already verified it's valid
			{
				readFromStandardInput = true;
			}
			else
			{
				if (Console.IsInputRedirected)
				{
					Console.WriteLine("WARNING: The standard input is redirected, but a file was also specified.\nIgnoring the standard input...");
				}
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
			FileHasher asyncHasher = new FileHasher(HashAlgorithm.Create(HashTypeNames[hashType]));
			asyncHasher.FileHashingProgress += new EventHandler<FileHashingProgressArgs>(asyncHash_FileHashingProgress);

			if (readFromStandardInput)
			{
				using (var stdIn = consoleInputStreamReference)
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
				Console.WriteLine("\nResult: {0}", resultHash);
			}
			else
			{
				bool isOk = StringComparer.OrdinalIgnoreCase.Equals(resultHash, hashToTestAgainst);
				Console.WriteLine("\nReference:  {1}\nCalculated: {2}\n\nResult: {0}", isOk ? "OK" : "FAIL", hashToTestAgainst, resultHash);
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
			string progressBar;
			string humanReadableSize;
			if (e.IsStreamLengthValid())
			{
				progressBar = GetProgressBar((int)((double)e.TotalBytesRead / (double)e.StreamLength * 100f));
				humanReadableSize = HumanReadableLength(e.StreamLength);
			}
			else
			{
				progressBar = GetProgressBar(-1);
				humanReadableSize = "??";
			}
			Console.Write(progressBar);
			lineLength += progressBar.Length;
			string moreInfo = string.Format(" {0}/{1} @{2}/s", HumanReadableLength(e.TotalBytesRead), humanReadableSize, HumanReadableLength(e.TotalBytesRead / totalTime));
			lineLength += moreInfo.Length;
			int padding = Console.BufferWidth - lineLength - 1;
			if (padding > 0)
			{
				Console.Write(new string(' ', padding));
			}
			Console.Write(moreInfo);
		}

		private static int invalidProgressIndicator = 0;
		private static bool directionRight = true;
		/// <summary>
		/// If the percent value is an invalid percentage (less than 0 or more than 100)
		/// the returned string is a moving star...
		/// </summary>
		private static string GetProgressBar(int percent)
		{
			const int baseLength = 50;
			const int fullLength = 52;
			StringBuilder dots = new StringBuilder();
			if (percent < 0 || percent > 100)
			{
				dots.Append("[");
				for (int i = 0; i < invalidProgressIndicator; i++)
				{
					dots.Append(" ");
				}
				dots.Append('*');
				//take account of the star and the bracket
				for (int i = invalidProgressIndicator + 2; i < baseLength + 2; i++)
				{
					dots.Append(" ");
				}
				dots.Append("]");
				invalidProgressIndicator += directionRight ? 1 : -1;
				if (invalidProgressIndicator >= baseLength || invalidProgressIndicator <= 0)
				{
					directionRight = !directionRight;
				}
				return dots.ToString();
			}
			else
			{
				int percentByTwo = (int)Math.Max(0, Math.Min(Math.Floor(percent / 2f), baseLength));
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

		private static void DisplayHelp()
		{
			const string help =
@"Valid inputs:
 - filePath [hash algorithm name]
  => returns the file hash as a hexadecimal string.
  the default algorithm is md5.
 - filePath [hash to test against (hexadecimal string)]
  => calculate and compare the hash of the file against the provided hash.
  the algorithm is automatically determined based on the length of the string.

If no file path is provided,
	the program will try to read from the standard input stream.

To force an argument to be interpreted as an input file path,
	put it between quotes.

Supported Algorithms:
 - MD5
 - SHA1
 - SHA256
 - SHA384
 - SHA512";
			Console.WriteLine("C# Hash Utility version {0}\nBy Melvyn Laily - arcanesanctum.net\n\n{1}",
				Assembly.GetExecutingAssembly().GetName().Version, help);
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
