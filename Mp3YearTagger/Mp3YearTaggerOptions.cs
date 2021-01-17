using System.Collections.Generic;

namespace KrahmerSoft.Mp3YearTagger
{
	public class Mp3YearTaggerOptions
	{
		/// <summary>
		/// Directory key words that indicate a collection.
		/// Must be all lower-case or match case exactly.
		/// </summary>
		public List<string> CollectionKeywords { get; set; } = new List<string>() {
			"hits",
			"collection",
			"greatest",
			"best",
			"mix",
			"ultimate",
			"essential",
			"singles",
			"anthology",
			"sampler"
		};

		/// <summary>
		/// Flag filename to create after all files in a directory have been checked and/or updated.
		/// Set to null to not create a flag file.
		/// </summary>
		public string FlagFilename { get; set; } = ".yearUpdated";

		/// <summary>
		/// Milliseconds to wait between web API requests. Avoid getting banned.
		/// </summary>
		public int WebApiThrottleMs { get; set; } = 100;

		/// <summary>
		///   <c>true</c> if directory should be processed recursively.
		/// </summary>
		public bool IsRecursive { get; set; } = false;

		/// <summary>
		/// <c>true</c> for dry run test mode.
		/// </summary>
		public bool DryRunMode { get; set; }
	}
}
