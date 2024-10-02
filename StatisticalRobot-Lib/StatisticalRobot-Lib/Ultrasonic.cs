using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Avans.StatisticalRobot
{
    public class Ultrasonic
    {
        public int Pin { get; }

        /// <summary>
        /// Dit is een digitaal device
        /// 3.3V/5V
        /// </summary>
        /// <param name="pin"></param>
        public Ultrasonic(int pin)
        {
            Robot.SetDigitalPinMode(pin, PinMode.Output);
            Pin = pin;
        }


        public int GetUltrasoneDistance()
        {
            Robot.SetDigitalPinMode(Pin, PinMode.Output);
            Robot.WriteDigitalPin(Pin, PinValue.Low);
            Robot.Wait(1);
            Robot.WriteDigitalPin(Pin, PinValue.High);
            Robot.WaitUs(10);
            Robot.WriteDigitalPin(Pin, PinValue.Low);

            Robot.SetDigitalPinMode(Pin, PinMode.Input);
            int pulse = Robot.PulseIn(Pin, PinValue.High, 50);
            return pulse / 29 / 2;
        }
    }
}
