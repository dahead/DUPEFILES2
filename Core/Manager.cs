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
			// Progress<ProgressReportModel> progress = new Progress<ProgressReportModel>();
			// progress.ProgressChanged += ReportAddFilesToIndexProgress;

			var searchOptions = new EnumerationOptions
			{
				AttributesToSkip = true ? FileAttributes.Hidden | FileAttributes.System : FileAttributes.System,
				RecurseSubdirectories = true
			};

			// Get files
			// var results = await FileTools.GetFilesAsync(progress, settings.Path, settings.SearchPattern, searchOptions, settings.Recursive, cts.Token);
			var results = await FileTools.GetFilesAsync(settings.Path, settings.SearchPattern, searchOptions, settings.Recursive, cts.Token);

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
		private void ReportAddFilesToIndexProgress(object sender, ProgressReportModel e)
		{
			foreach (var item in e.Files)
			{
				Console.WriteLine($" Adding { e.Files.Count } files...");
				foreach (FileInfo subitem in item)
				{
					Console.WriteLine($" Adding { subitem.FullName }");
				}
			}
		}

		public async Task ScanIndex(IndexScanCommand.Settings settings)
		{
			Progress<ItemDataModel> progress = new Progress<ItemDataModel>();
			progress.ProgressChanged += ReportScanIndexProgress;

			var searchOptions = new EnumerationOptions
			{
				AttributesToSkip = true ? FileAttributes.Hidden | FileAttributes.System : FileAttributes.System,
				RecurseSubdirectories = true
			};

			await ScanTools.GetHashParallelAsync(progress, this.idx, this.cts.Token);
		}

		private void ReportScanIndexProgress(object sender, ItemDataModel e)
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