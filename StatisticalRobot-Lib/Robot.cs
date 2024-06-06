using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Pwm;
using System.Device.Spi;

namespace Avans.StatisticalRobot;

public static class Robot {

    private static I2cBus i2cBus = I2cBus.Create(1);
    private static I2cDevice romi32u4 = i2cBus.CreateDevice(20);
    private static I2cDevice grovePiAnalog = i2cBus.CreateDevice(1);
    private static GpioController gpioController = new GpioController();
    private static PwmChannel pwm;
    private static bool pwmState;


    private static object[] ReadUnpack(int address, int size, string format)
    {
        romi32u4.WriteByte((byte)address);
        Thread.Sleep(1);

        // Lees de gegevens in de buffer
        byte[] readBuffer = new byte[size];
        romi32u4.Read(readBuffer);

        return StructConverter.Unpack(format, readBuffer);
    }

    private static void WritePack(int address, params object[] data)
    {
        byte[] writeBuffer = StructConverter.Pack(data)
            .Prepend((byte)address)
            .ToArray();
        romi32u4.Write(writeBuffer);
        Thread.Sleep(1); 
    }

    private static void WriteRaw(int address, byte[] data) 
    {
        byte[] writeBuffer = data.Prepend((byte)address)
            .ToArray();

        romi32u4.Write(writeBuffer);
        Thread.Sleep(1);
    }

    public static void LEDs(byte red, byte yellow, byte green)
    {
        WritePack(0,"BBB",red,yellow,green);
    }

    public static void PlayNotes(string notes)
    {
        byte[] data = new byte[16];
        data[0] = 1;
        byte[] noteBytes = System.Text.Encoding.ASCII.GetBytes(notes);
        Buffer.BlockCopy(noteBytes, 0, data, 1, Math.Min(noteBytes.Length, 14));
        WriteRaw(24, data);
    }

    public static void Motors(short left, short right)
    {
        WritePack(6,left,right);
    }

    public static bool[] ReadButtons()
    {
        return ReadUnpack(3,3,"???")
            .OfType<bool>()
            .ToArray();
    }

    public static ushort ReadBatteryMillivolts()
    {
        return ReadUnpack(10,2,"H")
            .OfType<ushort>()
            .FirstOrDefault((ushort)0);
    }

    public static ushort[] ReadAnalog()
    {
        return ReadUnpack(12,12,"HHHHHH")
            .OfType<ushort>()
            .ToArray();
    }

    public static short[] ReadEncoders()
    {
        return ReadUnpack(39,4,"hh")
            .OfType<short>()
            .ToArray();
    }

    public static I2cDevice CreateI2cDevice(byte address) 
    {
        return Robot.i2cBus.CreateDevice(address);
    }

    public static void SetPinMode(int pinNumber,PinMode state)
    {
        gpioController.OpenPin(pinNumber,state);
    }

    public static void WritePin(int pinNumber, PinValue value)
    {
        gpioController.Write(pinNumber,value);
    }

    public static PinValue ReadPin(int pinNumber)
    {
        return gpioController.Read(pinNumber);
    }

    public static void SetPwmPin(int frequency, double dutyCyclePercentage )
    {
        if(dutyCyclePercentage < 0 || dutyCyclePercentage > 1) 
        {
            throw new ArgumentOutOfRangeException("Duty Cycle needs to be between 0.0 and 1.0");
        }
        pwm = PwmChannel.Create(0,0,frequency,dutyCyclePercentage);
        pwmState = true;
        pwm.Start();
    }

    public static void ChangePwmFrequency(int frequency)
    {
        pwm.Frequency = frequency;
    }

    public static void ChangePwmDutyCycle(double dutyCycle)
    {
        pwm.DutyCycle = dutyCycle;
    }

    public static void StartStopPwm()
    {
        if(pwmState) pwm.Stop();
        else pwm.Start();
    }

    public static int AnalogRead(byte pin)
    {
        byte[] b = [3, pin, 0, 0];
        try
        {
            grovePiAnalog.Write(b);
            Thread.Sleep(1);
            grovePiAnalog.ReadByte();
            byte[] readBuffer = new byte[4];
            romi32u4.Read(readBuffer); 
            int v1 = (int)readBuffer[1];
            int v2 = (int)readBuffer[2];
            return (v1 * 256) + v2;
        }
        catch (Exception ex)
        {
            // Handle any exceptions or errors
            Console.WriteLine("Error: " + ex.Message);
            return 0;
        }
    }

    public static void SetAnalogType(string mode, byte pin)
    {
        byte[] b;
        if(mode == "output") b = [5, pin, 1, 0];
        else b = [5, pin, 0, 0];
        grovePiAnalog.Write(b);
        Thread.Sleep(1);
    }

}