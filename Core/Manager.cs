using System;
using System.Collections.Generic;
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

			var searchOptions = new EnumerationOptions
			{
				AttributesToSkip = true ? FileAttributes.Hidden | FileAttributes.System : FileAttributes.System,
				RecurseSubdirectories = true
			};

			// Get files
			var results = await GetFilesAsync(progress, settings.Path, settings.SearchPattern, searchOptions, settings.Recursive, cts.Token);
			// var results = await FileTools.GetFilesAsync(settings.Path, settings.SearchPattern, searchOptions, settings.Recursive, cts.Token);

			// add results to the index
			foreach (var item in results)
			{
				foreach (var subitem in item)
				{
					this.idx.Add(new ItemDataModel() { Path = subitem.FullName, Size = subitem.Length, Hash = string.Empty });
				}
			}

			// PrintResults(results);
		}

		public static async Task<List<FileInfo[]>> GetFilesAsync(IProgress<ProgressReportModel> progress, string basepath, string searchpattern, EnumerationOptions options, bool recursive, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(searchpattern))
				searchpattern = "*.*";

			// First get all directories
			var dirs = FileTools.EnumerateDirectoriesRecursive(basepath, searchpattern, recursive);

			// then get all files
			List<FileInfo[]> output = new List<FileInfo[]>();
			ProgressReportModel report = new ProgressReportModel();

			// Parallel async
			await Task.Run(() =>
			{
				Parallel.ForEach<DirectoryInfo>(dirs, (dir) =>
				{
					FileInfo[] results = dir.GetFiles(searchpattern, options);
					output.Add(results);

					cancellationToken.ThrowIfCancellationRequested();

					report.Files = output;
					// report.PercentageComplete = (output.Count * 100) / results.Length;
					progress.Report(report);
				});
			});
			return output;
		}

		private void ReportAddFilesToIndexProgress(object sender, ProgressReportModel e)
		{
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
			var result = await GetHashParallelAsync(progress, this.idx, settings, this.cts.Token);
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
			// 
		}

		public async Task<List<ItemDataModel>> UpdateIndex(IndexUpdateCommand.Settings settings)
		{
			Progress<ItemDataModel> progress = new Progress<ItemDataModel>();
			progress.ProgressChanged += ReportUpdateIndexProgress;
			var result = await UpdateIndexAsync(progress, this.idx, settings, this.cts.Token);
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