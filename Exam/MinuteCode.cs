using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Configuration;

namespace Exam
{
    public static class MinuteCode
    {
        //private const string MasterSecret = "xR0JKj+0PmPpib77mhPvxbwPuz/PHOrPhmeZUZG5Qa8";
        private static readonly string MasterSecret = ConfigurationManager.AppSettings["MasterSecret"];

        // Call this to get the code string like "9f13a2c7-482913"
        public static string GetCurrentDeviceMinuteCode(int digits = 6, int stepSeconds = 60)
        {
            string deviceHash = GetDeviceHash();                 
            long unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long timeStep = unix / stepSeconds;

            string payload = $"{deviceHash}:{timeStep}";
            string otp = ComputeNumericHmacCode(MasterSecret, payload, digits);

            return $"{deviceHash}-{otp}";
        }

        // How many seconds until code changes (for UI countdown)
        public static int SecondsUntilNextTick(int stepSeconds = 60)
        {
            long unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return (int)(stepSeconds - (unix % stepSeconds));
        }

        // --- helpers ---

        private static string GetDeviceHash()
        {
            // Use Windows MachineGuid; stable per install
            string machineGuid = ReadRegistry(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography",
                "MachineGuid"
            );
            if (string.IsNullOrWhiteSpace(machineGuid))
                machineGuid = Environment.MachineName; // fallback

             var sha = SHA256.Create();
            byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(machineGuid));
            // first 4 bytes => 8 hex chars, enough as a compact, stable device tag
            return BitConverter.ToString(h, 0, 4).Replace("-", "").ToLowerInvariant();
        }

        private static string ReadRegistry(string key, string valueName)
        {
            try
            {
                object v = Registry.GetValue(key, valueName, null);
                return v?.ToString();
            }
            catch { return null; }
        }

        private static string ComputeNumericHmacCode(string secret, string message, int digits)
        {
             var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            byte[] mac = h.ComputeHash(Encoding.UTF8.GetBytes(message));


            // dynamic truncation (like TOTP)
            int offset = mac[mac.Length - 1] & 0x0F;
            int binCode =
                ((mac[offset] & 0x7F) << 24) |
                ((mac[offset + 1] & 0xFF) << 16) |
                ((mac[offset + 2] & 0xFF) << 8) |
                (mac[offset + 3] & 0xFF);

            int mod = (int)Math.Pow(10, digits);
            int otp = binCode % mod;
            return otp.ToString().PadLeft(digits, '0');
        }
    }
}
