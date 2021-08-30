using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using dupesfiles2.Core;
using Spectre.Console;
using Spectre.Console.Cli;
using todo.Core;

namespace todo.Commands
{
	[Description("Add a todo item.")]
	public sealed class IndexScanCommand : AsyncCommand<IndexScanCommand.Settings>
	{
		public sealed class Settings : CommandSettings
		{
			[Description("Scan pattern. Defaults to *.* (every file).")]
			[CommandOption("-p|--pattern")]
			public string Pattern { get; set; }

			[Description("Lowest file size in bytes. Defaults to 1MB.")]
			[CommandOption("-l|--low")]
			[DefaultValue(1024 * 1024)]
			public int SizeMin { get; set; }

			[Description("Highest file size in bytes. Defaults to 0, no limit.")]
			[CommandOption("-h|--high")]
			[DefaultValue(0)]
			public int SizeMax { get; set; }
		}

		public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
		{

			Manager m = new Manager();

			await AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.Start($"Calculting SHA256 hashes...", async ctx =>
			{
				var task = m.ExecuteScanParallelAsync();
				task.Wait();
			});

			return 0;
		}


	}
}
