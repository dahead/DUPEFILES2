using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using dupesfiles2.Core;

namespace dupesfiles2.Commands
{
	[Description("Scans the index for duplicate files.")]
	public sealed class IndexScanCommand : AsyncCommand<IndexScanCommand.Settings>
	{
		public sealed class Settings : CommandSettings
		{
			[Description("Scan pattern. Defaults to *.* (every file).")]
			[CommandOption("-p|--pattern")]
			public string Pattern { get; set; }

			[Description("Minimum file size in bytes. Defaults to 1MB.")]
			[CommandOption("-a|--min")]
			[DefaultValue(long.MinValue)]
			public long SizeMin { get; set; }

			[Description("Maximum file size in bytes. Defaults to 0, no limit.")]
			[CommandOption("-z|--max")]
			[DefaultValue(long.MaxValue)]
			public long SizeMax { get; set; }
		}

		public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
		{
			Manager m = new Manager();

			// hash
			await AnsiConsole.Status()
			.StartAsync("Scanning the index for size and hash duplicates...", async ctx =>
			{
				await m.IndexScanHash(settings);
			});

			// binary
			await AnsiConsole.Status()
			.StartAsync("Scanning the index for binary duplicates...", async ctx =>
			{
				await m.IndexCompareBinary(settings);
			});

			// save and return
			m.Dispose();
			return 0;
		}


	}
}
