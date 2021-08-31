using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using dupefiles2.Core;

namespace dupefiles2.Commands
{
	[Description("Update the index.")]
	public sealed class IndexUpdateCommand : AsyncCommand<IndexUpdateCommand.Settings>
	{
		public sealed class Settings : CommandSettings
		{
			// [Description("Path to search. Defaults to current directory.")]
			// [CommandArgument(0, "[PATH]")]
			// public string Path { get; set; }

			// [Description("Search pattern. Defaults to *.* (every file).")]
			// [CommandOption("-p|--pattern")]
			// public string SearchPattern { get; set; }

			// [Description("Include hidden files. Defaults to false.")]
			// [CommandOption("--hidden")]
			// [DefaultValue(false)]
			// public bool IncludeHidden { get; set; }

			// [Description("Include all files in all sub directories. Defaults to false.")]
			// [CommandOption("--recursive")]
			// [DefaultValue(false)]
			// public bool Recursive { get; set; }
		}

		public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
		{
			Manager m = new Manager();
			await AnsiConsole.Status()
			.StartAsync("Updating the index...", async ctx =>
			{
				await m.UpdateIndex(settings);
			});
			AnsiConsole.WriteLine("Updated in the index.");
			m.Dispose();
			return 0;
		}
	}
}
