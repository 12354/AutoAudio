using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AudioSwitcher.AudioApi.CoreAudio;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.Win32;

namespace AutoAudio
{
    internal static class Program
    {
        /// <summary>The application name used for starting the program on windows startup
        /// .</summary>
        private static string APP_NAME_STARTUP = "AutoAudio";

        /// <summary>Determines whether AutoAudio is already automatically run on windows startup.</summary>
        /// <returns>
        ///   <c>true</c> if AutoAudio runs on startup; otherwise, <c>false</c>.</returns>
        static bool IsStartupItem()
        {
            // The path to the key where Windows looks for startup applications
            var rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (rkApp?.GetValue(APP_NAME_STARTUP) == null)
            {
                // The value doesn't exist, the application is not set to run at startup
                return false;
            }
            // The value exists, the application is set to run at startup
            return true;
        }


        static void Main()
        {
            try
            {
                Environment.CurrentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)
                                               ?? Environment.CurrentDirectory;
                
                var controller = new CoreAudioController();

                var options = new AutoAudioOptions();
                if (!File.Exists("options.txt"))
                {
                    FirstTimeSetup(controller, options);
                }
                else
                {
                    options = JsonConvert.DeserializeObject<AutoAudioOptions>(File.ReadAllText("options.txt"));
                }

                AutoAudioMainLoop(controller, options);
            }
            catch (Exception e)
            {
                File.WriteAllText("Fehler.log", e.ToString());
            }
        }

        private static void AutoAudioMainLoop(CoreAudioController controller, AutoAudioOptions options)
        {
            var audio2 = true;
            var first = true;
            var playback = controller.GetPlaybackDevices().ToList();
            var device1 = playback.Single(d => d.FullName.Contains(options.AudioDevice1));
            var device2 = playback.Single(d => d.FullName.Contains(options.AudioDevice2));
            var processName = options.ProcessAudioDevice2?.Replace(".exe", "") ?? "";

            while (true)
            {
                var time = DateTime.Now;
                var p = Process.GetProcessesByName(processName).Any();
                if (p && (!audio2 || first))
                {

                    if (!device1.FullName.Contains(options.NightTimeDontSwitchFromThisAudioDevice) ||
                        options.NightTimeStart >= time.Hour && time.Hour >= options.NightTimeEnd)
                    {
                        device2.SetAsDefault();
                        audio2 = true;
                        first = false;
                    }

                }

                if (!p && audio2)
                {
                    if (!device2.FullName.Contains(options.NightTimeDontSwitchFromThisAudioDevice) ||
                        options.NightTimeStart >= time.Hour && time.Hour >= options.NightTimeEnd)
                    {
                        device1.SetAsDefault();
                        audio2 = false;
                        first = false;
                    }

                }

                Thread.Sleep(500);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private static void FirstTimeSetup(CoreAudioController controller, AutoAudioOptions options)
        {
            NativeWindows.AllocAndShowConsole();
            var playbackDevices = controller.GetPlaybackDevices().ToList();
            for (var i = 0; i < playbackDevices.Count; i++)
            {
                Console.WriteLine(i + " : " + playbackDevices[i].FullName);
            }
            Console.Write("Speakers:");
            int id = InputPlaybackDeviceID(playbackDevices);
            options.AudioDevice1 = playbackDevices[id].FullName;
            Console.Write("Headset:");
            id = InputPlaybackDeviceID(playbackDevices);
            options.AudioDevice2 = playbackDevices[id].FullName;
            Console.Write("Process(ts3client_win64):");
            options.ProcessAudioDevice2 = Console.ReadLine();
            File.WriteAllText("options.txt", JsonConvert.SerializeObject(options, Formatting.Indented));
            Console.WriteLine("Add to autorun y/n?");
            var answer = Console.ReadKey().Key.ToString().Trim().ToLower();
            if (answer == "y" || answer == "j")
            {
                var rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (!IsStartupItem())
                    // Add the value in the registry so that the application runs at startup
                    rkApp?.SetValue(APP_NAME_STARTUP, System.Reflection.Assembly.GetEntryAssembly().Location);
            }
            else
            {
                var rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (IsStartupItem())
                    // Remove the value from the registry so that the application doesn't start
                    rkApp?.DeleteValue(APP_NAME_STARTUP, false);
            }
            NativeWindows.HideConsole();
        }

        private static int InputPlaybackDeviceID(IReadOnlyCollection<CoreAudioDevice> playbackDevices)
        {
            int id;
            while (!(int.TryParse(Console.ReadLine(), out id) || id < 0 || id >= playbackDevices.Count))
            {

            }

            return id;
        }
    }
}

