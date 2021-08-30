using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using todo.Commands;
using todo.Core;

namespace dupesfiles2.Core
{
	public class ScanTools
	{

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
					// ItemDataModel result = new ItemDataModel() { Path = item.Path, Hash = checksum };
					// report.Add(result);
					item.Hash = checksum;
					// cancellationToken.ThrowIfCancellationRequested();
					// report progress
					// progress.Report(result);

				});
			});
			return report;
		}

		private static string CalculateMD5(string filename)
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

		private static string CalculateSHA256(string filename)
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

		internal static async Task<List<ItemDataModel>> UpdateIndexAsync(Progress<ItemDataModel> progress, IndexDataModel idx, IndexUpdateCommand.Settings settings, CancellationToken token)
		{
			var report = new List<ItemDataModel>();
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


					// cancellationToken.ThrowIfCancellationRequested();

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
			return report;
		}
	}
}