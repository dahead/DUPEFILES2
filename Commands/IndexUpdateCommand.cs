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
			[Description("Verbose mode.")]
			[CommandOption("-v|--verbose")]
			[DefaultValue(false)]
			public bool Verbose { get; set; }
		}

		public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
		{
			Manager m = new Manager();
			await m.UpdateIndex(settings);

			// await AnsiConsole.Status()
			// .StartAsync("Updating the index...", async ctx =>
			// {
			// 	await m.UpdateIndex(settings);
			// });

			AnsiConsole.WriteLine("Updated in the index.");
			m.Dispose();
			return 0;
		}
	}
}
