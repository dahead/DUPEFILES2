using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using todo.Core;

namespace dupesfiles2.Core
{
	public class ScanTools
	{

		public static async Task<List<ItemDataModel>> GetHashParallelAsync(IProgress<ItemDataModel> progress, IndexDataModel idx, CancellationToken cancellationToken)
		{
			var report = new List<ItemDataModel>();
			await Task.Run(() =>
			{
				Parallel.ForEach<ItemDataModel>(idx, (item) =>
				{
					string checksum = CalculateSHA256(item.Path);
					// string checksum = string.Empty;

					// Todo: check for binary equality...
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

		private static string CalculateSHA256(string filename)
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
			catch (System.Exception ex)
			{
				return ex.Message;
			}
		}

	}
}