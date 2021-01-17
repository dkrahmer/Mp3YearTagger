using System;

namespace KrahmerSoft.Mp3YearTagger
{
	public class Mp3YearTaggerOptionsCli : Mp3YearTaggerOptions
	{
		public string Directory { get; set; }
		public bool CollectionsOnly { get; set; }
		public int ShowVerboseLevel { get; set; }

		public bool ValidateOptions()
		{
			bool valid = true;

			if (string.IsNullOrWhiteSpace(Directory))
			{
				Console.Error.WriteLine("Directory is required.");
				valid = false;
			}

			if (!valid)
			{
				Console.Error.WriteLine();
				Console.Error.WriteLine("  --help for usage details.");
			}

			return valid;
		}
	}
}
