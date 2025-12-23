using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Exam
{
    public static class Globals
    {
        public static bool licence_valid = Convert.ToBoolean( ConfigurationManager.AppSettings["_licence_valid"]); //if false will show the below _msg
        public static String _msg = "success";

        public static bool login_needed = Convert.ToBoolean(ConfigurationManager.AppSettings["_login_needed"]); // skips if login is not needed
        public static bool detect_camera = Convert.ToBoolean(ConfigurationManager.AppSettings["_detect_camera"]); // if true will detect camera setup
        public static bool detect_person = Convert.ToBoolean(ConfigurationManager.AppSettings["_detect_person"]); // if true will detect person
        public static bool screenshot_needed = Convert.ToBoolean(ConfigurationManager.AppSettings["_screenshot_needed"]); // if set to true it will take screenshots
        public static String default_login_url = ConfigurationManager.AppSettings["_default_login_api"]; // login api
        public static String default_open_url = ConfigurationManager.AppSettings["LmsProbeOrigin"]; // default open url
        public static String sapple_lms_url = ConfigurationManager.AppSettings["SapWebUrl"]; // sapple popup url
        public static String sapple_licence_url = ConfigurationManager.AppSettings["apiWebUrl"]; // sapple licence key url

        public static void KillDistractionApps()
        {
            // Extended list of distraction / communication / dev tools
            string[] apps =
            {
        // Chat / Communication
        "whatsapp", "telegram", "discord", "skype", "teams", "slack", "zoom", "viber", "signal",
        "line", "wechat", "messenger", "yourphone",

        // Social media & browsers
        "chrome", "firefox", "opera", "brave", "vivaldi",

        // Dev / API tools
        "postman", "insomnia", "fiddler", "charles", "wireshark",

        // File sharing / remote control
        "anydesk", "teamviewer", "ultraviewer", "rdpclip",

        // Media / entertainment
        "spotify", "vlc", "netflix", "itunes", "music", "videos",

        // Miscellaneous distractions
        "notion", "obsidian", "todoist", "evernote"
    };

            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (apps.Any(a => p.ProcessName.ToLower().Contains(a)))
                    {
                        p.Kill();
                    }
                }
                catch
                {
                    // Ignored intentionally (some system processes may not be killable)
                }
            }
        }

    }
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        //static void Main()
        //{
        //    Application.EnableVisualStyles();
        //    Application.SetCompatibleTextRenderingDefault(false);
        //    Application.Run(new Login());
        //}

        static void Main()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var section = config.GetSection("appSettings");

                // encrypt only if not already protected
                if (section != null && !section.SectionInformation.IsProtected)
                {
                    Exam.EncryptConfig();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error encrypting config: " + ex.Message);
            }
            // TODO
            // call first api to send the mac address, IP address and get settings back and set it in Globals Class
            //
            //
            //

            if(!Globals.licence_valid)
            {
                MessageBox.Show("ℹ️ Licence Expired or Seat Limit exceeded, contact Admin.\n"+Globals._msg);
                return;
            }
            
                Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (Globals.login_needed)
            {
                Application.Run(new Login());
            }
            else
            {
                Exam exam = new Exam();
                Application.Run(exam);
               
                Globals.KillDistractionApps();
            }
        }
    }
}
