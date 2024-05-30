using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Pwm;

public class GrovePi{

    private I2cBus i2cBus;
    public PwmChannel pwm;
    public digitalD5;
    public digitalD16;
    public digitalD18;
    public digitalD22;
    public digitalD24;
    public digitalD26;
    public I2cDevice i2cPort1;
    public I2cDevice i2cPort2;
    public I2cDevice i2cPort3;

    public GrovePi()
    {
        i2cBus = I2cBus.Create(1); 

    }

    public I2cDevice initPort1(byte address)
    {
        i2cPort1 = i2cBus.CreateDevice(address);
        return i2cPort1;
    }
}