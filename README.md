# La La Landscape (ds-musiclands)

A [nepTune-ish](http://www.cp.jku.at/projects/neptune/) music player, except bigger. Started working on it as a part of my diploma. Probably the worst way to do git with unity.

At the moment, tons of features aren't implemented yet (even when this readme refers to them as already present). Song positions are pre-calculated with t-sne. Songs are then thrown at a map that's 1024x1024 discrete positions big. The 1024x1024 map is stored in a binary tree, so we actually get 10 maps — levels — from 1x1 to 1024x1024. We count how many songs stick in a particular spot on the map, on all levels. We combine some of the levels together (higher levels for rough shapes, low levels for details), which gets us a nice, terrain-shaped terrain. Terrain is split into chunks, each chunk has some songs that represent it.

Songs start playing when you enter a chunk. Since they're not pre-downloaded — instead they're being streamed from the internet as you explore — we only play a single song at a time, which is unlike neptune — we do so to save the bandwidth, API calls and the good will of Google). 

## Requirements to start rolling

* Unity3D
* Unity3D Standard Assets (from unity store — not provided here)
* python2 (needs to be in /usr/bin/python2)
* following python libraries: gpmusicapi, requests, rpyc, (todo—see 7digital dependencies) — use `pip install` to install them.
* ffmpeg (needs to be in /usr/bin/ffmpeg)

If you want to actually listen to music, you'll also need either of the following:

* Google Play Music (GPM) subscription

* Have good amount of disk space ready, there's no cache management at all yet

## Setting up Google Play Music:

If you have paid subscription to Google Play Music:

0. Make sure you have an android (or iOS) phone or tablet connected to your paid GPM account.

1. Open python console.
2. Do this:

```
import gmusicapi
api = gmusicapi.Mobileclient()
api.login('user@gmail.com', 'my-password', Mobileclient.FROM_MAC_ADDRESS)
api.get_registered_devices()
```
From the output of the last command, select a device with type `ANDROID` or `IOS` and take its ID (remove the 0x part though). 


3. Run `gpm-server.py` in `musiclands-code/Assets/Resources/scripts/python2/`
4. Run `gpm-client.py login <email> <password> <id you got in step 2>` in `musiclands-code/Assets/Resources/scripts/python2/`


## Re-enabling 7digital

Code to deal is still in the project. You will need to re-enable it, probably disable Google Play Music and rewrite some code that assumes GPM. 

### Credentials for 7digital:

In this folder:

`musiclands-code/Assets/Resources/conf/`

Make a file named `7digital`. First line in that file is your API key, second line is secret (I think — refer to the implementation in code for more details)


## TODO:


Give out credits, attributions, and a total make-over to this readme.
