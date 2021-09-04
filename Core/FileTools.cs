using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace dupefiles2.Core
{
	public static class FileTools
	{
		public const int BinaryCompareBufferSize = 4096;
		public const int HashBufferSize = 1200000;

		/// <summary>
		/// Convert bytes to human readable string
		/// Thanks to https://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net
		/// </summary>
		/// <param name="byteCount"></param>
		/// <returns></returns>
		public static string BytesToString(this long byteCount)
		{
			string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
			if (byteCount == 0)
				return "0" + suf[0];
			long bytes = Math.Abs(byteCount);
			int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
			double num = Math.Round(bytes / Math.Pow(1024, place), 1);
			return (Math.Sign(byteCount) * num).ToString() + suf[place];
		}

		public static IEnumerable<DirectoryInfo> EnumerateDirectoriesRecursive(string basepath, string pattern, EnumerationOptions searchoptions, bool recursive = true)
		{
			var todo = new Queue<string>();
			todo.Enqueue(basepath);
			while (todo.Count > 0)
			{
				string dir = todo.Dequeue();
				string[] subdirs = new string[0];
				string[] items = new string[0];
				DirectoryInfo di = new DirectoryInfo(dir);

				try
				{
					if (recursive)
						subdirs = Directory.GetDirectories(dir, pattern, searchoptions);
					else
						subdirs = new string[] { dir };
				}
				catch (IOException)
				{
					continue;
				}
				catch (System.UnauthorizedAccessException)
				{
					continue;
				}

				// add direcotires
				foreach (string subdir in subdirs)
				{
					todo.Enqueue(subdir);
				}

				// get sub directories
				try
				{
					items = Directory.GetDirectories(dir, pattern);
				}
				catch (IOException)
				{
					continue;
				}
				catch (System.UnauthorizedAccessException)
				{
					continue;
				}

				// return all directories
				foreach (string item in items)
				{
					yield return new DirectoryInfo(item);
				}
			}
		}

		public static string CalculateSHA256(string filename)
		{
			try
			{
				using (var hash = SHA256.Create())
				{
					using (var stream = new BufferedStream(File.OpenRead(filename), HashBufferSize))
					{
						var result = hash.ComputeHash(stream);
						return BitConverter.ToString(result).Replace("-", "").ToLowerInvariant();
					}
				}
			}
			catch (System.Exception)
			{
				return "n/a";
			}
		}

		public static bool BinaryCompareFiles(string file1, string file2)
		{
			const int bufferSize = BinaryCompareBufferSize;
			var buffer1 = new byte[bufferSize];
			var buffer2 = new byte[bufferSize];

			using (FileStream fs1 = System.IO.File.OpenRead(file1))
			using (FileStream fs2 = System.IO.File.OpenRead(file2))
			{
				while (true)
				{
					int count1 = fs1.Read(buffer1, 0, bufferSize);
					int count2 = fs2.Read(buffer2, 0, bufferSize);

					if (count1 != count2)
						return false;

					if (count1 == 0)
						return true;

					int iterations = (int)Math.Ceiling((double)count1 / sizeof(Int64));
					for (int i = 0; i < iterations; i++)
					{
						if (BitConverter.ToInt64(buffer1, i * sizeof(Int64)) != BitConverter.ToInt64(buffer2, i * sizeof(Int64)))
							return false;
					}
				}
			}
		}

	}
}