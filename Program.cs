using System;
using System.Diagnostics;
using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace dupesfiles2
{
	class Program
	{
		static void Main(string[] args)
		{
			// create new app
			var app = new CommandApp();

			// configure
			app.Configure(config =>
			{
#if DEBUG
				config.PropagateExceptions();
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
					   .WithDescription("Refreshes the index content.");

				config.AddCommand<IndexScanCommand>("index-scan")
					   .WithAlias("scan")
					   .WithAlias("is")
					   .WithDescription("Scans the index for duplicates.");




			}


			AnsiConsole.Render(new FigletText("todo").LeftAligned().Color(Color.SkyBlue1));
			AnsiConsole.MarkupLine($"[grey]v.{ FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion.ToString() }[/]");
			app.Run(args);

		}
	}
