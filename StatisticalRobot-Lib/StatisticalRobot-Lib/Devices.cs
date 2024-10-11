namespace Avans.StatisticalRobot;

public class Devices
{
    /// <summary>
    /// This is a digital device
    /// 3.3V/5V
    /// </summary>
    /// <param name="pin"></param>
    /// <param name="msBlink">Time in milliseconds</param>
    public static BlinkLed BlinkLed(int pin, int msBlink)
    {
        return new BlinkLed(pin, msBlink);
    }

    /// <summary>
    /// This is a digital device
    /// 3.3V/5V
    /// </summary>
    /// <param name="pin"></param>
    /// <param name="defHigh">button has default behaviour: HIGH</param>
    public static Button Button(int pin, bool defHigh = false)
    {
        return new Button(pin, defHigh);
    }

    public static DHT11 TempHumidity(int pin)
    {
        return new DHT11(pin);
    }

    public static LCD16x2 LCD16x2(byte address)
    {
        return new LCD16x2(address);
    }

    public static Ultrasonic Ultrasonic(int pin)
    {
        return new Ultrasonic(pin);
    }

}