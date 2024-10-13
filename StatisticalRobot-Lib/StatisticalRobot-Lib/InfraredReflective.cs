using System.Device.Gpio;

namespace Avans.StatisticalRobot
{
    public class InfraredReflective
    {
        private readonly int _pin;

        /// <summary>
        /// This is a digital device
        /// 3.3V/5V
        /// Distance: 0 - 4.5 cm (best height is 1.2 cm)
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        public InfraredReflective(int pin)
        {
            Robot.SetDigitalPinMode(pin, PinMode.Input);
            _pin = pin;
        }

        private DateTime syncTime = new();
        private PinValue state;

        private int Update()
        {
            if (DateTime.Now - syncTime > TimeSpan.FromMilliseconds(50))
            {
                state = Robot.ReadDigitalPin(_pin);
                syncTime = DateTime.Now;
                return (int)state;
            }
            return -1;
        }

        public int Watch()
        {
            return Update();
        }

    }
}
