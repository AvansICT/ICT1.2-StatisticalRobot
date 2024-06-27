// using System.Device.I2c;

// using var bus = I2cBus.Create(1);
// using var analog = bus.CreateDevice(0x08);

// byte[] receiveBuffer = new byte[2];
// int value = 0;
// while(true) 
// {
//     analog.Write([0x30])
//     analog.WriteRead([0x30], receiveBuffer);

//     value = receiveBuffer[1] << 8 | receiveBuffer[0];
//     Console.WriteLine($"Value: {value}");

//     await Task.Delay(1000);
// }


// var sw = Stopwatch.StartNew();
// while(true) 
// {
//     Thread.Sleep(1000);

//     Robot.SetDigitalPinMode(16, PinMode.Output);
//     Robot.WriteDigitalPin(16, PinValue.Low);
//     Robot.Wait(1);
//     Robot.WriteDigitalPin(16, PinValue.High);
//     Robot.WaitUs(10);
//     Robot.WriteDigitalPin(16, PinValue.Low);

//     Robot.SetDigitalPinMode(16, PinMode.Input);
//     int pulse = Robot.PulseIn(16, PinValue.High, 50);

//     Console.WriteLine($"Pulse: {pulse}us, distance = {pulse / 29 / 2}cm");
// }


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
    else if(leftDistance <= 15 || rightDistance <= 15)
    {
        for(short i = 100; i > 0; i -= 5) {
            Robot.Motors(i, i);
            await Task.Delay(10);
        }
        Robot.Motors(-100,100);
        Robot.Wait(1000);
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



    await Task.Delay(500);
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