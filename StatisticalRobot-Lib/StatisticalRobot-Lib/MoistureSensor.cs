using System.Device.Gpio;

namespace Avans.StatisticalRobot
{
    public class MoistureSensor
    {
        private readonly byte _pin;

        /// <summary>
        /// This is a analog device
        /// 3.3V/5V
        /// </summary>
        /// <param name="pin">Pin number on grove board (A0=0, A2=2)</param>
        public MoistureSensor(byte pin)
        {
            _pin = pin;
        }


        private int Update()
        {
            int moisture = Robot.AnalogRead(_pin);
            return moisture;
        }

        public string GetMoisture()
        {
            string returnStr = "";
            int m = Update();
            if (0 <= m && m < 300)
                returnStr = "Dry";
            else if (300 <= m && m < 600)
                returnStr = "Moist";
            else
                returnStr = "Wet";
            return returnStr;
        }
    }
}
