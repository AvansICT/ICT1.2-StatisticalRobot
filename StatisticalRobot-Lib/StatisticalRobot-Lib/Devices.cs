
namespace Avans.StatisticalRobot;

public class Devices
{

    public static KnipperLed KnipperLed(int pinLed, int msKnipper) {
        return new KnipperLed(pinLed, msKnipper);
    }

    public static DHT11 TemperatuurEnLuchtvochtigheidSensor(int pinNummer) {
        return new DHT11(pinNummer);
    }

}