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

		public IndexDataModel idx { get; set; }
		CancellationTokenSource cts = new CancellationTokenSource();

		public Manager()
		{
			this.idx = IndexDataModel.LoadFromFile();
		}


		public async void AddFiles(Settings settings)
		{
			Progress<ProgressReportModel> progress = new Progress<ProgressReportModel>();
			progress.ProgressChanged += ReportProgress;

			var searchOptions = new EnumerationOptions
			{
				AttributesToSkip = true ? FileAttributes.Hidden | FileAttributes.System : FileAttributes.System,
				RecurseSubdirectories = true
			};

			var results = await FileTools.GetFilesAsync(progress, settings.Path, settings.SearchPattern, searchOptions);
			PrintResults(results);
		}

		public async Task ExecuteScanParallelAsync()
		{
			Progress<ItemDataModel> progress = new Progress<ItemDataModel>();
			// progress.ProgressChanged += ReportProgress;
			var results = await GetHashParallelAsync(this.idx, progress, cts.Token);
		}

		public async Task<List<ItemDataModel>> GetHashParallelAsync(IndexDataModel idx, IProgress<ItemDataModel> progress, CancellationToken cancellationToken)
		{
			var report = new List<ItemDataModel>();
			await Task.Run(() =>
			{
				Parallel.ForEach<ItemDataModel>(idx, (item) =>
				{
					// Ping host, get result, add result to output
					string checksum = CalculateSHA256(item.Path);

					// check for binary equality...

					ItemDataModel result = new ItemDataModel() { Path = item.Path, Hash = checksum };
					report.Add(result);

					cancellationToken.ThrowIfCancellationRequested();

					// report progress
					progress.Report(result);
				});
			});

			return report;
		}

		private void ReportProgress(object sender, ProgressReportModel e)
		{
			foreach (var item in e.SitesDownloaded)
			{
				foreach (FileInfo subitem in item)
				{
					AnsiConsole.MarkupLine($" { subitem.FullName }");
				}
			}
		}

		private void PrintResults(List<FileInfo[]> results)
		{
			foreach (var item in results)
			{
				foreach (FileInfo subitem in item)
				{
					AnsiConsole.MarkupLine($" { subitem.FullName }");
				}
			}
		}


		private string CalculateSHA256(string filename)
		{
			try
			{
				using (var sha = SHA256.Create())
				{
					using (var stream = new BufferedStream(File.OpenRead(filename), 1200000))
					{
						var hash = sha.ComputeHash(stream);
						return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
					}
				}
			}
			catch (System.Exception)
			{
				return string.Empty;
			}
		}
	}

}