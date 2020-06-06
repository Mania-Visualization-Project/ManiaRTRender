# ManiaRTRender

[![](https://img.shields.io/github/v/release/Keytoyze/ManiaRTRender?color=blue)](https://github.com/Keytoyze/ManiaRTRender/releases/latest)
[![](https://img.shields.io/github/downloads/Keytoyze/ManiaRTRender/total)](https://github.com/Keytoyze/ManiaRTRender/releases)
[![](https://img.shields.io/github/contributors/Keytoyze/ManiaRTRender?color=green)](https://github.com/Keytoyze/ManiaRTRender/graphs/contributors)
[![](https://img.shields.io/github/license/Keytoyze/ManiaRTRender)](https://github.com/Keytoyze/ManiaRTRender/blob/master/LICENSE)

[中文版](README.md)

## What is it?

ManiaRTRender is a visualization plugin of [OsuSync](https://github.com/Deliay/osuSync), used to mark colorful judgements and hit positions in the music game osu!mania. It is similar to [ManiaReplayMaster](https://github.com/Keytoyze/Mania-Replay-Master). Different from MRM, it's a real-time render tool. Open the tool and begin the game, online rendering will be done, no need to input a replay file anymore.

DEMO: https://www.bilibili.com/video/BV1vE411c73P


### Screenshots

![](Screenshots/screenshot1.png)

![](Screenshots/screenshot2.png)


## Installation

### Auto Installation
1. Install [OsuSync](https://github.com/Deliay/osuSync).

2. Open Sync.exe, enter the following commands.
```
plugins install ManiaRTRender
plugins update
```

### Manual Installation

1. Install [OsuSync](https://github.com/Deliay/osuSync).

2. Install [OsuRTDataProvider](https://github.com/OsuSync/OsuRTDataProvider). Download the latest version as prompted, and copy it to OsuSync. 

3. Download [ManiaRTRender](https://github.com/Keytoyze/ManiaRTRender/releases). After extracting it, copy it to OsuSync as well.

4. Open Sync.exe.

## Function Menu
Right click in the tool window will open a function menu, as follows.

![](Screenshots/menu.png)

- Hide Window in Idle: If checked, The whole renderer window will be hidden when the game is idle, and will show again after game starts.
- Top Most: If checked, the renderer will remain at the top of the screen.
- Close: Close the renderer window. Note: this operation is not reversible. You will need to restart sync.exe to reopen the renderer window.

## Configuration

ManiaRTRender supports modifying falling speed, background picture, rendering FPS and UI sizes. You can modify {OsuSync}/config.ini directly (under `[ManiaRTRender.SettingIni]`). Also, you can use ConfigGUI plugin to modify it by GUI.


|Setting Name|Default Value|Description
|-|-|-
|Speed|25|falling speed (not recommended to be too fast)
|BackgroundPicture||picture path, showing when the game hasn't started yet (empty to show default background)
|BackgroundPictureInPlaying||picture path, showing when the game starts (empty to show black background)
|FPS|0|rendering FPS (0 to enable VSync)
|NoteHeight|40|height of notes
|HitHeight|5|height of hit objects
|NoteStrokeWidth|3|stroke width of notes

## TourneyMode (in experiment)

Please refer to the introduction of Tourney Mode in [OsuRTDataProvider](https://github.com/OsuSync/OsuRTDataProvider). Just to modify relevant tourney settings items in OsuRTDataProvider.

## Compiling
```bash
git clone git@github.com:Keytoyze/ManiaRTRender.git ManiaRTRender
git clone git@github.com:OsuSync/Sync.git Sync
git clone git@github.com:OsuSync/OsuRTDataProvider.git OsuRTDataProvider
cd ManiaRTRender
```

Using Visual Studio to open ManiaRTRender.sln.

## LICENSE

[GNU General Public License v3.0](https://github.com/Keytoyze/ManiaRTRender/blob/master/LICENSE)
