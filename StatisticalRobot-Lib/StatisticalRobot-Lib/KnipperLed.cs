
using System.Device.Gpio;

namespace Avans.StatisticalRobot;

public class KnipperLed : IUpdatable
{

    public int PinLed { get; }
    public int MsKnipper { get; }

    public KnipperLed(int pinLed, int msKnipper)
    {
        Robot.SetDigitalPinMode(pinLed, PinMode.Output);
        PinLed = pinLed;
        MsKnipper = msKnipper;
    }

    private DateTime laatsteKnipper = new DateTime();
    private PinValue ledState = PinValue.Low;

    public void Update()
    {
        if(DateTime.Now - laatsteKnipper > TimeSpan.FromMilliseconds(MsKnipper)) {
            ledState = !ledState;
            Robot.WriteDigitalPin(this.PinLed, ledState);
            laatsteKnipper = DateTime.Now;
        }

        // while(true) {
        //     Robot.WriteDigitalPin(this.PinLed, PinValue.High);
        //     Robot.Wait(this.MsKnipper);
        //     Robot.WriteDigitalPin(this.PinLed, PinValue.Low);
        //     Robot.Wait(this.MsKnipper);
        // }
    }
}