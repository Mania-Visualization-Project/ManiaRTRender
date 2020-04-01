using IpcLibrary;
using System;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Windows.Forms;

namespace RenderClient
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length != 1) return args.Length;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RenderForm(int.Parse(args[0])));

            return 0;
        }
    }
}
