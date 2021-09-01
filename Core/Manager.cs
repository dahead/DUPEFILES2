using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using dupefiles2.Commands;
using static dupefiles2.Core.FileTools;

namespace dupefiles2.Core
{
	public class Manager
	{

		CancellationTokenSource cts = new CancellationTokenSource();

		private IndexDataModel idx { get; set; } = new IndexDataModel();

		public Manager()
		{
			this.idx = IndexDataModel.LoadFromFile();
		}

		internal void Dispose()
		{
			IndexDataModel.SaveToFile(this.idx);
		}

		#region "Add files"

		public async Task<List<FileInfo[]>> IndexAdd(IndexAddCommand.Settings settings)
		{
			Progress<IndexAddDataModel> progress = new Progress<IndexAddDataModel>();
			progress.ProgressChanged += ReportAddFilesToIndexProgress;
			// var watch = System.Diagnostics.Stopwatch.StartNew();

			// Get files
			var results = await GetFilesAsync(progress, settings, cts.Token);

			// add results to the index
			foreach (var item in results)
			{
				foreach (var subitem in item)
				{
					if (!idx.ContainsItem(subitem.FullName))
					{
						this.idx.Add(new IndexItemDataModel() { FullName = subitem.FullName, DirectoryName = subitem.DirectoryName, Size = subitem.Length, Hash = string.Empty });
					}
				}
			}

			// AnsiConsole.MarkupLine($"Total execution time: [bold]{ watch.ElapsedMilliseconds }[/]");

			return results;
		}

		private static async Task<List<FileInfo[]>> GetFilesAsync(IProgress<IndexAddDataModel> progress, IndexAddCommand.Settings settings, CancellationToken cancellationToken)
		{
			// Setup base search path
			if (string.IsNullOrWhiteSpace(settings.SearchPattern))
				settings.SearchPattern = "*";

			// Create search options
			var searchOptions = new EnumerationOptions
			{
				AttributesToSkip = true ? FileAttributes.Hidden | FileAttributes.System : FileAttributes.System,
				RecurseSubdirectories = false
			};

			// Add the base directory to the dirs list
			var dirs = new List<DirectoryInfo>();
			dirs.Add(new DirectoryInfo(settings.Path));

			// First get all directories...
			var subdirs = FileTools.EnumerateDirectoriesRecursive(settings.Path, settings.SearchPattern, searchOptions, true);
			foreach (var item in subdirs)
				dirs.Add(item);
			// ...then get all files
			List<FileInfo[]> output = new List<FileInfo[]>();

			// Parallel async
			await Task.Run(() =>
			{
				Parallel.ForEach<DirectoryInfo>(dirs, (dir) =>
				{
					// get files in THIS directory ONLY. NON recursive!
					FileInfo[] results = dir.GetFiles(settings.SearchPattern, searchOptions);
					output.Add(results);

					cancellationToken.ThrowIfCancellationRequested();

					IndexAddDataModel report = new IndexAddDataModel();
					report.BaseDirectory = dir.FullName;
					report.Count = results.Length;
					// report.PercentageComplete = (output.Count * 100) / results.Length;

					progress.Report(report);
				});
			});

			return output;
		}

		private void ReportAddFilesToIndexProgress(object sender, IndexAddDataModel e)
		{
			AnsiConsole.MarkupLine($" Adding [bold]{ e.Count }[/] files from [red]{ e.BaseDirectory }[/] ");
		}

		#endregion

		#region "Scan index"

		public async Task<List<IndexItemDataModel>> IndexScanHash(IndexScanCommand.Settings settings)
		{
			Progress<IndexItemDataModel> progress = new Progress<IndexItemDataModel>();
			progress.ProgressChanged += ReportScanIndexProgress;
			// var watch = System.Diagnostics.Stopwatch.StartNew();
			var result = await GetHashParallelAsync(progress, this.idx, settings, this.cts.Token);
			// AnsiConsole.MarkupLine($"Total execution time: [bold]{ watch.ElapsedMilliseconds }[/]");
			return result;
		}

