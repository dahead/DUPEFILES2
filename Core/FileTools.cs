using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using todo.Core;

namespace dupesfiles2.Core
{
	public class FileTools
	{

		public class ProgressReportModel
		{
			public int PercentageComplete { get; set; } = 0;
			public List<FileInfo[]> SitesDownloaded { get; set; } = new List<FileInfo[]>();
		}

		public static async Task<List<FileInfo[]>> GetFilesAsync(IProgress<ProgressReportModel> progress, string basepath, string pattern, EnumerationOptions options, bool recursive = true)
		{
			if (string.IsNullOrWhiteSpace(pattern))
				pattern = "*.*";

			// First get all directories
			var dirs = EnumerateDirectoriesRecursive(basepath, pattern, recursive);

			// when get all files
			List<FileInfo[]> output = new List<FileInfo[]>();
			ProgressReportModel report = new ProgressReportModel();

			await Task.Run(() =>
			{
				Parallel.ForEach<DirectoryInfo>(dirs, (dir) =>
				{
					FileInfo[] results = dir.GetFiles(pattern, options);
					output.Add(results);

					report.SitesDownloaded = output;
					// report.PercentageComplete = (output.Count * 100) / websites.Count;
					progress.Report(report);
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
				string[] files = new string[0];
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
					files = Directory.GetDirectories(dir, pattern);
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
				foreach (string filename in files)
				{
					yield return new DirectoryInfo(filename);
				}
			}

		}

	}
}