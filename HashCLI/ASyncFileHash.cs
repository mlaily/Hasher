using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace HashCLI
{
	/// <summary>
	/// http://www.alexandre-gomes.com/?p=144
	/// slightly modified
	/// </summary>
	public class AsyncFileHasher
	{
		private const int maxRaiseEventTime = 100;//ms

		protected readonly HashAlgorithm hashAlgorithm;
		protected bool cancel = false;

		public int BufferSize { get; set; }
		public byte[] Hash { get; protected set; }

		public delegate void FileHashingProgressHandler(object sender, FileHashingProgressArgs e);
		public event FileHashingProgressHandler FileHashingProgress;

		public AsyncFileHasher(HashAlgorithm hashAlgorithm)
		{
			this.hashAlgorithm = hashAlgorithm;
			this.BufferSize = 4096;
		}

		public byte[] ComputeHash(Stream stream)
		{
			// this makes it impossible to change the buffer size while computing  
			int localBufferSize = this.BufferSize;

			byte[] readAheadBuffer, buffer;
			int readAheadBytesRead, bytesRead;
			long size, totalBytesRead = 0;

			DateTime lastTime = DateTime.MinValue;
			DateTime startTime;
			DateTime now;

			size = stream.Length;
			readAheadBuffer = new byte[localBufferSize];
			readAheadBytesRead = stream.Read(readAheadBuffer, 0, readAheadBuffer.Length);

			totalBytesRead += readAheadBytesRead;

			//initialized here, to get a time as accurate as possible
			startTime = DateTime.Now;

			do
			{
				bytesRead = readAheadBytesRead;
				buffer = readAheadBuffer;

				readAheadBuffer = new byte[localBufferSize];
				readAheadBytesRead = stream.Read(readAheadBuffer, 0, readAheadBuffer.Length);

				totalBytesRead += readAheadBytesRead;

				if (readAheadBytesRead == 0)
				{
					hashAlgorithm.TransformFinalBlock(buffer, 0, bytesRead);
				}
				else
				{
					hashAlgorithm.TransformBlock(buffer, 0, bytesRead, buffer, 0);
				}
				now = DateTime.Now;
				if ((now - lastTime).TotalMilliseconds > maxRaiseEventTime)
				{
					FileHashingProgress(this, new FileHashingProgressArgs(totalBytesRead, size, startTime));
					lastTime = now;
				}
			} while (readAheadBytesRead != 0 && !cancel);
			FileHashingProgress(this, new FileHashingProgressArgs(size, size, startTime));

			if (cancel)
			{
				return null;
			}
			else
			{
				this.Hash = hashAlgorithm.Hash;
				return hashAlgorithm.Hash;
			}
		}

		public void Cancel()
		{
			cancel = true;
		}

		public override string ToString()
		{
			if (Hash == null)
			{
				return "";
			}
			StringBuilder hex = new StringBuilder(hashAlgorithm.HashSize / 8);
			foreach (byte b in Hash)
			{
				hex.AppendFormat("{0:x2}", b);
			}
			return hex.ToString();
		}

		public class FileHashingProgressArgs
		{
			public long TotalBytesRead { get; protected set; }
			public long Size { get; protected set; }
			public DateTime StartTime { get; protected set; }

			public FileHashingProgressArgs(long totalBytesRead, long size, DateTime startTime)
			{
				this.TotalBytesRead = totalBytesRead;
				this.Size = size;
				this.StartTime = startTime;
			}
		}
	}
}
