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

namespace Hasher
{
	/// <summary>
	/// original code borrowed from http://www.alexandre-gomes.com/?p=144
	/// not much of the original code still exist at this point...
	/// </summary>
	public class StreamHasher
	{
		private const int maxRaiseEventTime = 100;//ms
		private const int defaultBufferSize = 4096;

		private readonly HashAlgorithm hashAlgorithm;
		private bool cancel = false;

		public int BufferSize { get; set; }
		public byte[] Hash { get; protected set; }

		public event EventHandler<FileHashingProgressArgs> FileHashingProgress;

		private void OnFileHashingProgress(long totalBytesRead, long size, DateTime startTime)
		{
			var handler = FileHashingProgress;
			if (handler != null)
			{
				handler(this, new FileHashingProgressArgs(totalBytesRead, size, startTime));
			}
		}

		public StreamHasher(HashAlgorithm hashAlgorithm)
		{
			this.hashAlgorithm = hashAlgorithm;
			this.BufferSize = defaultBufferSize;
		}

		public byte[] ComputeHash(Stream stream)
		{
			this.Hash = null;

			// this makes it impossible to change the buffer size while computing  
			int localBufferSize = this.BufferSize;

			byte[] buffer = new byte[localBufferSize];
			int bytesRead;
			long streamLength;
			long totalBytesRead = 0;
			long lastEventedTotalBytesRead = 0;

			DateTime lastTime = DateTime.MinValue;
			DateTime startTime;
			DateTime now;

			if (stream.CanSeek)
			{
				streamLength = stream.Length;
			}
			else
			{
				streamLength = FileHashingProgressArgs.InvalidStreamLength;
			}

			startTime = DateTime.Now;

			do
			{
				if (!stream.CanRead)
				{
					bytesRead = 0;
				}
				else
				{
					bytesRead = stream.Read(buffer, 0, buffer.Length);
					totalBytesRead += bytesRead;
				}

				if (bytesRead == 0)
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
					OnFileHashingProgress(totalBytesRead, streamLength, startTime);
					lastEventedTotalBytesRead = totalBytesRead;
					lastTime = now;
				}
			} while (bytesRead > 0 && !cancel);
			if (lastEventedTotalBytesRead != totalBytesRead)
			{
				OnFileHashingProgress(totalBytesRead, streamLength, startTime);
			}

			if (cancel)
			{
				cancel = false;
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

		public string GetHashString()
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
	}

	public class FileHashingProgressArgs : EventArgs
	{
		public const long InvalidStreamLength = -1;

		public bool IsStreamLengthValid()
		{
			return StreamLength != InvalidStreamLength;
		}

		public long TotalBytesRead { get; private set; }
		/// <summary>
		/// Will always be InvalidStreamLength if the stream length is unavailable.
		/// </summary>
		public long StreamLength { get; private set; }
		public DateTime StartTime { get; private set; }

		public FileHashingProgressArgs(long totalBytesRead, DateTime startTime)
			: this(totalBytesRead, InvalidStreamLength, startTime) { }
		public FileHashingProgressArgs(long totalBytesRead, long streamLength, DateTime startTime)
		{
			this.TotalBytesRead = totalBytesRead;
			this.StreamLength = streamLength;
			this.StartTime = startTime;
		}
	}
}
