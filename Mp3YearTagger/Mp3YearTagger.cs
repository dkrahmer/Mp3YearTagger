using Id3Lib;
using MetaBrainz.MusicBrainz;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KrahmerSoft.Mp3YearTagger
{
	public class Mp3YearTagger
	{
		private static readonly DateTime OLDEST_ALLOWED_DATE = new DateTime(1877, 01, 01); // Any recording date before 1877 is impossible since the phonograph was invented in that year.
		private readonly Mp3YearTaggerOptions _options;

		public Action<object, VerboseInfo> VerboseOutput { get; set; }

		public Mp3YearTagger(Mp3YearTaggerOptions options)
		{
			_options = options;
		}

		public async Task<int> ProcessCollectionsDirectoriesAsync(string baseDirectory)
		{
			int fileCount = 0;
			if (!Directory.Exists(baseDirectory))
				return fileCount;

			bool shouldProcess = false;
			string directoryNodeName = Path.GetFileName(baseDirectory).ToLower();
			foreach (string collectionDirectoryKeyWord in _options.CollectionKeywords)
			{
				if (directoryNodeName.Contains(collectionDirectoryKeyWord))
				{
					shouldProcess = true;
					break;
				}
			}

			if (shouldProcess)
				fileCount += await ProcessDirectoryAsync(baseDirectory);

			foreach (string directory in Directory.EnumerateDirectories(baseDirectory))
			{
				fileCount += await ProcessCollectionsDirectoriesAsync(directory);
			}

			return fileCount;
		}

		public async Task<int> ProcessDirectoryAsync(string directory)
		{
			VerboseOutput?.Invoke(this, new VerboseInfo(1, () => $"Processing directory: \"{directory}\""));
			string flagFilePath = null;

			if (!string.IsNullOrWhiteSpace(_options.FlagFilename))
				flagFilePath = Path.Combine(directory, _options.FlagFilename);

			int fileCount = 0;
			if (flagFilePath != null && File.Exists(flagFilePath))
			{
				VerboseOutput?.Invoke(this, new VerboseInfo(1, () => $"    Already updated."));
			}
			else
			{
				foreach (string filePath in Directory.EnumerateFiles(directory, "*.mp3"))
				{
					await RetagFileAsync(filePath);
					fileCount++;

					if (!_options.DryRunMode)
						await Task.Delay(_options.WebApiThrottleMs);
				}

				if (flagFilePath != null && fileCount > 0)
				{
					if (_options.DryRunMode)
					{
						VerboseOutput?.Invoke(this, new VerboseInfo(0, () => $"    DRY RUN MODE: Would have created flag file: '{flagFilePath}'"));
					}
					else
					{
						using (File.OpenWrite(flagFilePath))
						{
						}
					}
				}
			}

			if (_options.IsRecursive)
			{
				foreach (string subdirectory in Directory.EnumerateDirectories(directory))
				{
					fileCount += await ProcessDirectoryAsync(subdirectory);
				}
			}

			return fileCount;
		}

		public async Task RetagFileAsync(string mp3FilePath)
		{
			VerboseOutput?.Invoke(this, new VerboseInfo(0, () => $"File: \"{mp3FilePath}\""));

			using (var stream = new FileStream(mp3FilePath, FileMode.Open, _options.DryRunMode ? FileAccess.Read : FileAccess.ReadWrite))
			{
				TagHandler tag1 = null;
				TagHandler tag2 = null;
				ID3v1 tagModel1 = null;

				try
				{
					// Read ID3v1 tags...
					stream.Seek(0, SeekOrigin.Begin);
					tagModel1 = new ID3v1();
					tagModel1.Deserialize(stream);

					tag1 = new TagHandler(tagModel1.FrameModel);
				}
				catch (Exception ex)
				{
					VerboseOutput?.Invoke(this, new VerboseInfo(3, () => $"    Could not read ID3v1 tags: {ex.ToString()}", isError: true));
				}

				try
				{
					// Read ID3v2 tags...
					stream.Seek(0, SeekOrigin.Begin);
					var tagModel2 = TagManager.Deserialize(stream);

					tag2 = new TagHandler(tagModel2);
				}
				catch (Exception ex)
				{
					VerboseOutput?.Invoke(this, new VerboseInfo(3, () => $"    Could not read ID3v2 tags: {ex.ToString()}", isError: true));
				}

				if (tag1 == null && tag2 == null)
				{
					VerboseOutput?.Invoke(this, new VerboseInfo(0, () => $"    No ID3 tags found. No changes.", isError: true));
					return;
				}

				if (string.IsNullOrWhiteSpace(tag1?.Year) && tag2 != null)
					tag1 = null; // prefer storing the year in v2 by checking v1 first

				if (string.IsNullOrWhiteSpace(tag2?.Year) && tag1 != null)
					tag2 = null; // The year is stored in the v1 tag only

				string artist = (tag2?.Artist ?? tag1?.Artist).Trim(new char[] { ' ', '\n', '\0' }).Trim();
				string title = (tag2?.Title ?? tag1?.Title).Trim(new char[] { ' ', '\n', '\0' }).Trim();
				string year = (tag2?.Year ?? tag1?.Year).Trim(new char[] { ' ', '\n', '\0' }).Trim();

				if (tag1 != null && tag2 != null && tag1.Year != tag2.Year)
					year = $"{tag1.Year} & {tag2.Year}";

				if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(title))
				{
					VerboseOutput?.Invoke(this, new VerboseInfo(0, () => $"    Title and/or artist tags are missing or contain empty values. No changes.", isError: true));
					return;
				}

				VerboseOutput?.Invoke(this, new VerboseInfo(1, () => $"    Found MP3 Tags: \"{artist}\" \"{title}\" ({year})"));
				var oldestReleaseDate = await GetOldestReleaseDateAsync(artist, title);

				if (oldestReleaseDate == null)
				{
					VerboseOutput?.Invoke(this, new VerboseInfo(0, () => $"    Oldest release date could not be determined. Online lookup failed. No changes.", isError: true));
					return;
				}

				if (year == oldestReleaseDate.Value.Year.ToString())
				{
					VerboseOutput?.Invoke(this, new VerboseInfo(0, () => $"    Year is already correct. No changes."));
					return;
				}

				if (_options.DryRunMode)
				{
					VerboseOutput?.Invoke(this, new VerboseInfo(0, () => $"    DRY RUN MODE: Year WOULD HAVE BEEN updated to ({oldestReleaseDate.Value.Year})."));
				}
				else
				{
					// Update the year with the new looked up year
					if (tag1 != null)
					{
						tag1.Year = oldestReleaseDate.Value.Year.ToString();

						tagModel1.FrameModel = tag1.FrameModel;
						tagModel1.Serialize(stream);
					}

					if (tag2 != null)
					{
						tag2.Year = oldestReleaseDate.Value.Year.ToString();

						TagManager.Serialize(tag2.FrameModel, stream);
					}

					VerboseOutput?.Invoke(this, new VerboseInfo(0, () => $"    Year updated to ({oldestReleaseDate.Value.Year})."));
				}
			}
		}

		public async Task<DateTime?> GetOldestReleaseDateAsync(string artist, string title)
		{
			// Remove any '\0' chars. Some tags end with '\0' char or contain double-quotes that may break the search.
			artist.Replace("\0", "").Replace("\"", "");
			title.Replace("\0", "").Replace("\"", "");

			VerboseOutput?.Invoke(this, new VerboseInfo(2, () => $"    Looking up on MusicBrainz..."));
			using (var musicBrainzQuery = new Query("OnlineMp3Retagger", "1.0.0.0", "https://github.com/dkrahmer"))
			{
				string searchQuery = $"artist:\"{artist}\" AND recording:\"{title}\""; // MusicBrainz query language
				var recordings = await musicBrainzQuery.FindRecordingsAsync(searchQuery);

				DateTime? oldestReleaseDate = null;

				VerboseOutput?.Invoke(this, new VerboseInfo(3, () => $"    Found {recordings.Results.Count} recording results."));
				foreach (var recording in recordings.Results)
				{
					if (recording.Score < 70)
						continue;

					if (recording.Item.Releases == null)
						continue;

					foreach (var release in recording.Item.Releases)
					{
						if (release.Date == null)
							continue;

						var thisReleaseDate = release.Date.NearestDate;
						if (thisReleaseDate < OLDEST_ALLOWED_DATE)
							continue;

						if (oldestReleaseDate == null || thisReleaseDate < oldestReleaseDate)
							oldestReleaseDate = thisReleaseDate;
					}
				}

				return oldestReleaseDate;
			}
		}
	}
}
