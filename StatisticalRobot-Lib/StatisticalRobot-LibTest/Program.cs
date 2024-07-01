using System.Device.Gpio;
using Avans.StatisticalRobot;

Robot.Wait(100);

int leftDistance;
int rightDistance;
Robot.SetDigitalPinMode(5,PinMode.Output);
Robot.SetDigitalPinMode(16, PinMode.Output);

Robot.Motors(0,0);

ushort batteryData = Robot.ReadBatteryMillivolts();
Console.WriteLine(batteryData);

bool [] buttonData = Robot.ReadButtons();
bool buttonA = buttonData[0];
bool buttonB = buttonData[1];
bool buttonC = buttonData[2];

Robot.SetPwmPin(400,0.1);
Robot.StartPwm();

Robot.Wait(2000);

while(!(buttonA && buttonB)) 
{
    int[] humTempData = ReadDht11Data(18);

    Console.WriteLine($"Humidity={humTempData[0]}.{humTempData[1]}%/nTemperature={humTempData[2]}.{humTempData[3]}C");

    leftDistance = GetUltrasoneDistance(5);
    rightDistance = GetUltrasoneDistance(16);

    if(leftDistance <= 30 && rightDistance >= 50)
    {
        Robot.Motors(100,60);
    }
    else if(leftDistance >= 50 && rightDistance <= 30)
    {
        Robot.Motors(60,100);
    }
    else if(leftDistance <= 20 && rightDistance <= 20)
    {
        for(short i = 100; i > 0; i -= 5) {
            Robot.Motors(i, i);
            Robot.Wait(10);
        }
        Robot.Motors(-100,100);
        Robot.Wait(900);
        Robot.Motors(0,0);
    }
    else
    {
        Robot.Motors(100,100);
    }


    buttonData = Robot.ReadButtons();
    buttonA = buttonData[0];
    buttonB = buttonData[1];
    buttonC = buttonData[2];

    // if(buttonB&&buttonC)
    // {
    //     for(short i = 0; i < 400; i += 5) {
    //         Robot.Motors(i, i);
    //         await Task.Delay(10);
    //     }

    //     for(short i = 400; i > 0; i -= 5) {
    //         Robot.Motors(i, i);
    //         await Task.Delay(10);
    //     }
    //     Robot.Motors(0,0);
    // }

    // if(buttonA) Robot.WriteDigitalPin(5,PinValue.High);
    // else        Robot.WriteDigitalPin(5,PinValue.Low);

    // if(buttonB) valueAnalog0 = Robot.AnalogRead(2);
    // else        Console.WriteLine(valueAnalog0);

    // if(buttonC) Robot.ChangePwmDutyCycle(0.5);
    // else        Robot.ChangePwmDutyCycle(0.0);
    Robot.ChangePwmDutyCycle(0.5-(Robot.AnalogRead(0)/2000.0));

    //Console.WriteLine($"Sensor left distance ={GetUltrasoneDistance(5)} | Sensor right distance ={GetUltrasoneDistance(16)}");

    await Task.Delay(1500);
}

Robot.Motors(0,0);
Robot.WriteDigitalPin(5,PinValue.Low);
Robot.StopPwm();
Robot.PlayNotes("o4l16ceg>c8");
Console.WriteLine("The loop has ended");

//Console.ReadLine();


static int GetUltrasoneDistance(int pin)
{
    Robot.SetDigitalPinMode(pin, PinMode.Output);
    Robot.WriteDigitalPin(pin, PinValue.Low);
    Robot.Wait(1);
    Robot.WriteDigitalPin(pin, PinValue.High);
    Robot.WaitUs(10);
    Robot.WriteDigitalPin(pin, PinValue.Low);

    Robot.SetDigitalPinMode(pin, PinMode.Input);
    int pulse = Robot.PulseIn(pin, PinValue.High, 50);
    return pulse/29/2;
}

static int[] ReadDht11Data(int DHTPIN)
{
    int[] dht11_dat = new int[5];
    PinValue lastState = PinValue.High;
    byte counter;
    byte j = 0, i;

    Array.Clear(dht11_dat, 0, dht11_dat.Length);

    Robot.SetDigitalPinMode(DHTPIN, PinMode.Output);
    Robot.WriteDigitalPin(DHTPIN, PinValue.Low);
    Robot.Wait(18);
    Robot.WriteDigitalPin(DHTPIN, PinValue.High);
    Robot.WaitUs(40);
    Robot.SetDigitalPinMode(DHTPIN, PinMode.Input);

    for (i = 0; i < 85; i++)
    {
        counter = 0;
        while (Robot.ReadDigitalPin(DHTPIN) == lastState)
        {
            counter++;
            Thread.Sleep(TimeSpan.FromTicks(1));
            if (counter == 255)
            {
                break;
            }
        }
        lastState = Robot.ReadDigitalPin(DHTPIN);

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