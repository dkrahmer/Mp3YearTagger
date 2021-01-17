using System;
using System.Threading.Tasks;

namespace KrahmerSoft.Mp3YearTagger
{
	internal class Program
	{
		private static Mp3YearTaggerOptionsCli _optionsCli;
		private static int Main(string[] args)
		{
			return MainAsync(args).GetAwaiter().GetResult();
		}

		private static async Task<int> MainAsync(string[] args)
		{
			Mp3YearTagger onlineMp3Retagger = null;

			try
			{
				_optionsCli = new Mp3YearTaggerOptionsCli();
				CommandLineParser.ParseArguments(args, _optionsCli);

				if (!_optionsCli.ValidateOptions())
					return 1;

				onlineMp3Retagger = new Mp3YearTagger(_optionsCli);
				onlineMp3Retagger.VerboseOutput += HandleVerboseOutput;

				if (_optionsCli.CollectionsOnly)
				{
					await onlineMp3Retagger.ProcessCollectionsDirectoriesAsync(_optionsCli.Directory);
				}
				else
				{
					await onlineMp3Retagger.ProcessDirectoryAsync(_optionsCli.Directory);
				}
				return 0;
			}
			catch (ApplicationException ex)
			{
				Console.Error.WriteLine(ex.Message);
				return 1;
			}
			catch (ArgumentException ex)
			{
				Console.Error.WriteLine(ex.Message);
				return 1;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.ToString());
				return 1;
			}
			finally
			{
				if (onlineMp3Retagger != null)
					onlineMp3Retagger.VerboseOutput -= HandleVerboseOutput;
			}
		}

		private static void HandleVerboseOutput(object sender, VerboseInfo e)
		{
			if (e.VerboseLevel > _optionsCli.ShowVerboseLevel)
				return;

			if (e.IsError)
			{
				Console.Error.WriteLine($"{e.Message}");
			}
			else
			{
				Console.WriteLine($"{e.Message}");
			}
		}
	}
}
