using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IpcLibrary
{
    public static class IpcConstants
    {
        public static readonly int GAME_WIDTH = 540;
        public static readonly int GAME_HEIGHT = 960;

        //public static readonly string CHANNEL = "ManiaRTRender";
        public static readonly string OBJECT_CONFIG_NAME = "config";
        private static readonly string OBJECT_RENDER_NAME = "render";
        //public static readonly string URL_CONFIG = GetUrl(OBJECT_CONFIG_NAME);
        public const int SIZE_RENDER = 65536;
        public const int SIZE_CONFIG = 65536;

        public static string GetRenderObjName(int id)
        {
            return $"{OBJECT_RENDER_NAME}_{id}";
        }

        //public static string GetRenderObjUrl(int id)
        //{
        //    Console.WriteLine(GetUrl(GetRenderObjName(id)));
        //    return GetUrl(GetRenderObjName(id));
        //}

        //private static string GetUrl(string name)
        //{
        //    return $"ipc://{CHANNEL}/{name}";
        //}
    }
}
