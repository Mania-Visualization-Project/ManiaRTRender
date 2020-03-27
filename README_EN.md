# ManiaRTRender

[中文版](README.md)

## What is it?

ManiaRTRender is a visualization plugin of [OsuSync](https://github.com/Deliay/osuSync), used to mark colorful judgements and hit positions in the music game osu!mania. It is similar to [ManiaReplayMaster](https://github.com/Keytoyze/Mania-Replay-Master). Different from MRM, it's a real-time render tool. Open the tool and begin the game, online rendering will be done, no need to input a replay file anymore.

DEMO: https://www.bilibili.com/video/BV1vE411c73P


### Screenshots

![](Screenshots/screenshot1.png)

![](Screenshots/screenshot2.png)


## Installation

1. Install [OsuSync](https://github.com/Deliay/osuSync)。

2. Install [OsuRTDataProvider](https://github.com/OsuSync/OsuRTDataProvider). Download the latest version (`1.6.1` at least) as prompted, and copy it to OsuSync. 

3. Download [ManiaRTRender](https://github.com/Keytoyze/ManiaRTRender/releases). After extracting it, copy it to OsuSync as well.

4. Open Sync.exe。

## Configuration

ManiaRTRender supports modifying falling speed, background picture, rendering FPS and UI sizes. You can modify {OsuSync}/config.ini directly (under `[ManiaRTRender.SettingIni]`). Also, you can use ConfigGUI plugin to modify it by GUI.


|Setting Name|Default Value|Description
|-|-|-
|Speed|25|falling speed (not recommended to be too fast)
|BackgroundPicture||picture path, showing when the game hasn't started yet (empty to show default background)
|FPS|0|rendering FPS (0 to enable VSync)
|NoteHeight|40|height of notes
|HitHeight|5|height of hit objects
|NoteStrokeWidth|3|stroke width of notes

## TourneyMode (in experiment)

Please refer to the introduction of Tourney Mode in [OsuRTDataProvider](https://github.com/OsuSync/OsuRTDataProvider). Just to modify relevant tourney settings items in OsuRTDataProvider.

## LICENSE

[GNU General Public License v3.0](https://github.com/Keytoyze/ManiaRTRender/blob/master/LICENSE)
