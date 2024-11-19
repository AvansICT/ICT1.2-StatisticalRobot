using System.Device.Gpio;

namespace Avans.StatisticalRobot;

public class Buzzer
{
    private readonly int _pin;
    private readonly int _intervalms;

    /// <summary>
    /// This is a digital device
    /// Beeps (On/Off)
    /// 3.3V/5V
    /// Resonant Frequency: 2300±300Hz
    /// </summary>
    /// <param name="pin">Pin number on grove board</param>
    /// <param name="intervalms">Time in milliseconds between beeps</param>
    public Buzzer(int pin, int intervalms)
    {
        Robot.SetDigitalPinMode(pin, PinMode.Output);
        _pin = pin;
        _intervalms = intervalms;
    }

    private DateTime syncTime = new();
    private PinValue state;

    private void Update()
    {
        if (DateTime.Now - syncTime > TimeSpan.FromMilliseconds(_intervalms))
        {
            Robot.WriteDigitalPin(_pin, state);
            syncTime = DateTime.Now;
            state = !state;
        }
    }

    public void Beep()
    {
        Update();
    }

}