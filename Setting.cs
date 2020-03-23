using Sync.Tools;
using Sync.Tools.ConfigurationAttribute;
using System;

namespace ManiaRTRender
{
    class SettingIni : IConfigurable
    {
        [Integer(MinValue = 1, MaxValue = 40)]
        public ConfigurationElement Speed
        {
            set => Setting.Speed = int.Parse(value);
            get => Setting.Speed.ToString();
        }

        [Integer]
        public ConfigurationElement NoteHeight
        {
            set => Setting.NoteHeight = int.Parse(value);
            get => Setting.NoteHeight.ToString();
        }

        [Integer]
        public ConfigurationElement HitHeight
        {
            set => Setting.HitHeight = int.Parse(value);
            get => Setting.HitHeight.ToString();
        }

        [Integer]
        public ConfigurationElement NoteStrokeWidth
        {
            set => Setting.NoteStrokeWidth = int.Parse(value);
            get => Setting.NoteStrokeWidth.ToString();
        }

        [String]
        public ConfigurationElement BackgroundPicture
        {
            set => Setting.BackgroundPicture = value;
            get => Setting.BackgroundPicture;
        }

        public void onConfigurationLoad()
        {
        }

        public void onConfigurationReload()
        {
        }

        public void onConfigurationSave()
        {
        }
    }

    class Setting
    {
        public static int Speed = 25;
        public static int NoteHeight = 40;
        public static int HitHeight = 5;
        public static int NoteStrokeWidth = 3;
        public static String BackgroundPicture = "";
    }
}
