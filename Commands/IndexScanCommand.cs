using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using dupefiles2.Core;

namespace dupefiles2.Commands
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
			// await m.IndexScanHash(settings);
			// await m.IndexCompareBinary(settings);


			// m.IndexScanHash(settings);
			// m.IndexCompareBinary(settings);

			// await AnsiConsole.Progress()
			// .StartAsync(async ctx =>
			// {
			// 	// Define tasks
			// 	var task1 = ctx.AddTask("[green]Hash check[/]");
			// 	var task2 = ctx.AddTask("[green]Binary check[/]");

			// 	// await m.IndexScanHash(settings);
			// 	// await m.IndexCompareBinary(settings);

			// 	while (!ctx.IsFinished)
			// 	{
			// 		task1.Increment(1.5);
			// 		task2.Increment(0.5);
			// 	}
			// });


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

		// private void Start(Settings settings)
		// {
		// 	Manager m = new Manager();

		// 	// hash
		// 	AnsiConsole.Status()
		// 	.Start("Scanning the index for size and hash duplicates...", ctx =>
		// 	{
		// 		m.IndexScanHash(settings);
		// 	});

		// 	// binary
		// 	AnsiConsole.Status()
		// 	.Start("Scanning the index for binary duplicates...", ctx =>
		// 	{
		// 		m.IndexCompareBinary(settings);
		// 	});

		// 	m.Dispose();
		// }


	}
}
