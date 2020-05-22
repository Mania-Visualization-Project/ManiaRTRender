using IpcLibrary;
using System;

namespace RenderClient
{
    class Setting
    {
        public static int Speed = 0;
        public static int FPS = 60;
        public static int NoteHeight = 40;
        public static int HitHeight = 5;
        public static int NoteStrokeWidth = 3;
        public static string BackgroundPicture = "";
        public static string BackgroundPictureInPlaying = "";

        public static bool IsVSync => FPS == 0;
        public static int ORTDPListenInterval = 100;
        private static bool _hasInit = false;
        private static int _remoteId = -1;
        private static byte[] _buff = new byte[65536];

        public static bool SyncIfNeed()
        {
            if (_hasInit) return false;
            if (_remoteId < 0) _remoteId = SerializeUtils.InitShareMemory(IpcConstants.OBJECT_CONFIG_NAME, IpcConstants.SIZE_CONFIG);

            //RemoteConfig remoteConfig = (RemoteConfig)Activator.GetObject(typeof(RemoteConfig), IpcConstants.URL_CONFIG);
            //RemoteConfig remoteConfig = (RemoteConfig)SerializeUtils.Fetch(RemoteId);

            SerializeUtils.Fetch(_remoteId, ref _buff);
            RemoteConfig remoteConfig = new RemoteConfig();
            remoteConfig.Read(ref _buff, 0);

            if (!remoteConfig.Loaded) return false;
            Speed = remoteConfig.Speed;
            FPS = remoteConfig.FPS;
            NoteHeight = remoteConfig.NoteHeight;
            HitHeight = remoteConfig.HitHeight;
            NoteStrokeWidth = remoteConfig.NoteStrokeWidth;
            BackgroundPicture = remoteConfig.BackgroundPicture;
            BackgroundPictureInPlaying = remoteConfig.BackgroundPictureInPlaying;
            _hasInit = true;
            return true;

        }
    }
}
