using System.Device.Gpio;

namespace Avans.StatisticalRobot;

public class Button
{
    private readonly int _pin;
    private readonly bool _defHigh;

    /// <summary>
    /// This is a digital device
    /// 3.3V/5V
    /// </summary>
    /// <param name="pin"></param>
    /// <param name="defHigh">button has default behaviour: HIGH</param>
    public Button(int pin, bool defHigh = false)
    {
        Robot.SetDigitalPinMode(pin, PinMode.Input);
        _pin = pin;
        _defHigh = defHigh;
    }

    private DateTime syncTime = new();
    private PinValue state;

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
        return (state == PinValue.High && _defHigh) ? "Released" : "Pressed";
    }
}