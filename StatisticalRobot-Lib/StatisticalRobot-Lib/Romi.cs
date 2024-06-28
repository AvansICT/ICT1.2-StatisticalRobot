using System;
using System.IO;
using System.Threading;
using System.Device.I2c;
using static StructConverter;

public class Romi
{
    private I2cBus i2cBus;
    private I2cDevice romi32u4;

    public Romi()
    {
        i2cBus = I2cBus.Create(1);
        romi32u4 = i2cBus.CreateDevice(20);
    }

    private object[] ReadUnpack(int address, int size, string format)
    {
        romi32u4.WriteByte((byte)address);
        Thread.Sleep(1); // 100 microseconden = 0.0001 seconden

        // Lees de gegevens in de buffer
        byte[] readBuffer = new byte[size];
        romi32u4.Read(readBuffer);

        return StructConverter.Unpack(format, readBuffer);
    }

    private void WritePack(int address, params object[] data)
    {
        byte[] writeBuffer = StructConverter.Pack(data)
            .Prepend((byte)address)
            .ToArray();
        romi32u4.Write(writeBuffer);
        Thread.Sleep(1); // 100 microseconden = 0.0001 seconden
    }

    public void LEDs(byte red, byte yellow, byte green)
    {
        WritePack(0,"BBB",red,yellow,green);
    }

    public void PlayNotes(string notes)
    {
        byte[] data = new byte[16];
        data[0] = 1;
        byte[] noteBytes = System.Text.Encoding.ASCII.GetBytes(notes);
        Buffer.BlockCopy(noteBytes, 0, data, 1, Math.Min(noteBytes.Length, 15));
        WritePack(24,data);
    }

    public void Motors(short left, short right)
    {
        WritePack(6,left,right);
    }

    public bool[] ReadButtons()
    {
        return ReadUnpack(3,3,"???")
            .OfType<bool>()
            .ToArray();
    }

    public ushort ReadBatteryMillivolts()
    {
        return ReadUnpack(10,2,"H")
            .OfType<ushort>()
            .FirstOrDefault((ushort)0);
    }

    public ushort[] ReadAnalog()
    {
        return ReadUnpack(12,12,"HHHHHH")
            .OfType<ushort>()
            .ToArray();
    }

    public short[] ReadEncoders()
    {
        return ReadUnpack(39,4,"hh")
            .OfType<short>()
            .ToArray();
    }
}

