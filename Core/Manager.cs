using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

			// PrintResults(results);
		}

		public static async Task<List<FileInfo[]>> GetFilesAsync(IProgress<ProgressReportModel> progress, string basepath, string searchpattern, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(searchpattern))
				searchpattern = "*.*";

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
			// foreach (var item in e.Files)
			// {
			// 	Console.WriteLine($" Adding { e.Files.Count } files...");
			// 	foreach (FileInfo subitem in item)
			// 	{
			// 		Console.WriteLine($" Adding { subitem.FullName }");
			// 	}
			// }
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

			// var data = idx
			// .Where(t => t.Size > settings.SizeMin && t.Size < settings.SizeMax);
			//.Where(t => t.Path.Contains(settings.Pattern)
			// );

			var report = new List<ItemDataModel>();

			await Task.Run(() =>
			{
				Parallel.ForEach<ItemDataModel>(idx, (item) =>
				{
					// string checksum = CalculateMD5(item.Path);
					string checksum = CalculateSHA256(item.Path);
					ItemDataModel result = new ItemDataModel() { Path = item.Path, Hash = checksum };
					report.Add(result);

					// remember the hash
					item.Hash = checksum;

					cancellationToken.ThrowIfCancellationRequested();

					// report progress
					progress.Report(result);

				});
			});

			return report;
		}

		private void ReportScanIndexProgress(object sender, ItemDataModel e)
		{
			AnsiConsole.MarkupLine($" Hashing file [bold]{ e.Path }[/] [red]{ e.Hash }[/] ");
			// Console.WriteLine($" Hashing file { e.Path } ");
			// Debug.Print($" Hashing file { e.Path } ");
		}

		public async Task<List<ItemDataModel>> UpdateIndex(IndexUpdateCommand.Settings settings)
		{
			Progress<ItemDataModel> progress = new Progress<ItemDataModel>();
			progress.ProgressChanged += ReportUpdateIndexProgress;
			var watch = System.Diagnostics.Stopwatch.StartNew();
			var result = await UpdateIndexAsync(progress, this.idx, settings, this.cts.Token);
			AnsiConsole.MarkupLine($"Total execution time: [bold]{ watch.ElapsedMilliseconds }[/]");
			return result;
		}

		internal static async Task<List<ItemDataModel>> UpdateIndexAsync(Progress<ItemDataModel> progress, IndexDataModel idx, IndexUpdateCommand.Settings settings, CancellationToken cancellationToken)
		{
			// var report = new List<ItemDataModel>();

			await Task.Run(() =>
			{
				Parallel.ForEach<ItemDataModel>(idx, (item) =>
				{
					if (System.IO.File.Exists(item.Path))
					{
						// Update file size
						FileInfo fi = new System.IO.FileInfo(item.Path);
						item.Size = fi.Length;

						// update checksum
						string checksum = CalculateSHA256(item.Path);
						item.Hash = checksum;
					}
					else
					{
						// idx.Remove(item);
						item.Path = string.Empty;
					}

					cancellationToken.ThrowIfCancellationRequested();

					// report progress
					// progress.Report(result);

				});
			});

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

			// return
			//return report;
			return null;
		}

		private void ReportUpdateIndexProgress(object sender, ItemDataModel e)
		{
			// 
		}

		internal void Dispose()
		{
			IndexDataModel.SaveToFile(this.idx);
		}


		private void PrintResults(List<FileInfo[]> results)
		{
			// foreach (var item in results)
			// {
			// 	foreach (FileInfo subitem in item)
			// 	{
			// 		AnsiConsole.MarkupLine($" { subitem.FullName }");
			// 	}
			// }
		}

	}

}