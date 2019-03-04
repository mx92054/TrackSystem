using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace InsituSystem
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            //程序启动时读取log4net的配置文件。
            log4net.Config.XmlConfigurator.Configure();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frMain());
        }
    }
}
