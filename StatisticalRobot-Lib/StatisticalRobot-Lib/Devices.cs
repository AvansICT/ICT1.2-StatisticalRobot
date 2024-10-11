namespace Avans.StatisticalRobot;

public class Devices
{
    /// <summary>
    /// This is a digital device
    /// 3.3V/5V
    /// </summary>
    /// <param name="pin">Pin number on grove board</param>
    /// <param name="msBlink">Time in milliseconds</param>
    public static BlinkLed BlinkLed(int pin, int msBlink)
    {
        return new BlinkLed(pin, msBlink);
    }

    /// <summary>
    /// This is a digital device
    /// 3.3V/5V
    /// </summary>
    /// <param name="pin">Pin number on grove board</param>
    public static Led Led(int pin)
    {
        return new Led(pin);
    }

    /// <summary>
    /// This is a digital device
    /// 3.3V/5V
    /// </summary>
    /// <param name="pin">Pin number on grove board</param>
    /// <param name="defHigh">button has default behaviour: HIGH</param>
    public static Button Button(int pin, bool defHigh = false)
    {
        return new Button(pin, defHigh);
    }

    /// <summary>
    /// This is a digital device
    /// 3.3V/5V
    /// </summary>
    /// <param name="pin">Pin number on grove board</param>
    public static DHT11 TempHumidity(int pin)
    {
        return new DHT11(pin);
    }

    /// <summary>
    /// Dit is een I2C device
    /// 3.3V/5V
    /// </summary>
    /// <param name="address">Address for example: 0x3E</param>
    public static LCD16x2 LCD16x2(byte address)
    {
        return new LCD16x2(address);
    }

    /// <summary>
    /// This is a digital device
    /// 3.3V/5V
    /// Detecting range: 0-4m
    /// Resolution: 1cm
    /// </summary>
    /// <param name="pin">Pin number on grove board</param>
    public static Ultrasonic Ultrasonic(int pin)
    {
        return new Ultrasonic(pin);
    }

}