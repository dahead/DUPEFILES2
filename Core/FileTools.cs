using System;
using System.Collections.Generic;
using System.IO;
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

		// public static async Task<List<FileInfo[]>> GetFilesAsync(IProgress<ProgressReportModel> progress, string basepath, string pattern, EnumerationOptions options, bool recursive, CancellationToken cancellationToken)
		public static async Task<List<FileInfo[]>> GetFilesAsync(string basepath, string searchpattern, EnumerationOptions options, bool recursive, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(searchpattern))
				searchpattern = "*.*";

			// First get all directories
			var dirs = EnumerateDirectoriesRecursive(basepath, searchpattern, recursive);

			// then get all files
			List<FileInfo[]> output = new List<FileInfo[]>();
			// ProgressReportModel report = new ProgressReportModel();

			// Parallel async
			await Task.Run(() =>
			{
				Parallel.ForEach<DirectoryInfo>(dirs, (dir) =>
				{
					FileInfo[] results = dir.GetFiles(searchpattern, options);
					output.Add(results);
					cancellationToken.ThrowIfCancellationRequested();
					// report.Files = output;
					// report.PercentageComplete = (output.Count * 100) / results.Length;
					// progress.Report(report);
				});
			});
			return output;
		}

		private static IEnumerable<DirectoryInfo> EnumerateDirectoriesRecursive(string basepath, string pattern, bool recursive = true)
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

	}
}