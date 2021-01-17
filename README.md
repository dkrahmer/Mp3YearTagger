# Mp3YearTagger

## About
Mass update MP3 ID3 tags with the oldest year for each given song.
This is useful for compilations of songs that were individually released previously in various different years.
Setting the year to the original release year makes it easier to organize and find music by year or decade.
The release year is looked up from the MusicBrainz API using the song title and artist stored in the existing ID3 tags.

## Cloning
This repo includes a git submodule so be sure to clone with submodules by running:

 `git clone --recurse-submodules https://github.com/dkrahmer/Mp3YearTagger.git`

Or clone the submodule after cloning the main repo by running:

`git pull --recurse-submodules`
