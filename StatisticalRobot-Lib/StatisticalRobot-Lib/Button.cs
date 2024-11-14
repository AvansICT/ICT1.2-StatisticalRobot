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
    /// <param name="pin">Pin number on grove board</param>
    /// <param name="defHigh">button has default behaviour: HIGH</param>
    public Button(int pin, bool defHigh = false)
    {
        Robot.SetDigitalPinMode(pin, PinMode.Input);
        _pin = pin;
        _defHigh = defHigh;
    }

    public string GetState()
    {
        return (Robot.ReadDigitalPin(_pin) == PinValue.High) ? "Released" : "Pressed";
    }
}