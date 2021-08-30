using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using todo.Core;

namespace dupesfiles2.Core
{
	public class FileTools
	{

		public class ProgressReportModel
		{
			public int PercentageComplete { get; set; } = 0;
			public List<FileInfo[]> Files { get; set; } = new List<FileInfo[]>();
		}

		// public static async Task<List<FileInfo[]>> GetFilesAsync(string basepath, string searchpattern, EnumerationOptions options, bool recursive, CancellationToken cancellationToken)


		public static IEnumerable<DirectoryInfo> EnumerateDirectoriesRecursive(string basepath, string pattern, bool recursive = true)
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
						subdirs = Directory.GetDirectories(dir);
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

				foreach (string subdir in subdirs)
				{
					todo.Enqueue(subdir);
				}

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

				// Return all files
				foreach (string item in items)
				{
					yield return new DirectoryInfo(item);
				}
			}

		}

		public static string CalculateMD5(string filename)
		{
			try
			{
				using (var hash = MD5.Create())
				{
					using (var stream = new BufferedStream(File.OpenRead(filename), 1200000))
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

		public static string CalculateSHA256(string filename)
		{
			try
			{
				using (var hash = SHA256.Create())
				{
					using (var stream = new BufferedStream(File.OpenRead(filename), 1200000))
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

	}
}