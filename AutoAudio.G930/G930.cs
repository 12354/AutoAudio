using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magic;

namespace AutoAudio.G930
{
    class G930
    {
        private static uint Offset1 = 0x0100D9C8;
        private static uint Offset2 = 0x10;
        private static uint Offset3 = 0x20;
        private static uint Offset4 = 0x28;
        private static uint Offset5 = 0x10;
        private static uint Offset6 = 0x9c;

        private ulong _batteryAddress = 0;
        private BlackMagic _lcore;
        public G930()
        {
            
        }

        /// <exception cref="Exception">Throws general exception on failure.</exception>
        public int ReadBattery()
        {
            try
            {


                if (!UpdateBlackMagic())
                {
                    //Lcore not running -> Headset not on
                    return 0;
                }

                UpdateBatteryAddress();
                return _lcore.ReadInt(_batteryAddress);
            }
            catch
            {
                _lcore = null;
                return 0;
            }
        }


        private void UpdateBatteryAddress()
        {
            if (_batteryAddress != 0)
            {
                return;
            }

            var adr = (ulong) (_lcore.MainModule.BaseAddress.ToInt64() + Offset1);
            adr = _lcore.ReadUInt64(adr) + Offset2;
            adr = _lcore.ReadUInt64(adr) + Offset3;
            adr = _lcore.ReadUInt64(adr) + Offset4;
            adr = _lcore.ReadUInt64(adr) + Offset5;
            adr = _lcore.ReadUInt64(adr) + Offset6;
            _batteryAddress = adr;
        }

        private bool UpdateBlackMagic()
        {
            if (_lcore != null)
            {
                return true;
            }
            _lcore = new BlackMagic();

            return _lcore.Open(SProcess.GetProcessFromProcessName("LCore"));
        }
    }
}
