# La La Landscape (ds-musiclands)

A [nepTune-ish](http://www.cp.jku.at/projects/neptune/) music player, except bigger. Started working on it as a part of my diploma. Probably the worst way to do git with unity.

At the moment, tons of features aren't implemented yet (even when this readme refers to them as already present). Song positions are pre-calculated with t-sne. Songs are then thrown at a map that's 1024x1024 discrete positions big. The 1024x1024 map is stored in a binary tree, so we actually get 10 maps — levels — from 1x1 to 1024x1024. We count how many songs stick in a particular spot on the map, on all levels. We combine some of the levels together (higher levels for rough shapes, low levels for details), which gets us a nice, terrain-shaped terrain. Terrain is split into chunks, each chunk has some songs that represent it.

Songs start playing when you enter a chunk. Since they're not pre-downloaded — instead they're being streamed from the internet as you explore — we only play a single song at a time, which is unlike neptune — we do so to save the bandwidth, API calls and the good will of Google). 

## Requirements to start rolling

* Unity3D
* Unity3D Standard Assets (from unity store — not provided here)
* python2 (needs to be in path)
* following python libraries: gpmusicapi, requests, rpyc, (todo—see 7digital dependencies) — use `pip install` to install them.
* ffmpeg (needs to be in path)
* Linux

Currently, there's been no effort at all to make this multi-platform. Windows support could be prolly hacked real fast.

If you want to actually listen to music, you'll also need either of the following:

* 7digital API key
* Google Play Music (GPM) subscription

7digital's free API is actually rather restrictive, lacks songs (can't pull from countries like UK, DE, US. A great deal of songs aren't available outside of the bigger countries that we can't pretend we're in), and this program probably breaks 7digital's terms of service. **If you have GPM subscription, just use that instead** (or - better yet - alongside 7digital's offerings).

Entering 7digital details: go to `musiclands-code/Assets/Resources/conf`. In there, make a file called `7digital` (no file extension). First line should have your 7digital key, second should have your 7digital secret.

**GPM not yet implemented. Soon-ish.**

* Have good amount of disk space ready, there's no cache management at all

## TODO:


Give out credits, attributions, and a total make-over to this readme.
