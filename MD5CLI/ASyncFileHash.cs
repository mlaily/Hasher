using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace MD5CLI
{
	public class ASyncFileHasher
	{

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

		protected HashAlgorithm hashAlgorithm;
		protected byte[] hash;
		protected bool cancel = false;
		protected int bufferSize = 4096;
		protected int maxRaiseEventTime = 100;
		protected long lastTime = 0;
		protected DateTime lastStartTime;

		public delegate void FileHashingProgressHandler(object sender, FileHashingProgressArgs e);
		public event FileHashingProgressHandler FileHashingProgress;

		public ASyncFileHasher(HashAlgorithm hashAlgorithm)
		{
			this.hashAlgorithm = hashAlgorithm;
		}

		public byte[] ComputeHash(Stream stream)
		{
			cancel = false;
			hash = null;
			int _bufferSize = bufferSize; // this makes it impossible to change the buffer size while computing  

			byte[] readAheadBuffer, buffer;
			int readAheadBytesRead, bytesRead;
			long size, totalBytesRead = 0;

			size = stream.Length;
			readAheadBuffer = new byte[_bufferSize];
			readAheadBytesRead = stream.Read(readAheadBuffer, 0, readAheadBuffer.Length);

			totalBytesRead += readAheadBytesRead;

			//initialized here, to get a time as accurate as possible
			lastStartTime = DateTime.Now;

			do
			{
				bytesRead = readAheadBytesRead;
				buffer = readAheadBuffer;

				readAheadBuffer = new byte[_bufferSize];
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
				if (DateTime.Now.Ticks - lastTime > maxRaiseEventTime)
				{
					FileHashingProgress(this, new FileHashingProgressArgs(totalBytesRead, size, lastStartTime));
					lastTime = DateTime.Now.Ticks;
				}
			} while (readAheadBytesRead != 0 && !cancel);
			FileHashingProgress(this, new FileHashingProgressArgs(size, size, lastStartTime));
			if (cancel)
			{
				return hash = null;
			}

			return hash = hashAlgorithm.Hash;
		}

		public int BufferSize
		{
			get { return bufferSize; }
			set { bufferSize = value; }
		}

		public byte[] Hash
		{
			get { return hash; }
		}

		public void Cancel()
		{
			cancel = true;
		}

		public override string ToString()
		{
			StringBuilder hex = new StringBuilder(hashAlgorithm.HashSize / 8);
			foreach (byte b in Hash)
			{
				hex.AppendFormat("{0:x2}", b);
			}
			return hex.ToString();
		}
	}

}
