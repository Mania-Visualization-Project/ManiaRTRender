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
        public static String BackgroundPicture = "";
        public static String BackgroundPictureInPlaying = "";

        public static bool IsVSync => FPS == 0;
        public static int ORTDPListenInterval = 100;

        private static bool SettingLoaded = false;
        private static bool ORTDPLoaded = false;

        public static void SyncConfig(bool isSetting)
        {
            if (isSetting) SettingLoaded = true;
            else ORTDPLoaded = true;

            if (SettingLoaded && ORTDPLoaded)
            {
                RemoteConfig remoteConfig = new RemoteConfig();
                remoteConfig.FPS = Setting.FPS;
                remoteConfig.HitHeight = Setting.HitHeight;
                remoteConfig.NoteHeight = Setting.NoteHeight;
                remoteConfig.NoteStrokeWidth = Setting.NoteStrokeWidth;
                remoteConfig.ORTDPListenInterval = Setting.ORTDPListenInterval;
                remoteConfig.Speed = Setting.Speed;
                remoteConfig.BackgroundPicture = Setting.BackgroundPicture;
                remoteConfig.BackgroundPictureInPlaying = Setting.BackgroundPictureInPlaying;
                remoteConfig.Loaded = true;

                byte[] buff = new byte[65536];
                int length = remoteConfig.Write(ref buff, 0);
                SerializeUtils.Save(RemoteConfig.id, ref buff, length);
            }
        }
    }
}
