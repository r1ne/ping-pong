using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Ping_pong
{
    static class Program
    {
        public static menu myMenu;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            myMenu = new menu();
            Application.Run(myMenu);
        }
    }
}
