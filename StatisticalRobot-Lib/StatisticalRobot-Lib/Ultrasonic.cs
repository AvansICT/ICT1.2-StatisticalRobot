using System.Device.Gpio;

namespace Avans.StatisticalRobot
{
    public class Ultrasonic
    {
        private readonly int _pin;

        /// <summary>
        /// This is a digital device
        /// 3.3V/5V
        /// Detecting range: 0-4m
        /// Resolution: 1cm
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        public Ultrasonic(int pin)
        {
            Robot.SetDigitalPinMode(pin, PinMode.Output);
            _pin = pin;
        }


        public int GetUltrasoneDistance()
        {
            Robot.SetDigitalPinMode(_pin, PinMode.Output);
            Robot.WriteDigitalPin(_pin, PinValue.Low);
            Robot.Wait(1);
            Robot.WriteDigitalPin(_pin, PinValue.High);
            Robot.WaitUs(10);
            Robot.WriteDigitalPin(_pin, PinValue.Low);

            Robot.SetDigitalPinMode(_pin, PinMode.Input);
            int pulse = Robot.PulseIn(_pin, PinValue.High, 50);
            return pulse / 29 / 2;
        }
    }
}
