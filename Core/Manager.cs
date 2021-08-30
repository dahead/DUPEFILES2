using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using todo.Commands;
using todo.Core;
using static dupesfiles2.Core.FileTools;
using static todo.Commands.IndexAddCommand;

namespace dupesfiles2.Core
{
	public class Manager
	{

		CancellationTokenSource cts = new CancellationTokenSource();

		public IndexDataModel idx { get; set; } = new IndexDataModel();

		public Manager()
		{
			this.idx = IndexDataModel.LoadFromFile();
		}

		public async Task AddFilesToIndex(IndexAddCommand.Settings settings)
		{
			Progress<ProgressReportModel> progress = new Progress<ProgressReportModel>();
			progress.ProgressChanged += ReportAddFilesToIndexProgress;
			var watch = System.Diagnostics.Stopwatch.StartNew();

			// Get files
			var results = await GetFilesAsync(progress, settings.Path, settings.SearchPattern, cts.Token);

			// add results to the index
			foreach (var item in results)
			{
				foreach (var subitem in item)
				{
					this.idx.Add(new ItemDataModel() { Path = subitem.FullName, Size = subitem.Length, Hash = string.Empty });
				}
			}

			AnsiConsole.MarkupLine($"Total execution time: [bold]{ watch.ElapsedMilliseconds }[/]");
		}

		public static async Task<List<FileInfo[]>> GetFilesAsync(IProgress<ProgressReportModel> progress, string basepath, string searchpattern, CancellationToken cancellationToken)
		{
			// setup base search path
			if (string.IsNullOrWhiteSpace(searchpattern))
				searchpattern = "*.*";

			// create search options
			var searchOptions = new EnumerationOptions
			{
				AttributesToSkip = true ? FileAttributes.Hidden | FileAttributes.System : FileAttributes.System,
				RecurseSubdirectories = false
			};

			// add basedir to dirs
			var dirs = new List<DirectoryInfo>();
			dirs.Add(new DirectoryInfo(basepath));

			// First get all directories
			var subdirs = FileTools.EnumerateDirectoriesRecursive(basepath, searchpattern, true);
			foreach (var item in subdirs)
			{
				dirs.Add(item);
			}

			// then get all files
			List<FileInfo[]> output = new List<FileInfo[]>();

			// Parallel async
			await Task.Run(() =>
			{
				Parallel.ForEach<DirectoryInfo>(dirs, (dir) =>
				{
					FileInfo[] results = dir.GetFiles(searchpattern, searchOptions);
					output.Add(results);

					cancellationToken.ThrowIfCancellationRequested();

					ProgressReportModel report = new ProgressReportModel();
					report.BaseDirectory = dir.FullName;
					report.Count = results.Length;
					// report.PercentageComplete = (output.Count * 100) / results.Length;

					progress.Report(report);
				});
			});

			return output;
		}

		private void ReportAddFilesToIndexProgress(object sender, ProgressReportModel e)
		{
			AnsiConsole.MarkupLine($" Adding [bold]{ e.Count }[/] files from [red]{ e.BaseDirectory }[/] ");
		}

		public async Task<List<ItemDataModel>> ScanIndex(IndexScanCommand.Settings settings)
		{
			Progress<ItemDataModel> progress = new Progress<ItemDataModel>();
			progress.ProgressChanged += ReportScanIndexProgress;
			var watch = System.Diagnostics.Stopwatch.StartNew();
			var result = await GetHashParallelAsync(progress, this.idx, settings, this.cts.Token);
			AnsiConsole.MarkupLine($"Total execution time: [bold]{ watch.ElapsedMilliseconds }[/]");
			return result;
		}

		public static async Task<List<ItemDataModel>> GetHashParallelAsync(IProgress<ItemDataModel> progress, IndexDataModel idx, IndexScanCommand.Settings settings, CancellationToken cancellationToken)
		{
			var report = new List<ItemDataModel>();

			// Todo: make use of the settings
			// var fsmin = idx.Where(t => t.Size > 0 && t.Size < settings.SizeMax);
			// var fspat = fsmin.Where(t => t.Path.Contains(settings.Pattern));

			IEnumerable<IGrouping<long, ItemDataModel>> filesizeduplicates =
				idx.GroupBy(f => f.Size, f => f);

			await Task.Run(() =>
			{
				Parallel.ForEach<IGrouping<long, ItemDataModel>>(filesizeduplicates, (g) =>
				{
					// Only when we have more than one file
					if (g.Count() > 1)
					{
						foreach (var sub in g)
						{
							// string checksum = CalculateMD5(item.Path);
							string checksum = CalculateSHA256(sub.Path);
							ItemDataModel result = new ItemDataModel() { Path = sub.Path, Hash = checksum };
							report.Add(result);

							// remember the hash
							sub.Hash = checksum;

							cancellationToken.ThrowIfCancellationRequested();

							// report progress
							progress.Report(result);
						}
					}
				});
			});

			return report;
		}

		private void ReportScanIndexProgress(object sender, ItemDataModel e)
		{
			AnsiConsole.MarkupLine($"Hashing file [bold]{ e.Path }[/] [red]{ e.Hash }[/] ");
			// Console.WriteLine($" Hashing file { e.Path } ");
			// Debug.Print($" Hashing file { e.Path } ");
		}

		public async Task<List<ItemDataModel>> UpdateIndex(IndexUpdateCommand.Settings settings)
		{
			Progress<IndexUpdateDataModel> progress = new Progress<IndexUpdateDataModel>();
			progress.ProgressChanged += ReportUpdateIndexProgress;
			var watch = System.Diagnostics.Stopwatch.StartNew();
			var result = await UpdateIndexAsync(progress, this.idx, settings, this.cts.Token);
			AnsiConsole.MarkupLine($"Total execution time: [bold]{ watch.ElapsedMilliseconds }[/]");
			return result;
		}

		internal static async Task<List<ItemDataModel>> UpdateIndexAsync(IProgress<IndexUpdateDataModel> progress, IndexDataModel idx, IndexUpdateCommand.Settings settings, CancellationToken cancellationToken)
		{
			var report = new List<IndexUpdateDataModel>();
			await Task.Run(() =>
			{
				Parallel.ForEach<ItemDataModel>(idx, (item) =>
				{
					IndexUpdateDataModel result = new IndexUpdateDataModel() { Path = item.Path };

					if (System.IO.File.Exists(item.Path))
					{
						long oldsize = item.Size;

						// Update file size
						FileInfo fi = new System.IO.FileInfo(item.Path);

						if (oldsize != fi.Length)
						{
							// update file size
							item.Size = fi.Length;

							// update checksum
							string checksum = CalculateSHA256(item.Path);
							item.Hash = checksum;

							result.Action = "Updating size and hash.";
						}
					}
					else
					{
						// idx.Remove(item);
						result.Action = "Removing entry";
						item.Path = string.Empty;
					}
					report.Add(result);

					cancellationToken.ThrowIfCancellationRequested();

					// report progress
					progress.Report(result);

				});
			});

			// remove non existing entries
			await Task.Run(() =>
			{
				for (int i = idx.Count - 1; i >= 0; i--)
				{
					if (idx[i].Path == string.Empty)
					{
						idx.RemoveAt(i);
					}
				}
			});

			// todo: add new directories/files

			// return
			return idx;
			// return report;
		}

		private void ReportUpdateIndexProgress(object sender, IndexUpdateDataModel e)
		{
			AnsiConsole.MarkupLine($"Updating index [bold]{ e.Path }[/] [red]{ e.Action }[/]");
		}

		internal void Dispose()
		{
			IndexDataModel.SaveToFile(this.idx);
		}
	}
}