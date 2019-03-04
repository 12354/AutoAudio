using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AudioSwitcher.AudioApi.CoreAudio;
using Magic;
using Newtonsoft.Json;

namespace AutoAudio.G930
{
    class AutoAudioOptions
    {
        public int NightTimeStart { get; set; }
        public int NightTimeEnd { get; set; }
        public string NightTimeDontSwitchFromThisAudioDevice { get; set; }

        public int Version { get; set; }
        public string AudioDevice1 { get; set; }
        public string AudioDevice2 { get; set; }
        public string ProcessAudioDevice2 { get; set; }
    }
    class Program
    {
        private static uint Offset1 = 0x0100D9C8;
        private static uint Offset2 = 0x10;
        private static uint Offset3 = 0x20;
        private static uint Offset4 = 0x28;
        private static uint Offset5 = 0x10;
        private static uint Offset6 = 0x9c;
        private static AutoAudioOptions _options;

        static void Main(string[] args)
        {
            bool wasHeadsetOnBefore = false;

            try
            {
                Environment.CurrentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
               
                _options = new AutoAudioOptions();
                _options = JsonConvert.DeserializeObject<AutoAudioOptions>(File.ReadAllText("options.txt"));
                while (true)
                {
                    try
                    {
                        Run();
                    }
                    catch (Exception ee)
                    {
                        Console.WriteLine(ee);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
       
        }

        private static void Run()
        {
            var g930 = new G930();
            
            var controller = new CoreAudioController();
            var playback = controller.GetPlaybackDevices();
            var speakersDevice = playback.Single(d => d.FullName.Contains(_options.AudioDevice1));
            var headsetDevice = playback.Single(d => d.FullName.Contains(_options.AudioDevice2));
            while (true)
            {
                var batteryLevel = g930.ReadBattery();
                Console.WriteLine("Battery level: "+ batteryLevel);
                if (batteryLevel > 0 && !headsetDevice.IsDefaultDevice)
                {
                    headsetDevice.SetAsDefault();
                }

                if (batteryLevel == 0 && !speakersDevice.IsDefaultDevice)
                {
                    speakersDevice.SetAsDefault();
                }
                Thread.Sleep(500);
            }
        }
    }
}
