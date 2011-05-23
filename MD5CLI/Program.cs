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
            ASyncFileHashAlgorithm asyncHash = new ASyncFileHashAlgorithm(HashAlgorithm.Create(string.Format("{0}", hashType)));
            asyncHash.FileHashingProgress += delegate(object sender, ASyncFileHashAlgorithm.FileHashingProgressArgs e)
            {
                WriteCLIPercentage((int)((double)e.TotalBytesRead / (double)e.Size * 100.0));
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

        public static void WriteCLIPercentage(int percent)
        {
            percent = (int)Math.Max(0, Math.Min(Math.Floor(percent / 2.0), 50));
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (int i = 1; i <= percent; i++)
            {
                sb.Append(".");
            }
            j = 0;
            for (int i = percent; i < 50; i++)
            {
                sb.Append(" ");
            }
            sb.Append("]");
            
            Console.Write("\r{0}", sb.ToString());
        }

        public enum HashType
        {
            Unknown,
            MD5,
            SHA1,
        }

    }

    public class ASyncFileHashAlgorithm
    {

        public class FileHashingProgressArgs
        {
            public long TotalBytesRead { get; private set; }
            public long Size { get; private set; }
            public FileHashingProgressArgs(long totalBytesRead, long size)
            {
                this.TotalBytesRead = totalBytesRead;
                this.Size = size;
            }
        }

        protected HashAlgorithm hashAlgorithm;
        protected byte[] hash;
        protected bool cancel = false;
        protected int bufferSize = 4096;
        protected int maxRaiseEventTime = 100;
        protected long lastTime = 0;
        public delegate void FileHashingProgressHandler(object sender, FileHashingProgressArgs e);
        public event FileHashingProgressHandler FileHashingProgress;

        public ASyncFileHashAlgorithm(HashAlgorithm hashAlgorithm)
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

            do
            {
                bytesRead = readAheadBytesRead;
                buffer = readAheadBuffer;

                readAheadBuffer = new byte[_bufferSize];
                readAheadBytesRead = stream.Read(readAheadBuffer, 0, readAheadBuffer.Length);

                totalBytesRead += readAheadBytesRead;

                if (readAheadBytesRead == 0)
                    hashAlgorithm.TransformFinalBlock(buffer, 0, bytesRead);
                else
                    hashAlgorithm.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                if (DateTime.Now.Ticks - lastTime > maxRaiseEventTime)
                {
                    FileHashingProgress(this, new FileHashingProgressArgs(totalBytesRead, size));
                    lastTime = DateTime.Now.Ticks;
                }
            } while (readAheadBytesRead != 0 && !cancel);
            FileHashingProgress(this, new FileHashingProgressArgs(size, size));
            if (cancel)
                return hash = null;

            return hash = hashAlgorithm.Hash;
        }

        public int BufferSize
        {
            get
            { return bufferSize; }
            set
            { bufferSize = value; }
        }

        public byte[] Hash
        {
            get
            { return hash; }
        }

        public void Cancel()
        {
            cancel = true;
        }

        public override string ToString()
        {
            string hex = "";
            foreach (byte b in Hash)
                hex += b.ToString("x2");

            return hex;
        }
    }


}
