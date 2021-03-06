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
	[Description("Add files to the index.")]
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

			[Description("Minimum file size in bytes. Defaults to 1MB.")]
			[CommandOption("-a|--min")]
			[DefaultValue(long.MinValue)]
			public long SizeMin { get; set; }

			[Description("Maximum file size in bytes. Defaults to 0, no limit.")]
			[CommandOption("-z|--max")]
			[DefaultValue(long.MaxValue)]
			public long SizeMax { get; set; }

			[Description("Verbose mode.")]
			[CommandOption("-v|--verbose")]
			[DefaultValue(false)]
			public bool Verbose { get; set; }
		}

		public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
		{
			// use current directory if none given
			if (string.IsNullOrEmpty(settings.Path))
				settings.Path = Environment.CurrentDirectory;

			Manager m = new Manager();
			// await m.IndexAdd(settings);

			await AnsiConsole.Status()
				.StartAsync("Indexing...", async ctx =>
				{
					ctx.SpinnerStyle(Style.Parse("green"));
					ctx.Spinner(Spinner.Known.Star);
					await m.IndexAdd(settings);
				});

			// await AnsiConsole.Status()
			// .StartAsync("Adding files to the index...", async ctx =>
			// {
			// 	await m.IndexAdd(settings);
			// });

			// AnsiConsole.MarkupLine("[green]Done! Index has now {} files.[/]");
			m.Dispose();
			return 0;
		}
	}
}