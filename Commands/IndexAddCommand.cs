using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using dupesfiles2.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace todo.Commands
{
	[Description("Add a todo item.")]
	public sealed class IndexAddCommand : AsyncCommand<IndexAddCommand.Settings>
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

			[Description("Include all files in all sub directories. Defaults to true.")]
			[CommandOption("--recursive")]
			[DefaultValue(true)]
			public bool Recursive { get; set; }
		}

		public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
		{
			var searchOptions = new EnumerationOptions
			{
				AttributesToSkip = settings.IncludeHidden
				   ? FileAttributes.Hidden | FileAttributes.System
				   : FileAttributes.System,
				RecurseSubdirectories = settings.Recursive
			};

			await AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.Start("Adding files to the index...", async ctx =>
			{
				// var searchPattern = settings.SearchPattern ?? "*.*";
				// var searchPath = settings.Path ?? Directory.GetCurrentDirectory();
				// var files = new DirectoryInfo(searchPath).GetFiles(searchPattern, searchOptions);



				Manager m = new Manager();
				m.AddFiles(settings);

				// var totalFileSize = files.Sum(fileInfo => fileInfo.Length);
				// AnsiConsole.MarkupLine($"Total file size for [green]{searchPattern}[/] files in [green]{searchPath}[/]: [blue]{totalFileSize:N0}[/] bytes");
			});

			return 0;
		}
	}
}
