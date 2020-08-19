using IpcLibrary;
using ManiaRTRender.Utils;
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
                Setting.SyncConfig(false);
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

        [String(RequireRestart = true)]
        public ConfigurationElement BackgroundPictureInPlaying
        {
            set => Setting.BackgroundPictureInPlaying = value;
            get => Setting.BackgroundPictureInPlaying;
        }
        
        [Integer(RequireRestart = true)]
        public ConfigurationElement ServerSleepPerCycle
        {
            set => Setting.ServerSleepPerCycle = int.Parse(value);
            get => Setting.ServerSleepPerCycle.ToString();
        }

        [Float(MinValue = 0.0f, MaxValue = 1.0f)]
        public ConfigurationElement RateSmoothFactor
        {
            set => Setting.RateSmoothFactor = float.Parse(value);
            get => Setting.RateSmoothFactor.ToString();
        }

        public void onConfigurationLoad()
        {
            Setting.SyncConfig(true);
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
        public static int ServerSleepPerCycle = 8;
        public static float RateSmoothFactor = 0.8f;
        public static string BackgroundPicture = "";
        public static string BackgroundPictureInPlaying = "";

        public static bool IsVSync => FPS == 0;
        public static int ORTDPListenInterval = 100;

        private static bool SettingLoaded = false;
        private static bool ORTDPLoaded = false;

        public static void SyncConfig(bool isSetting)
        {
            if (isSetting) SettingLoaded = true;
            else ORTDPLoaded = true;

            if (!SettingLoaded || !ORTDPLoaded) return;
            RemoteConfig remoteConfig = new RemoteConfig
            {
                FPS = Setting.FPS,
                HitHeight = Setting.HitHeight,
                NoteHeight = Setting.NoteHeight,
                NoteStrokeWidth = Setting.NoteStrokeWidth,
                ORTDPListenInterval = Setting.ORTDPListenInterval,
                Speed = Setting.Speed,
                BackgroundPicture = Setting.BackgroundPicture,
                BackgroundPictureInPlaying = Setting.BackgroundPictureInPlaying,
                Loaded = true
            };

            byte[] buff = new byte[65536];
            int length = remoteConfig.Write(ref buff, 0);
            SerializeUtils.Save(RemoteConfig.ID, ref buff, length);
        }
    }
}
