using IpcLibrary;
using System;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Windows.Forms;

namespace RenderClient
{
    static class Program
    {
        public static int ParentId = -1;
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            //args = new string[] { "0", $"{Process.GetCurrentProcess().Id}" };
            if (args.Length != 2) return -args.Length;
            ParentId = int.Parse(args[1]);
            var id = int.Parse(args[0]);
            //var id = 0;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RenderForm(id));

            return 0;
        }
    }
}
