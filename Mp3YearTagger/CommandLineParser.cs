using System;
using System.Collections.Generic;
using System.Linq;

namespace KrahmerSoft.Mp3YearTagger
{
	internal class CommandLineParser
	{
		public static int ParseArguments(string[] args, Mp3YearTaggerOptionsCli optionsCli)
		{
			return ParseArgumentsInternal(args, optionsCli);
		}

		private static int ParseArgumentsInternal(string[] args, Mp3YearTaggerOptionsCli optionsCli)
		{
			if (args == null)
				return 0;

			var argsNotUsedCount = 0;
			var argIndex = 0;
			var requiredArgumentCandidates = new List<string>();

			try
			{
				while (argIndex < args.Length)
				{
					string arg = args[argIndex];
					switch (arg)
					{
						case "--collections-only":
						case "-c":
							optionsCli.CollectionsOnly = true;
							break;

						case "--dry-run":
						case "-d":
							optionsCli.DryRunMode = true;
							break;

						case "--flag-filename":
						case "-f":
							optionsCli.FlagFilename = GetNextArg(args, ref argIndex);
							break;

						case "--collection-keywords":
						case "-k":
							optionsCli.CollectionKeywords = GetNextArgAsStringEnumerable(args, ref argIndex).ToList();
							break;

						case "--web-api-throttle-ms":
						case "-t":
							optionsCli.WebApiThrottleMs = GetNextArgAsInt(args, ref argIndex);
							break;

						case "--recursive":
						case "-r":
							optionsCli.IsRecursive = true;
							break;

						case "--verbose":
						case "-v":
							optionsCli.ShowVerboseLevel++;
							break;

						case "--quiet":
						case "-q":
							optionsCli.ShowVerboseLevel = -1;
							break;

						case "--help":
						case "-h":
						case "/h":
						case "/?":
							ShowHelp();
							Environment.Exit(0);
							break;

						default:
							if (TryExpandMergedArgs(argIndex, ref args, optionsCli))
							{
								continue;
							}
							else
							{
								requiredArgumentCandidates.Add(arg);
								argsNotUsedCount++;
							}
							break;
					}

					argIndex++;
				}
			}
			catch (ArgumentException ex)
			{
				Console.Error.WriteLine(ex.Message);
				Environment.Exit(1);
			}

			if (requiredArgumentCandidates.Count != 1)
			{
				Console.Error.WriteLine($"Invalid arguments: {string.Join(", ", requiredArgumentCandidates)}");
				Environment.Exit(1);
			}

			optionsCli.Directory = requiredArgumentCandidates[0];
			argsNotUsedCount--;

			return argsNotUsedCount;
		}

		private static void ShowHelp()
		{
			var defaultValues = new Mp3YearTaggerOptionsCli();

			Console.WriteLine($"Usage: Mp3YearTagger [OPTIONS]... [DIRECTORY]");
			Console.WriteLine();
			Console.WriteLine($"Required arguments:");
			Console.WriteLine($"  [DIRECTORY]                    directory path with MP3 files to process");
			Console.WriteLine();
			Console.WriteLine($"Optional arguments:");
			Console.WriteLine($"  -c, --collections-only         process collection directories recursively (see -k, --collection-keywords)");
			Console.WriteLine($"  -d, --dry-run                  dry run - do not make any file changes - will output changes that would have been made");
			Console.WriteLine($"  -f, --flag-filename            override the filename to create in each directory after processing (use \"\" to disable)");
			Console.WriteLine($"                                   (default: {defaultValues.FlagFilename})");
			Console.WriteLine($"  -k, --collection-keywords      comma-separated list of directory key words that indicate a collection (override)");
			Console.WriteLine($"                                   (default: {string.Join(", ", defaultValues.CollectionKeywords)})");
			Console.WriteLine($"  -q, --quiet                    quite, no output (except for errors)");
			Console.WriteLine($"  -r, --recursive                process directory recursively");
			Console.WriteLine($"  -t, --web-api-throttle-ms      milliseconds to wait between MusicBrainz API requests to avoid getting banned (default: {defaultValues.WebApiThrottleMs})");
			Console.WriteLine($"  -v, --verbose                  increase verbose output (Example: -vvv for verbosity level 3)");
		}

		private static string GetNextArg(string[] args, ref int currentArgIndex)
		{
			int nextArgIndex = currentArgIndex + 1;
			if (nextArgIndex >= args.Length)
				return null;

			currentArgIndex = nextArgIndex;
			return args[currentArgIndex];
		}

		private static IEnumerable<string> GetNextArgAsStringEnumerable(string[] args, ref int currentArgIndex, string separator = ",")
		{
			string strVal = GetNextArg(args, ref currentArgIndex);
			return strVal
				.Split(separator)
				.Select(i => i.Trim());
		}

		private static int GetNextArgAsInt(string[] args, ref int currentArgIndex)
		{
			string strVal = GetNextArg(args, ref currentArgIndex);
			if (!int.TryParse(strVal, out int intVal))
				throw new ArgumentException($"Invalid data argument data. Expected an integer value: '{args[currentArgIndex - 1]} {strVal}'");

			return intVal;
		}

		private static bool GetNextArgAsBool(string[] args, ref int currentArgIndex)
		{
			string strVal = GetNextArg(args, ref currentArgIndex);
			if (!bool.TryParse(strVal, out bool boolVal))
				throw new ArgumentException($"Invalid data argument data. Expected a boolean value (1/0, true/false): '{args[currentArgIndex - 1]} {strVal}'");

			return boolVal;
		}

		private static bool TryExpandMergedArgs(int argIndex, ref string[] args, Mp3YearTaggerOptionsCli optionsCli)
		{
			string potentiallyMergedArgs = args[argIndex];
			if (potentiallyMergedArgs == null || potentiallyMergedArgs.Length <= 2 || potentiallyMergedArgs[0] != '-' || potentiallyMergedArgs[1] == '-')
				return false;

			var newArgs = potentiallyMergedArgs.Substring(1).Select(ch => $"-{ch}");
			var argsList = new List<string>(args);

			// Replace the merged args with expanded args
			argsList.RemoveAt(argIndex);
			argsList.InsertRange(argIndex, newArgs);
			args = argsList.ToArray();

			return true;
		}
	}
}
