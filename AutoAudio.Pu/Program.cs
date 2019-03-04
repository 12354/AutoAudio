using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSCore.CoreAudioAPI;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;

namespace AutoAudio.Pu
{
    class AutoAudioOptions
    {
        public List<AudioCurveOption> Curve { get; set; }
        public string ProcessNameNoExe { get; set; }
        public int MillisecondDelay { get; set; }
        public float DefaultVolume { get; set; }

    }

    internal class AudioCurveOption
    {
        public float GameOutputVolume { get; set; }
        public float GameWindowsVolume { get; set; }

        [JsonIgnore]
        public Stopwatch ActiveSince { get; set; } = new Stopwatch();
    }

    class Program
    {
        private static AutoAudioOptions _options;
        private static string APP_NAME_STARTUP = "AutoAudioPu";

        [DllImport("kernel32.dll",
            EntryPoint = "GetStdHandle",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetStdHandle(UInt32 nStdHandle);
        [DllImport("kernel32.dll",
            EntryPoint = "AllocConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern int AllocConsole();
        private const int MY_CODE_PAGE = 437;
        static bool IsStartupItem()
        {
            // The path to the key where Windows looks for startup applications
            var rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (rkApp.GetValue(APP_NAME_STARTUP) == null)
                // The value doesn't exist, the application is not set to run at startup
                return false;
            else
                // The value exists, the application is set to run at startup
                return true;
        }
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        [DllImport("kernel32.dll",
            EntryPoint = "GetStdHandle",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]

        private static extern IntPtr GetStdHandle(int nStdHandle);
        private const int STD_OUTPUT_HANDLE = -11;
        private static bool ConsoleOutput = false;
        static void Main(string[] args)
        {
            try
            {
                Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                if (args.Length == 0)
                {
                    ConsoleOutput = true;
                    AllocConsole();
                    Console.WriteLine("Add to autorun? [y=add,n=remove]");
                    var answer = Console.ReadKey().Key.ToString().Trim().ToLower();
                    if (answer == "y" || answer == "j")
                    {
                        var rkApp =
                            Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                        rkApp.SetValue(APP_NAME_STARTUP,
                            "\"" + System.Reflection.Assembly.GetEntryAssembly().Location + "\" --noconsole");

                    }
                    else
                    {
                        var rkApp =
                            Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                        if (IsStartupItem())
                            // Remove the value from the registry so that the application doesn't start
                            rkApp.DeleteValue(APP_NAME_STARTUP, false);
                    }
                    // Add the value in the registry so that the application runs at startup
                    Console.WriteLine();
                }



                if (!File.Exists("options.txt"))
                {
                    File.WriteAllText("options.txt", JsonConvert.SerializeObject(new AutoAudioOptions()
                    {
                        ProcessNameNoExe = "TslGame",
                        MillisecondDelay = 2000,
                        DefaultVolume = 1,
                        Curve = new List<AudioCurveOption>()
                        {
                            new AudioCurveOption()
                            {
                                GameOutputVolume = 0.02f,
                                GameWindowsVolume = 0.5f,

                            },
                            new AudioCurveOption()
                            {
                                GameOutputVolume = 0.03f,
                                GameWindowsVolume = 0.2f,
                            }

                        }
                    }));
                }
                _options = JsonConvert.DeserializeObject<AutoAudioOptions>(File.ReadAllText("options.txt"),
                    new JsonSerializerSettings()
                    {
                        Formatting = Formatting.Indented,
                    });
                _options.Curve = _options.Curve.OrderBy(option => option.GameOutputVolume).ToList();
                Console.WriteLine("Waiting for " + _options.ProcessNameNoExe);
                while (true)
                {
                    try
                    {
                        using (var sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render))
                        {
                            using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                            {
                                var sessions = sessionEnumerator;
                                foreach (var session in sessions)
                                {
                                    using (var audioMeterInformation = session.QueryInterface<AudioMeterInformation>())
                                    {
                                        using (var session2 = session.QueryInterface<AudioSessionControl2>())
                                        {
                                            using (var audioControl = session.QueryInterface<SimpleAudioVolume>())
                                            {
                                                if (!session2.Process.ProcessName.Contains(_options.ProcessNameNoExe))
                                                    continue;

                                                while (true)
                                                {

                                                    HandleTslGame(session, session2, audioControl,
                                                        audioMeterInformation);
                                                    Thread.Sleep(100);
                                                }
                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Thread.Sleep(5000);
                    }

                    Thread.Sleep(5000);
                }
            }
            catch (Exception e)
            {
                File.WriteAllText("error.log", e.ToString());
                Console.WriteLine(e);
                Console.ReadLine();
            }
        }

        private static float lerp(float a, float b, float by)
        {
            return a * by + b * (1 - by);
        }
        static List<float> _volumes = new List<float>();
        private static int _bufferSize = 10;
        private static AudioCurveOption BaseCurve = new AudioCurveOption();
        private static void HandleTslGame(AudioSessionControl session, AudioSessionControl2 session2, SimpleAudioVolume audioControl, AudioMeterInformation gameVolume)
        {
            
            var pid = session2.Process.ProcessName;
            var curves = _options.Curve;
            
            float finalVolume = 1;
            var currentVolume = audioControl.MasterVolume;
            if (_volumes.Count > _bufferSize)
                _volumes.RemoveAt(0);
            _volumes.Add(gameVolume.PeakValue);
            //var peak = _volumes.Average();
            var peak = _volumes.OrderBy(p => p).ToArray()[_volumes.Count / 2];
            var currentCurve = BaseCurve;
            for (var index = 0; index < curves.Count; index++)
            {
                var audioCurveOption1 = curves[index];
                var audioCurveOption2 = (index +1 < curves.Count) ? curves[index +1] : curves[index];
                if (peak > audioCurveOption1.GameOutputVolume && peak < audioCurveOption2.GameOutputVolume)
                {
                    finalVolume = lerp(audioCurveOption1.GameWindowsVolume, audioCurveOption2.GameWindowsVolume,
                        1f - ((peak - audioCurveOption1.GameOutputVolume) /
                        (audioCurveOption2.GameOutputVolume - audioCurveOption1.GameOutputVolume)));
                    currentCurve = audioCurveOption1;
                }
            }
            if (peak >= curves[curves.Count - 1].GameOutputVolume)
            {
                finalVolume = curves[curves.Count - 1].GameWindowsVolume;
                currentCurve = curves[curves.Count - 1];
            }
            if (peak < curves[0].GameOutputVolume)
            {
                finalVolume = _options.DefaultVolume;
                currentCurve = BaseCurve;
            }
            if (currentVolume != finalVolume)
            {
                var timer = currentCurve.ActiveSince;
                if (!timer.IsRunning)
                {
                    timer.Start();
                    foreach (var audioCurveOption in curves.Concat(new []{BaseCurve}).Except(new []{currentCurve}))
                    {
                        if (audioCurveOption.ActiveSince.ElapsedMilliseconds < _options.MillisecondDelay)
                        {
                            audioCurveOption.ActiveSince.Reset();
                        }
                    }
                }
                else if (timer.ElapsedMilliseconds > _options.MillisecondDelay)
                {
                    audioControl.MasterVolume = finalVolume;
                    if (ConsoleOutput)
                        Console.WriteLine(pid + " " + peak + " set to " + finalVolume);
                    foreach (var audioCurveOption in curves.Concat(new[] { BaseCurve }).Except(new[] { currentCurve }))
                    {
                        audioCurveOption.ActiveSince.Reset();
                    }

                }
            }
            /*
            if (currentVolume != finalVolume)
            {
                var timer = currentCurve.ActiveSince;
                if (!_changeTimer.IsRunning)
                {
                    _changeTimer.Start();
                }
                else if (_changeTimer.ElapsedMilliseconds > _options.MillisecondDelay)
                {
                    audioControl.MasterVolume = finalVolume;
                    Console.WriteLine(pid + " " + peak + " set to " + finalVolume);

                }
            }
            else
            {
                _changeTimer.Stop();
                _changeTimer.Reset();
            }*/
            if(ConsoleOutput)
                Console.Title = "Current volume: " + peak;
        }


        private static AudioSessionManager2 GetDefaultAudioSessionManager2(DataFlow dataFlow)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                using (var device = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia))
                {
                    var sessionManager = AudioSessionManager2.FromMMDevice(device);
                    return sessionManager;
                }
            }
        }
    }
}