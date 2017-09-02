# Extras/contents:

* bhtsne (folder) — cloned from [todo: add link], modified to read files containing song features
* addIds (bash script) — bhtsne reads a file with lines formatted `[song id]\t[artist] - [title]` (where each [song id] represents a `[song id].mp3.mrscore` file) and outputs coordinates, but not ids. This file puts song ids before their respective coordinates.
* generateFixedIdList.sh (bash script) — we got about 4 million songs (we managed to extract 3M). We extracted that into a folder. This was a dumb idea, because it absolutely wrecked the filesystem. Mounting the disk with extracted data takes a long time. Depending on a system and how it feels, the disk might not even get detected at times or at all. If it gets detected and mounted, just hovering over the folder with 3M files hangs the file browser for a long while. This script was made in an attempt to split the 3M files into subfolders, hoping this would make the issue go away. I think it lessened it a bit.
* removeMissingFromReadme (bash script) — We failed to extract all 4M songs we got, but metadata.txt still contained those missing ids. This script creates a new file with missing files removed.
