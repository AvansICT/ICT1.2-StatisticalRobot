
using Avans.StatisticalRobot;
using System.Device.Gpio;

public class DHT11 {

    private readonly int pin;
    /// <summary>
    /// Dit is een analoge device
    /// 3.3V/5V
    /// </summary>
    /// <param name="pin"></param>
    public DHT11(int pin) 
    {
        Robot.SetDigitalPinMode(pin, PinMode.Output);
        this.pin = pin;
    }

    public int[] GetTemperatureAndHumidity() 
    {
        return this.ReadDht11Data();
    }

    private int[] ReadDht11Data()
    {
        int[] dht11_dat = new int[5];
        PinValue lastState = PinValue.High;
        byte counter;
        byte j = 0, i;

        Array.Clear(dht11_dat, 0, dht11_dat.Length);

        Robot.SetDigitalPinMode(this.pin, PinMode.Output);
        Robot.WriteDigitalPin(this.pin, PinValue.Low);
        Robot.Wait(18);
        Robot.WriteDigitalPin(this.pin, PinValue.High);
        Robot.WaitUs(40);
        Robot.SetDigitalPinMode(this.pin, PinMode.Input);

        for (i = 0; i < 85; i++)
        {
            counter = 0;
            while (Robot.ReadDigitalPin(this.pin) == lastState)
            {
                counter++;
                Thread.Sleep(TimeSpan.FromTicks(1));
                if (counter == 255)
                {
                    break;
                }
            }
            lastState = Robot.ReadDigitalPin(this.pin);

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