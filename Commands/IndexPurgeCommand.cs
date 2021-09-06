using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using dupefiles2.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace dupefiles2.Commands
{
	[Description("Pugre files from the index.")]
	public sealed class IndexPurgeCommand : AsyncCommand<IndexPurgeCommand.Settings>
	{

		public enum PurgeMode
		{
			Nothing,
			Delete,
			Move,
			MoveToRecycleBin,
			ReplaceWithEmptyFile,
			ReplaceWithTextFile
		}

		public sealed class Settings : CommandSettings
		{
			[Description("Purge mode.")]
			[CommandOption("-m|--mode")]
			[DefaultValue(PurgeMode.Nothing)]
			public PurgeMode Mode { get; set; }

			[Description("Filter for file extensions. Defaults to *.* (every file).")]
			[CommandOption("-e|--extension")]
			[DefaultValue("*.*)")]
			public string SearchPattern { get; set; }

			[Description("Quiet mode. No individual selection of files.")]
			[CommandOption("-q|--quiet")]
			[DefaultValue(false)]
			public bool Quiet { get; set; }

			[Description("Simulation mode. No files get harmed.")]
			[CommandOption("-s|--simulate")]
			[DefaultValue(false)]
			public bool Simulate { get; set; }

			[Description("Verbose mode.")]
			[CommandOption("-v|--verbose")]
			[DefaultValue(false)]
			public bool Verbose { get; set; }
		}

		public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
		{
			Manager m = new Manager();
			await AnsiConsole.Status()
				.StartAsync("Purging...", async ctx =>
				{
					ctx.SpinnerStyle(Style.Parse("green"));
					ctx.Spinner(Spinner.Known.Star);
					await m.IndexPurge(settings);
				});

			// AnsiConsole.MarkupLine("[green]Done! Index has now {} files.[/]");
			m.Dispose();
			return 0;
		}
	}
}