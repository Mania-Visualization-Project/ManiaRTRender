using Sync.Tools;
using Sync.Tools.ConfigurationAttribute;
using System;

namespace ManiaRTRender
{

    namespace ORTDPSetting
    {
        class SettingIni : IConfigurable
        {
            [Integer(MinValue = 1, MaxValue = 10000)]
            public ConfigurationElement ListenInterval
            {
                set 
                {
                    ManiaRTRenderPlugin.Logger.I($"ORTDP's ListenInterval: {value}");
                    Setting.ORTDPListenInterval = int.Parse(value);
                }
                get => Setting.ORTDPListenInterval.ToString();
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
    }

    class SettingIni : IConfigurable
    {
        [Integer(MinValue = 1, MaxValue = 40, RequireRestart = true)]
        public ConfigurationElement Speed
        {
            set => Setting.Speed = int.Parse(value);
            get => Setting.Speed.ToString();
        }

        [Integer(MinValue = 0, MaxValue = 480, RequireRestart = true)]
        public ConfigurationElement FPS
        {
            set => Setting.FPS = int.Parse(value);
            get => Setting.FPS.ToString();
        }

        [Integer(RequireRestart = true)]
        public ConfigurationElement NoteHeight
        {
            set => Setting.NoteHeight = int.Parse(value);
            get => Setting.NoteHeight.ToString();
        }

        [Integer(RequireRestart = true)]
        public ConfigurationElement HitHeight
        {
            set => Setting.HitHeight = int.Parse(value);
            get => Setting.HitHeight.ToString();
        }

        [Integer(RequireRestart = true)]
        public ConfigurationElement NoteStrokeWidth
        {
            set => Setting.NoteStrokeWidth = int.Parse(value);
            get => Setting.NoteStrokeWidth.ToString();
        }

        [String(RequireRestart = true)]
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
        public static int FPS = 0;
        public static int NoteHeight = 40;
        public static int HitHeight = 5;
        public static int NoteStrokeWidth = 3;
        public static String BackgroundPicture = "";

        public static bool IsVSync => FPS == 0;

        public static int ORTDPListenInterval = 100;
    }
}
