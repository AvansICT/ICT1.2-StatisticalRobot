
using System.Device.Gpio;

namespace Avans.StatisticalRobot;

public class Button
{
    private readonly int _pin;

    /// <summary>
    /// Dit is een digitaal device
    /// 3.3V/5V
    /// </summary>
    /// <param name="pin"></param>
    public Button(int pin)
    {
        Robot.SetDigitalPinMode(pin, PinMode.Input);
        _pin = pin;
    }

    private DateTime syncTime = new();
    private PinValue state = PinValue.High;

    private void Update()
    {
        if (DateTime.Now - syncTime > TimeSpan.FromMilliseconds(250))
        {
            state = Robot.ReadDigitalPin(_pin);
            syncTime = DateTime.Now;
        }
    }

    public string GetState()
    {
        Update();
        return state == PinValue.High ? "Released" : "Pressed";
    }
}