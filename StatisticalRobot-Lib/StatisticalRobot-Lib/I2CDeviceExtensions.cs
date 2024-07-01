
using System.Device.I2c;

/// <summary>
/// Makes it possible to specify what data needs to be read or written to what register
/// </summary>
public static class I2CDeviceExtensions
{
    public static void WriteRegister(this I2cDevice device, byte register) {
        device.Write([ register ]);
        Thread.Sleep(1);
    }

    public static void WriteRegister(this I2cDevice device, byte register, ReadOnlySpan<byte> data) 
    {
        byte[] writeBuffer = new byte[data.Length + 1];

        writeBuffer[0] = register;
        data.CopyTo(writeBuffer.AsSpan().Slice(1));

        device.Write(writeBuffer);
        Thread.Sleep(1);
    }
    public static void WriteByteRegister(this I2cDevice device, byte register, byte data) 
    {
        byte[] writeBuffer = [register, data];  
        device.Write(writeBuffer);
        Thread.Sleep(1);
    }
    public static void ReadRegister(this I2cDevice device, byte register, Span<byte> readBuffer) 
    {
        // Trigger the register for reading
        device.WriteRegister(register);

        // Actually read the register
        device.Read(readBuffer);
    }

}