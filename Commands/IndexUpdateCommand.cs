using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;

namespace todo.Commands
{
	[Description("Update the index.")]
	public sealed class IndexUpdateCommand : AsyncCommand<IndexUpdateCommand.Settings>
	{
		public sealed class Settings : CommandSettings
		{
			[Description("Path to search. Defaults to current directory.")]
			[CommandArgument(0, "[PATH]")]
			public string Path { get; set; }

			[Description("Search pattern. Defaults to *.* (every file).")]
			[CommandOption("-p|--pattern")]
			public string SearchPattern { get; set; }

			[Description("Include hidden files. Defaults to false.")]
			[CommandOption("--hidden")]
			[DefaultValue(false)]
			public bool IncludeHidden { get; set; }

			[Description("Include all files in all sub directories. Defaults to false.")]
			[CommandOption("--recursive")]
			[DefaultValue(false)]
			public bool Recursive { get; set; }
		}

		public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
		{

			AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.Start("Getting files sizes...", ctx =>
			{
				// AnsiConsole.MarkupLine($"Total file size for [green]{searchPattern}[/] files in [green]{searchPath}[/]: [blue]{totalFileSize:N0}[/] bytes");
			});

			return 0;
		}
	}
}
