using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using dupefiles2.Commands;

namespace dupefiles2
{

	class Program
	{
		private const string cAppName = "DF2";

		public static async Task<int> Main(string[] args)
		{
			// create new app
			var app = new CommandApp();

			// configure
			app.Configure(config =>
			{
				config.SetApplicationName(cAppName);
#if DEBUG
				// config.PropagateExceptions();
				config.ValidateExamples();
#endif
				// add commands
				config.AddCommand<IndexAddCommand>("index-add")
					   .WithAlias("add")
					   .WithAlias("ia")
					   .WithDescription("Adds files/directories to the index.");

				config.AddCommand<IndexUpdateCommand>("index-update")
					   .WithAlias("update")
					   .WithAlias("iu")
					   .WithDescription("Refreshes the index.");

				config.AddCommand<IndexScanCommand>("index-scan")
					   .WithAlias("scan")
					   .WithAlias("is")
					   .WithDescription("Scans the index for duplicates.");
			});


			AnsiConsole.Render(new FigletText(cAppName).LeftAligned().Color(Color.SkyBlue1));
			AnsiConsole.MarkupLine($"[grey]v.{ FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion.ToString() }[/]");

			return await app.RunAsync(args);

		}
	}

}