using System;
using System.Threading;
using System.Device.I2c;

namespace Avans.StatisticalRobot
{

    public class RGBSensor
    {
        private const byte TCS34725_ADDRESS = 0x29;
        private const byte TCS34725_COMMAND_BIT = 0x80;
        private const byte TCS34725_ENABLE = 0x00;
        private const byte TCS34725_ENABLE_PON = 0x01;
        private const byte TCS34725_ENABLE_AEN = 0x02;
        private const byte TCS34725_ID = 0x12;
        private const byte TCS34725_ATIME = 0x01;
        private const byte TCS34725_CONTROL = 0x0F;
        private const byte TCS34725_CDATAL = 0x14;
        private const byte TCS34725_RDATAL = 0x16;
        private const byte TCS34725_GDATAL = 0x18;
        private const byte TCS34725_BDATAL = 0x1A;

        private I2cDevice _device;
        private bool _initialized;
        private byte _integrationTime;
        private byte _gain;

        public RGBSensor(byte address, byte integrationTime, byte gain)
        {
            _device = Robot.CreateI2cDevice(address);
            _integrationTime = integrationTime;
            _gain = gain;
            _initialized = false;
        }

        private void Write8(byte reg, byte value)
        {
            _device.Write(new byte[] { (byte)(TCS34725_COMMAND_BIT | reg), value });
        }

        private byte Read8(byte reg)
        {
            _device.WriteByte((byte)(TCS34725_COMMAND_BIT | reg));
            byte[] readBuffer = new byte[1];
            _device.Read(readBuffer);
            return readBuffer[0];
        }

        private ushort Read16(byte reg)
        {
            _device.WriteByte((byte)(TCS34725_COMMAND_BIT | reg));
            byte[] readBuffer = new byte[2];
            _device.Read(readBuffer);
            return (ushort)((readBuffer[1] << 8) | readBuffer[0]);
        }

        public void Enable()
        {
            Write8(TCS34725_ENABLE, TCS34725_ENABLE_PON);
            Thread.Sleep(3);
            Write8(TCS34725_ENABLE, (byte)(TCS34725_ENABLE_PON | TCS34725_ENABLE_AEN));
        }

        public void Disable()
        {
            byte reg = Read8(TCS34725_ENABLE);
            Write8(TCS34725_ENABLE, (byte)(reg & ~(TCS34725_ENABLE_PON | TCS34725_ENABLE_AEN)));
        }

        public bool Begin()
        {
            byte x = Read8(TCS34725_ID);
            if (x != 0x44 && x != 0x10)
            {
                return false;
            }
            _initialized = true;
            SetIntegrationTime(_integrationTime);
            SetGain(_gain);
            Enable();
            return true;
        }

        public void SetGain(byte gain)
        {
            if (!_initialized)
            {
                Begin();
            }
            Write8(TCS34725_CONTROL, gain);
            _gain = gain;
        }

        public void SetIntegrationTime(byte integrationTime)
        {
            if (!_initialized)
            {
                Begin();
            }
            Write8(TCS34725_ATIME, integrationTime);
            _integrationTime = integrationTime;

        }

        public void GetRawData(out ushort r, out ushort g, out ushort b, out ushort c)
        {
            if (!_initialized)
            {
                Begin();
            }
            c = Read16(TCS34725_CDATAL);
            r = Read16(TCS34725_RDATAL);
            g = Read16(TCS34725_GDATAL);
            b = Read16(TCS34725_BDATAL);

            switch (_integrationTime)
            {
                case 0xFF: // TCS34725_INTEGRATIONTIME_2_4MS
                    Thread.Sleep(3);
                    break;
                case 0xF6: // TCS34725_INTEGRATIONTIME_24MS
                    Thread.Sleep(24);
                    break;
                case 0xEB: // TCS34725_INTEGRATIONTIME_50MS
                    Thread.Sleep(50);
                    break;
                case 0xD5: // TCS34725_INTEGRATIONTIME_101MS
                    Thread.Sleep(101);
                    break;
                case 0xC0: // TCS34725_INTEGRATIONTIME_154MS
                    Thread.Sleep(154);
                    break;
                case 0x00: // TCS34725_INTEGRATIONTIME_700MS
                    Thread.Sleep(700);
                    break;
            }
        }
    }
}