		private static async Task<List<IndexItemDataModel>> GetHashParallelAsync(IProgress<IndexItemDataModel> progress, IndexDataModel idx, IndexScanCommand.Settings settings, CancellationToken cancellationToken)
		{
			var report = new List<IndexItemDataModel>();

			IEnumerable<IGrouping<long, IndexItemDataModel>> filesizeduplicates =
				idx.GroupBy(f => f.Size, f => f);

			await Task.Run(() =>
			{
				Parallel.ForEach<IGrouping<long, IndexItemDataModel>>(filesizeduplicates, (g) =>
				{
					// Only when we have more than one file
					if (g.Count() > 1)
					{
						var filtered = g.Where(t => t.Size > settings.SizeMin && t.Size < settings.SizeMax);
						foreach (var sub in filtered)
						{
							// string checksum = CalculateMD5(item.Path);
							string checksum = CalculateSHA256(sub.FullName);
							IndexItemDataModel result = new IndexItemDataModel() { FullName = sub.FullName, DirectoryName = sub.DirectoryName, Size = sub.Size, Hash = checksum };
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

		private void ReportScanIndexProgress(object sender, IndexItemDataModel e)
		{
			AnsiConsole.MarkupLine($"Hashing file [bold]{ e.FullName }[/] [red]{ e.Hash }[/] ");
		}

		#endregion

		#region "Compare index"

		public async Task<List<IndexCompareDataModel>> IndexCompareBinary(IndexScanCommand.Settings settings)
		{
			Progress<IndexCompareDataModel> progress = new Progress<IndexCompareDataModel>();
			progress.ProgressChanged += ReportCompareIndexProgress;
			// var watch = System.Diagnostics.Stopwatch.StartNew();
			var result = await GetBinaryParallelAsync(progress, this.idx, settings, this.cts.Token);
			// AnsiConsole.MarkupLine($"Total execution time: [bold]{ watch.ElapsedMilliseconds }[/]");
			PrintIndexCompareResults(result);
			return result;
		}

		private static void PrintIndexCompareResults(List<IndexCompareDataModel> result)
		{

			var group = result.Where(t => t.Identical == true).GroupBy(f => f.Hash, f => f);

			if (group.Count() == 0)
			{
				AnsiConsole.MarkupLine("[green]No duplicates found in the index.[/]");
				return;
			}

			foreach (var item in group)
			{
				if (item.Count() == 0)
				{
					continue;
				}
				var first = item.ToList()[0];
				AnsiConsole.MarkupLine($"[bold]Hash group [red]{ first.Hash }[/][/] Size: { first.Size.BytesToString() }");
				foreach (var sub in item)
				{
					AnsiConsole.MarkupLine($":white_small_square: { sub.Fullname1 }");
					AnsiConsole.MarkupLine($":white_small_square: { sub.Fullname2 }");
				}

			}

		}

		private static async Task<List<IndexCompareDataModel>> GetBinaryParallelAsync(IProgress<IndexCompareDataModel> progress, IndexDataModel idx, IndexScanCommand.Settings settings, CancellationToken cancellationToken)
		{
			var report = new List<IndexCompareDataModel>();

			IEnumerable<IGrouping<string, IndexItemDataModel>> filehashduplicates =
				idx.GroupBy(f => f.Hash, f => f);

			await Task.Run(() =>
			{
				Parallel.ForEach<IGrouping<string, IndexItemDataModel>>(filehashduplicates, (g) =>
				{
					if (g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key))
						for (int i = 0; i < g.ToList().Count() - 1; i++)
						{
							var cur = g.ToList()[i];
							var next = g.ToList()[i + 1];

							// dont compare the file with itself
							if (cur.FullName == next.FullName)
								continue;

							// AnsiConsole.WriteLine($"File: { cur.Path } { next.Path }");
							// AnsiConsole.WriteLine($"Hash: { cur.Hash } { next.Hash }");

							var identical = FileTools.BinaryCompareFiles(cur.FullName, next.FullName);

							IndexCompareDataModel result = new IndexCompareDataModel() { Hash = cur.Hash, Size = cur.Size, Fullname1 = cur.FullName, Fullname2 = next.FullName, Identical = identical };
							report.Add(result);

							cancellationToken.ThrowIfCancellationRequested();

							// report progress
							progress.Report(result);
						}
				});
			});

			return report;
		}

		private void ReportCompareIndexProgress(object sender, IndexCompareDataModel e)
		{
			switch (e.Identical)
			{
				case true:
					AnsiConsole.MarkupLine($"[red bold]DUPE[/] [grey]{ e.Fullname1 }[/] and [grey]{ e.Fullname2 }[/]");
					break;
				// case false:
				// 	AnsiConsole.MarkupLine($"[grey]nodupe[/] [green]{ e.File1 }[/] and [green]{ e.File2 }[/] [yellow bold]{ e.Identical }[/]");
				// 	break;
				default:
					break;
			}
		}

		#endregion

		#region "Update index"

		public async Task<List<IndexItemDataModel>> UpdateIndex(IndexUpdateCommand.Settings settings)
		{
			Progress<IndexUpdateDataModel> progress = new Progress<IndexUpdateDataModel>();
			progress.ProgressChanged += ReportUpdateIndexProgress;
			// var watch = System.Diagnostics.Stopwatch.StartNew();
			var result = await UpdateIndexAsync(progress, this.idx, settings, this.cts.Token);
			// AnsiConsole.MarkupLine($"Total execution time: [bold]{ watch.ElapsedMilliseconds }[/]");
			return result;
		}

		internal static async Task<List<IndexItemDataModel>> UpdateIndexAsync(IProgress<IndexUpdateDataModel> progress, IndexDataModel idx, IndexUpdateCommand.Settings settings, CancellationToken cancellationToken)
		{

			// remove parameter idx and instead later on use the result of UpdateIndexAsync and make changes
			// to the MAIN index accordlingly?

			// var report = new List<IndexUpdateDataModel>();

			await Task.Run(() =>
			{
				Parallel.ForEach<IndexItemDataModel>(idx, (item) =>
				{
					IndexUpdateDataModel result = new IndexUpdateDataModel() { FullName = item.FullName };
					if (System.IO.File.Exists(item.FullName))
					{
						// check for changed file size
						long oldsize = item.Size;
						FileInfo fi = new System.IO.FileInfo(item.FullName);

						// Update file size
						if (oldsize != fi.Length)
						{
							// update file size
							item.Size = fi.Length;

							// update checksum
							string checksum = CalculateSHA256(item.FullName);
							item.Hash = checksum;
							result.Action = "[red]updated[/]";
						}
					}
					else
					{
						// idx.Remove(item);
						result.Action = "[red]removed[/]";
						item.FullName = string.Empty;
					}

					// report
					// report.Add(result);

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
					if (idx[i].FullName == string.Empty)
					{
						idx.RemoveAt(i);
					}
				}
			});

			// Add new directories/files based on the directories already present
			IndexItemList newitemlist = new IndexItemList();
			await Task.Run(() =>
			{
				var dirs =
					idx.GroupBy(f => f.DirectoryName, f => f).ToList();

				foreach (var dir in dirs)
				{
					foreach (var sub in dir)
					{
						IndexUpdateDataModel result = new IndexUpdateDataModel() { FullName = sub.FullName };
						result.Action = "[yellow]checked[/]";
						var files = new DirectoryInfo(sub.DirectoryName).GetFiles();
						foreach (var pnf in files)
						{
							if (!idx.ContainsItem(pnf.FullName))
							{
								idx.Add(new IndexItemDataModel() { FullName = pnf.FullName, DirectoryName = pnf.DirectoryName, Size = pnf.Length });
								result.Action = "[green]added[/]";
							}
						}
						// report progress
						progress.Report(result);
					}

				}
			});

			return idx;
		}

		private void ReportUpdateIndexProgress(object sender, IndexUpdateDataModel e)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(e.Action))
				{
					AnsiConsole.MarkupLine($"Updating index [bold]{ e.FullName }[/] [red]{ e.Action }[/]");
				}
			}
			catch (System.Exception)
			{
				// Spectre.Console
				// Sometimes there are invalid characters				
				// System.InvalidOperationException: Could not find color or style 'MV'.
				//    at Spectre.Console.StyleParser.Parse(String text) in / _ / src / Spectre.Console / StyleParser.cs:line 16
			}
		}

		#endregion
	}
}