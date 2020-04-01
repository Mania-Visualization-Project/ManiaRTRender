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
        private static bool HasInit = false;
        private static int RemoteId = -1;
        private static byte[] buff = new byte[65536];

        public static bool SyncIfNeed()
        {
            if (HasInit) return false;
            if (RemoteId < 0) RemoteId = SerializeUtils.InitShareMemory(IpcConstants.OBJECT_CONFIG_NAME, IpcConstants.SIZE_CONFIG);

            //RemoteConfig remoteConfig = (RemoteConfig)Activator.GetObject(typeof(RemoteConfig), IpcConstants.URL_CONFIG);
            //RemoteConfig remoteConfig = (RemoteConfig)SerializeUtils.Fetch(RemoteId);

            SerializeUtils.Fetch(RemoteId, ref buff);
            RemoteConfig remoteConfig = new RemoteConfig();
            remoteConfig.Read(ref buff, 0);

            if (remoteConfig.Loaded)
            {
                Speed = remoteConfig.Speed;
                FPS = remoteConfig.FPS;
                NoteHeight = remoteConfig.NoteHeight;
                HitHeight = remoteConfig.HitHeight;
                NoteStrokeWidth = remoteConfig.NoteStrokeWidth;
                BackgroundPicture = remoteConfig.BackgroundPicture;
                BackgroundPictureInPlaying = remoteConfig.BackgroundPictureInPlaying;
                HasInit = true;
                return true;
            }

            return false;
        }
    }
}
