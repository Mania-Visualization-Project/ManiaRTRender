﻿using ManiaRTRender.Core;
using OsuRTDataProvider.Mods;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using static ManiaRTRender.ManiaRTRenderPlugin;

namespace ManiaRTRender.Utils
{
    public static class OsuUtils
    {
        private static readonly double[] BASE_JUDGEMENT_OFFSET = { 16.5, 64, 97, 127, 151, 188 };
        private static readonly double[] BASE_JUDGEMENT_OFFSET_HR = { 11.5, 45, 69, 90, 107, 133 };
        private static readonly double[] BASE_JUDGEMENT_OFFSET_EZ = { 22.5, 89, 135, 177, 211, 263 };
        private static readonly double DECREMENT_NONE = 3.0;
        private static readonly double DECREMENT_HR = 2.1;
        private static readonly double DECREMENT_EZ = 4.2;

        public static readonly Color[] JUDGEMENT_COLORS = {
            Color.FromArgb(255, 255, 255),
            Color.FromArgb(255, 210, 55),
            Color.FromArgb(121, 208, 32),
            Color.FromArgb(30, 104, 197),
            Color.FromArgb(225, 52, 155),
            Color.FromArgb(255, 0, 0)
        };
        public static readonly Color COLOR_LIGHT = Color.FromArgb(100, 100, 100);

        private static double[] GetJudgementWindow(double od, ModsInfo modsInfo)
        {
            var result = new double[6];
            double decrement;
            if (modsInfo.HasMod(ModsInfo.Mods.HardRock))
            {
                BASE_JUDGEMENT_OFFSET_HR.CopyTo(result, 0);
                decrement = DECREMENT_HR;
            }
            else if (modsInfo.HasMod(ModsInfo.Mods.Easy))
            {
                BASE_JUDGEMENT_OFFSET_EZ.CopyTo(result, 0);
                decrement = DECREMENT_EZ;
            }
            else
            {
                BASE_JUDGEMENT_OFFSET.CopyTo(result, 0);
                decrement = DECREMENT_NONE;
            }

            var speedRatio = GetSpeedRatio(modsInfo);
            for (var i = 1; i < 6; i++)
            {
                result[i] -= decrement * od;
                result[i] *= speedRatio;
            }
            Logger.I($"Judgement window: [{string.Join(", ", result)}]");
            return result;
        }

        // a naive parser
        public static ManiaBeatmap ReadBeatmap(string beatmapFile, ModsInfo modsInfo)
        {
            var isHitObject = false;
            var beatmap = new ManiaBeatmap();
            var od = double.NaN;
            var notes = new List<Note>();

            Exception expInParsing = null;

            using (var fs = File.OpenRead(beatmapFile))
            using (var reader = new StreamReader(fs))
            {
                var line = reader.ReadLine()?.Trim();
                if (line != null)
                {
                    var elements = line.Split(',');
                    while (!reader.EndOfStream)
                    {
                        if (line.StartsWith("CircleSize"))
                        {
                            try
                            {
                                beatmap.Key = int.Parse(line.Split(':').Last());
                                Logger.I($"Find key: {beatmap.Key}");
                            }
                            catch (Exception e)
                            {
                                Logger.E(e.StackTrace);
                            }
                        }
                        else if (line.StartsWith("OverallDifficulty"))
                        {
                            try
                            {
                                od = double.Parse(line.Split(':').Last());
                                Logger.I($"Find od: {od}");
                            } 
                            catch (Exception e)
                            {
                                Logger.E(e.StackTrace);
                            }
                        }
                        else if (line.StartsWith("["))
                        {
                            isHitObject = line == "[HitObjects]";
                            continue;
                        }
                        else if (line.StartsWith("Mode"))
                        {
                            beatmap.IsMania = 3 == int.Parse(line.Split(':').Last());
                        }

                        if (!isHitObject || line == string.Empty) continue;
                        {
                            try
                            {
                                var note = new Note
                                {
                                    Column = (int) Math.Floor(int.Parse(elements[0]) * beatmap.Key / 512.0),
                                    TimeStamp = long.Parse(elements[2])
                                };
                                var endTime = long.Parse(elements[5].Split(':')[0]);
                                note.Duration = (int.Parse(elements[3]) & 128) != 0 ? endTime - note.TimeStamp : 0L;
                                notes.Add(note);
                            }
                            catch (Exception e)
                            {
                                expInParsing = e;
                            }
                        }

                    }
                }
            }

            if (expInParsing != null)
            {
                Logger.W(expInParsing.StackTrace);
            }

            if (!beatmap.IsMania)
            {
                return null;
            }

            if (beatmap.Key <= 0)
            {
                Logger.E("Cannot parse key.");
                return null;
            }

            if (double.IsNaN(od))
            {
                Logger.W("Cannot parse od. Use od = 0.");
                od = 0;
            }

            beatmap.JudgementWindow = GetJudgementWindow(od, modsInfo);
            beatmap.Notes = notes;

            if (modsInfo.HasMod(ModsInfo.Mods.Mirror))
            {
                foreach (var n in beatmap.Notes) n.Column = beatmap.Key - n.Column - 1;
            }
            if (modsInfo.HasMod(ModsInfo.Mods.Random))
            {
                Logger.W("Currently Random mod is not supported. It will not work correctly.");
            }
            return beatmap;
        }

        public static double GetSpeedRatio(ModsInfo modsInfo)
        {
            if (modsInfo.HasMod(ModsInfo.Mods.DoubleTime))
            {
                return 1.5;
            }

            return modsInfo.HasMod(ModsInfo.Mods.HalfTime) ? 0.75 : 1.0;
        }
    }
}
