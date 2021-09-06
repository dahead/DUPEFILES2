using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using dupefiles2.Commands;
using static dupefiles2.Core.FileTools;
using System.Diagnostics;

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
			// // Parallel async
			// await Task.Run(() =>
			// {
			// 	Parallel.ForEach<FileInfo[]>(results, (item) =>
			// 	{
			// 		foreach (var subitem in item)
			// 		{
			// 			if (!idx.ContainsItem(subitem.FullName))
			// 			{
			// 				this.idx.Add(new IndexItemDataModel() { FullName = subitem.FullName, DirectoryName = subitem.DirectoryName, Size = subitem.Length, Hash = string.Empty });
			// 			}
			// 		}
			// 	});
			// });

			// add results to the index
			foreach (var item in results)
				foreach (var subitem in item)
					if (!idx.ContainsItem(subitem.FullName))
						this.idx.Add(new IndexItemDataModel()
						{ FullName = subitem.FullName, DirectoryName = subitem.DirectoryName, Size = subitem.Length, Hash = string.Empty });

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
			// Parallel async
			List<FileInfo[]> output = new List<FileInfo[]>();
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

					if (settings.Verbose)
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

		public async Task<IndexItemList> IndexScanHash(IndexScanCommand.Settings settings)
		{
			Progress<IndexItemDataModel> progress = new Progress<IndexItemDataModel>();
			progress.ProgressChanged += ReportScanIndexProgress;

#if DEBUG
			var watch = System.Diagnostics.Stopwatch.StartNew();
#endif
			var result = await GetHashParallelAsync(progress, this.idx, settings, this.cts.Token);

#if DEBUG
			AnsiConsole.MarkupLine($"Total execution time: [bold]{ watch.ElapsedMilliseconds }[/]");
#endif

			return result;
		}

		private static async Task<IndexItemList> GetHashParallelAsync(IProgress<IndexItemDataModel> progress, IndexDataModel idx, IndexScanCommand.Settings settings, CancellationToken cancellationToken)
		{
			var report = new IndexItemList();

			IEnumerable<IGrouping<long, IndexItemDataModel>> filesizeduplicates =
				idx.
				// only check files we have no hash already
				Where(t => t.Hash == string.Empty).
				Where(t => t.Size > 0).
				// group by file size
				GroupBy(f => f.Size, f => f).
				// Only when we have more than one file in this group
				Where(t => t.Count() > 1);

			await Task.Run(() =>
			{
				Parallel.ForEach<IGrouping<long, IndexItemDataModel>>(filesizeduplicates, (g) =>
				{
					// Only when we have more than one file in this group
					// if (g.Count() > 1)
					{
						// var filtered = g.Where(t => t.Size > settings.SizeMin && t.Size < settings.SizeMax);
						foreach (var sub in g)
						{
							string checksum = CalculateSHA256(sub.FullName);
							IndexItemDataModel result = new IndexItemDataModel() { FullName = sub.FullName, DirectoryName = sub.DirectoryName, Size = sub.Size, Hash = checksum };
							report.Add(result);

							// remember the hash
							sub.Hash = checksum;

							cancellationToken.ThrowIfCancellationRequested();

							// report progress
							if (settings.Verbose)
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

			// Show found duplicates
			PrintIndexCompareResults(settings, result);

			return result;
		}

		private static async Task<IndexCompareDataModelList> GetBinaryParallelAsync(IProgress<IndexCompareDataModel> progress, IndexDataModel idx, IndexScanCommand.Settings settings, CancellationToken cancellationToken)
		{
			var report = new IndexCompareDataModelList();

			IEnumerable<IGrouping<string, IndexItemDataModel>> filehashduplicates =
				idx.
				// Only when we have calculated a hash
				Where(t => t.Hash != string.Empty && t.IsDupe == false).
				GroupBy(f => f.Hash, f => f).
				// Only when we have more than one file in this group
				Where(t => t.Count() > 1);

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

							// if (cur.Hash != next.Hash)
							// {
							// 	Debug.Print("SHOULD NOT HAPPEN!");
							// 	continue;
							// }

							if (!System.IO.File.Exists(cur.FullName) || !System.IO.File.Exists(next.FullName))
							{
								// update the index?
								continue;
							}

							cancellationToken.ThrowIfCancellationRequested();

							// AnsiConsole.WriteLine($"File: { cur.Path } { next.Path }");
							// AnsiConsole.WriteLine($"Hash: { cur.Hash } { next.Hash }");

							// Compare files binary
							try
							{
								var identical = FileTools.BinaryCompareFiles(cur.FullName, next.FullName, cur.Size);
								if (identical)
								{
									IndexCompareDataModel result = new IndexCompareDataModel() { Hash = cur.Hash, Size = cur.Size, Fullname1 = cur.FullName, Fullname2 = next.FullName, Identical = identical };
									report.Add(result);

									// mark found items in the index as duplicates
									idx.MarkDuplicates(result);

									// report progress
									if (settings.Verbose)
										progress.Report(result);
								}
							}
							catch (System.Exception)
							{
							}
						}
				});
			});

			return report;
		}

		private static void PrintIndexCompareResults(IndexScanCommand.Settings settings, IndexCompareDataModelList result)
		{
			var group = result.Where(t => t.Identical == true).GroupBy(f => f.Hash, f => f);

			if (group.Count() == 0)
			{
				AnsiConsole.MarkupLine("[green]No duplicates found in the index.[/]");
				return;
			}

			// tree output
			if (settings.Tree)
			{
				var tree =
					new Tree($"[yellow]Duplicates:[/]")
					.Style(Style.Parse("red"))
					.Guide(TreeGuide.Ascii);
				foreach (var item in group)
				{
					if (item.Count() == 0)
						continue;
					var first = item.ToList()[0];
					var foo = tree.AddNode($"[yellow]Hash group { first.Hash }:[/] Size: { first.Size.BytesToString() }");
					foreach (var sub in item)
					{
						foo.AddNode(sub.Fullname1);
						foo.AddNode(sub.Fullname2);
					}
				}
				AnsiConsole.Render(tree);
			}

			// table output
			if (settings.Table)
			{
				var table = new Table()
					.Title($"[yellow]Duplicates[/]")
					.Border(TableBorder.Ascii)
					.AddColumn(new TableColumn("Hash"))
					.AddColumn(new TableColumn("Files"));

				foreach (var item in group)
				{
					if (item.Count() == 0)
						continue;
					var first = item.ToList()[0];
					table.AddRow(first.Hash, first.Size.BytesToString());
					foreach (var sub in item)
					{
						table.AddRow(string.Empty, sub.Fullname1);
						table.AddRow(string.Empty, sub.Fullname2);
					}

				}
				AnsiConsole.Render(table);
			}

			// normal console output
			if (!settings.Table)
			{
				foreach (var item in group)
				{
					if (item.Count() == 0)
						continue;
					var first = item.ToList()[0];
					// AnsiConsole.MarkupLine($"[bold]Hash group [red]{ first.Hash }[/][/] Size: { first.Size.BytesToString() }");
					AnsiConsole.WriteLine($"Hash group { first.Hash } Size: { first.Size.BytesToString() }");
					foreach (var sub in item)
					{
						// AnsiConsole.MarkupLine($":white_small_square: { sub.Fullname1 }");
						// AnsiConsole.MarkupLine($":white_small_square: { sub.Fullname2 }");
						AnsiConsole.WriteLine(sub.Fullname1);
						AnsiConsole.WriteLine(sub.Fullname2);
					}
				}
			}

		}

		// private Color[] colors =
		// {
		// 	Color.Red,
		// 	Color.Green,
		// 	Color.Blue,
		// 	Color.Lime,
		// 	Color.SkyBlue1,
		// 	Color.Fuchsia,
		// };

		// private Color GetRandomColor(string hash)
		// {
		// 	Random r = new Random();
		// 	Color.FromArgb(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256));
		// }

		private void ReportCompareIndexProgress(object sender, IndexCompareDataModel e)
		{
			switch (e.Identical)
			{
				case true:
					AnsiConsole.MarkupLine($"[red bold]DUPE FILE PAIR FOUND[/] [grey]{ e.Fullname1 }[/] and [grey]{ e.Fullname2 }[/]");
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

		public async Task<IndexItemList> UpdateIndex(IndexUpdateCommand.Settings settings)
		{
			Progress<IndexUpdateDataModel> progress = new Progress<IndexUpdateDataModel>();
			progress.ProgressChanged += ReportUpdateIndexProgress;
			// var watch = System.Diagnostics.Stopwatch.StartNew();
			var result = await UpdateIndexAsync(progress, this.idx, settings, this.cts.Token);
			// AnsiConsole.MarkupLine($"Total execution time: [bold]{ watch.ElapsedMilliseconds }[/]");
			return result;
		}

		internal static async Task<IndexItemList> UpdateIndexAsync(IProgress<IndexUpdateDataModel> progress, IndexDataModel idx, IndexUpdateCommand.Settings settings, CancellationToken cancellationToken)
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

							// dont update checksum. calculate it when needed later in the scan.
							// update checksum
							// string checksum = CalculateSHA256(item.FullName);
							// item.Hash = checksum;

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
					if (settings.Verbose)
						progress.Report(result);

				});
			});

			// remove non existing entries
			await Task.Run(() =>
			{
				for (int i = idx.Count - 1; i >= 0; i--)
					if (idx[i].FullName == string.Empty)
						idx.RemoveAt(i);
			}
			);

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
						IndexUpdateDataModel result = new IndexUpdateDataModel() { FullName = sub.FullName, Action = "[green]checked[/]" };
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
						if (settings.Verbose)
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
				//
				// Sometimes there are invalid characters				
				// System.InvalidOperationException: Could not find color or style 'MV'.
				//    at Spectre.Console.StyleParser.Parse(String text) in / _ / src / Spectre.Console / StyleParser.cs:line 16
			}
		}

		#endregion

		#region "Purge Index"

		public async Task<List<IndexPurgeDataModel>> IndexPurge(IndexPurgeCommand.Settings settings)
		{
			Progress<IndexPurgeDataModel> progress = new Progress<IndexPurgeDataModel>();
			progress.ProgressChanged += ReportPurgeIndexProgress;
			// var watch = System.Diagnostics.Stopwatch.StartNew();
			var result = await PurgeIndexParallelAsync(progress, this.idx, settings, this.cts.Token);
			// AnsiConsole.MarkupLine($"Total execution time: [bold]{ watch.ElapsedMilliseconds }[/]");
			PrintIndexPurgeResults(settings, result);
			return result;
		}

		private static async Task<List<IndexPurgeDataModel>> PurgeIndexParallelAsync(Progress<IndexPurgeDataModel> progress, IndexDataModel idx, IndexPurgeCommand.Settings settings, CancellationToken token)
		{
			var result = new List<IndexPurgeDataModel>();
			// Todo...
			// These functions dont really need to be parallel and or asynchronous.

			// Ask the use what we shall do for each duplicate?
			// Mark files to delete by user?

			// remove non existing entries
			await Task.Run(() =>
			{

				IEnumerable<IGrouping<string, IndexItemDataModel>> filehashduplicates =
					idx.
					// Only when we have calculated a hash
					Where(t => t.Hash != string.Empty && t.IsDupe == true).
					GroupBy(f => f.Hash, f => f).
					// Only when we have more than one file in this group
					Where(t => t.Count() > 1);

				int current_group = 0;
				int count_groups = filehashduplicates.Count();

				// automaticallay mark files
				// foreach (var item in filehashduplicates)
				// {
				// 	foreach (var sub in item)
				// 	{

				// 	}
				// }


				switch (settings.Mode)
				{
					case IndexPurgeCommand.PurgeMode.Nothing:
						break;
					case IndexPurgeCommand.PurgeMode.Delete:
						break;
				}

				// 

				if (settings.Quiet)
				{

				}

				foreach (var item in filehashduplicates)
				{

					current_group++;

					var first = item.ToList()[0];

					var p = new MultiSelectionPrompt<string>()
							.Title($"{current_group} / {count_groups} Select one ore more files of this group [green]{ first.Hash }[/][red bold] { first.Size.BytesToString() }[/] for further action.")
							.InstructionsText("[grey](Press [green]<space>[/] to toggle, [green]<enter>[/] to proceed)[/]")
							.NotRequired()
							.PageSize(10);

					// get all files to possibly delete into an array for display
					string[] subfiles = new string[item.Count()];
					for (int i = 0; i < item.Count(); i++)
					{
						subfiles[i] = item.ToList()[i].FullName;
					}

					// add choice group
					p.AddChoiceGroup(first.Hash, subfiles);

					var result = AnsiConsole.Prompt(p);

					// AnsiConsole.MarkupLine("You selected: " + result);
				}


			});

			return result;
		}

		private void ReportPurgeIndexProgress(object sender, IndexPurgeDataModel e)
		{
			// Todo...
			throw new NotImplementedException();
		}
		private void PrintIndexPurgeResults(IndexPurgeCommand.Settings settings, List<IndexPurgeDataModel> result)
		{
			// Todo...
			throw new NotImplementedException();
		}

		#endregion


		// https://devblogs.microsoft.com/dotnet/file-io-improvements-in-dotnet-6/
		// async Task ThreadSafeAsync(string path, IReadOnlyList<ReadOnlyMemory<byte>> buffers)
		// {
		// 	using SafeFileHandle handle = File.OpenHandle( // new API (preview 6)
		// 		path, FileMode.Open, FileAccess.Read, FileShare.None, FileOptions.Asynchronous);

		// 	long offset = 0;
		// 	for (int i = 0; i < buffers.Count; i++)
		// 	{
		// 		await RandomAccess.WriteAsync(handle, buffers[i], offset); // new API (preview 7)
		// 		offset += buffers[i].Length;
		// 	}
		// }

	}
}