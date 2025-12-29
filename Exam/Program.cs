using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
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

    public class AuthenticateResponse
    {
        public string LmsProbeOrigin { get; set; }
        public bool licence_valid { get; set; }
        public bool login_needed { get; set; }
        public bool detect_camera { get; set; }
        public bool detect_person { get; set; }
        public bool screenshot_needed { get; set; }
        public string default_login_api { get; set; }
        public string msg { get; set; }
        public string SapWebUrl { get; set; }
    }

    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
    
 
        public static string GetMacAddress()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    return nic.GetPhysicalAddress().ToString();
                }
            }
            return string.Empty;
        }

        public static string GetLocalIpAddress()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return string.Empty;
        }

        public static string GetHostName()
        {
            return Dns.GetHostName();
        }

        public static async Task<AuthenticateResponse> AuthenticateAsync()
        {
            var apiUrl = Globals.sapple_licence_url;

            var requestData = new
            {
                mac_address = GetMacAddress(),
                ip_address = GetLocalIpAddress(),
                hostname = GetHostName()
            };

            using (HttpClient client = new HttpClient())
            {
                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                    response.EnsureSuccessStatusCode();

                    string responseJson = await response.Content.ReadAsStringAsync();

                    var result = JsonSerializer.Deserialize<AuthenticateResponse>(
                        responseJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return result;
                }
                catch (Exception ex) { }
                return new AuthenticateResponse();
            }
        }
        [STAThread]
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

            AuthenticateResponse response = AuthenticateAsync().GetAwaiter().GetResult();
            if (response.LmsProbeOrigin != null)
            {
                Globals._msg = string.IsNullOrEmpty(response.msg) ? Globals._msg : response.msg;
                Globals.licence_valid = response.licence_valid;
                Globals.login_needed = response.login_needed;
                Globals.detect_camera = response.detect_camera;
                Globals.detect_person = response.detect_person;
                Globals.screenshot_needed = response.screenshot_needed;


                Globals.default_open_url = string.IsNullOrEmpty(response.LmsProbeOrigin) ? Globals.default_open_url : response.LmsProbeOrigin;
                Globals.default_login_url = string.IsNullOrEmpty(response.default_login_api) ? Globals.default_login_url : response.default_login_api;
                Globals.sapple_lms_url = string.IsNullOrEmpty(response.SapWebUrl) ? Globals.sapple_lms_url : response.SapWebUrl;

            }
            if (!Globals.licence_valid)
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
