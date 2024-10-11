
using Avans.StatisticalRobot;
using System.Device.Gpio;

public class TempHumidity
{
    private readonly int _pin;

    /// <summary>
    /// This is a digital device
    /// 3.3V/5V
    /// </summary>
    /// <param name="pin">pin number</param>
    public TempHumidity(int pin) 
    {
        Robot.SetDigitalPinMode(pin, PinMode.Output);
        _pin = pin;
    }

    public int[] GetTemperatureAndHumidity() 
    {
        return ReadDht11Data();
    }

    private int[] ReadDht11Data()
    {
        int[] dht11_dat = new int[5];
        PinValue lastState = PinValue.High;
        byte counter;
        byte j = 0, i;

        Array.Clear(dht11_dat, 0, dht11_dat.Length);

        Robot.SetDigitalPinMode(_pin, PinMode.Output);
        Robot.WriteDigitalPin(_pin, PinValue.Low);
        Robot.Wait(18);
        Robot.WriteDigitalPin(_pin, PinValue.High);
        Robot.WaitUs(40);
        Robot.SetDigitalPinMode(_pin, PinMode.Input);

        for (i = 0; i < 85; i++)
        {
            counter = 0;
            while (Robot.ReadDigitalPin(_pin) == lastState)
            {
                counter++;
                Thread.Sleep(TimeSpan.FromTicks(1));
                if (counter == 255)
                {
                    break;
                }
            }
            lastState = Robot.ReadDigitalPin(_pin);

            if (counter == 255)
                break;

            if ((i >= 4) && (i % 2 == 0))
            {
                dht11_dat[j / 8] <<= 1;
                if (counter > 16)
                    dht11_dat[j / 8] |= 1;
                j++;
            }
        }

        if ((j >= 40) &&
            (dht11_dat[4] == ((dht11_dat[0] + dht11_dat[1] + dht11_dat[2] + dht11_dat[3]) & 0xFF)))
        {
            //fahrenheit = dht11_dat[2] * 9f / 5f + 32;
            return dht11_dat;
        }
        else
        {
            Array.Clear(dht11_dat, 0, dht11_dat.Length);
            return dht11_dat;
        }
    }

}