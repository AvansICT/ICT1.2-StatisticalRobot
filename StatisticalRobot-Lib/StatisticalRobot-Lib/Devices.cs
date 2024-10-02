namespace Avans.StatisticalRobot;

public class Devices
{

    public static KnipperLed KnipperLed(int pinLed, int msKnipper)
    {
        return new KnipperLed(pinLed, msKnipper);
    }

    public static DHT11 TemperatuurEnLuchtvochtigheidSensor(int pinNummer)
    {
        return new DHT11(pinNummer);
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