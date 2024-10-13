namespace Avans.StatisticalRobot;

public class Devices
{

    public class Analog
    {
        /// <summary>
        /// 3.3V/5V
        /// Maxlux detected: 350lux
        /// Responsetime: 20 ~ 30 milliseconds
        /// Peak wavelength: 540 nm
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        public static LightSensor LightSensor(byte pin, int intervalms)
        {
            return new LightSensor(pin, intervalms);
        }
    }

    public class Digital
    {
        /// <summary>
        /// 3.3V/5V
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        /// <param name="msBlink">Time in milliseconds</param>
        public static BlinkLed BlinkLed(int pin, int msBlink)
        {
            return new BlinkLed(pin, msBlink);
        }

        /// <summary>
        /// 3.3V/5V
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        public static Led Led(int pin)
        {
            return new Led(pin);
        }

        /// <summary>
        /// 3.3V/5V
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        /// <param name="defHigh">button has default behaviour: HIGH</param>
        public static Button Button(int pin, bool defHigh = false)
        {
            return new Button(pin, defHigh);
        }

        /// <summary>
        /// 3.3V/5V
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        public static DHT11 TempHumidity(int pin)
        {
            return new DHT11(pin);
        }

        /// <summary>
        /// 3.3V/5V
        /// Detecting range: 0-4m
        /// Resolution: 1cm
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        public static Ultrasonic Ultrasonic(int pin)
        {
            return new Ultrasonic(pin);
        }
    }

    public class I2c
    {
        /// <summary>
        /// 3.3V/5V
        /// </summary>
        /// <param name="address">Address for example: 0x3E</param>
        public static LCD16x2 LCD16x2(byte address)
        {
            return new LCD16x2(address);
        }
    }
}