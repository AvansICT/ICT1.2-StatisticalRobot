using Avans.StatisticalRobot;
using System.Device.I2c;

namespace Avans.StatisticalRobot;

public interface IActuator
{
}

public interface IDisplay : IActuator
{
    public void SetText(string text);

    public void ClearDisplay();
}

public  class I2CDisplay : IDisplay
{
    private byte _i2c_address = 0x00;
    private I2cDevice _device;

    public I2CDisplay(byte i2c_address)
    {
        _i2c_address = i2c_address;
        _device = Robot.CreateI2cDevice(_i2c_address);
    }

    private void WriteCommandToDisplay(byte cmd)
    {
        _device.WriteByteRegister(0x80, cmd);
    }

    public void ClearDisplay()
    {
        WriteCommandToDisplay(0x01);
    }

    public void SetText(string text)
    {
        if (_i2c_address == 0x00)
        {
            throw new Exception("Display address not set");
        }

        ClearDisplay();
        Thread.Sleep(50);
        WriteCommandToDisplay(0x08 | 0x04); // display on, no cursor
        WriteCommandToDisplay(0x28); // 2 lines
        Thread.Sleep(50);
        int count = 0;
        int row = 0;

        foreach (char c in text)
        {
            if (c == '\n' || count == 16)
            {
                count = 0;
                row++;
                if (row == 2) break;
                WriteCommandToDisplay(0xc0);
                if (c == '\n') continue;
            }
            count++;
            _device.WriteByteRegister(0x40, (byte)c);
        }


    }
}

