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


using System.Device.Gpio;
using Avans.StatisticalRobot;

Robot.Motors(0,0);

Robot.SetDigitalPinMode(5,PinMode.Output);

ushort batteryData = Robot.ReadBatteryMillivolts();
Console.WriteLine(batteryData);

bool [] buttonData = Robot.ReadButtons();
bool buttonA = buttonData[0];
bool buttonB = buttonData[1];
bool buttonC = buttonData[2];

Robot.SetPwmPin(400,0.5);
Robot.StartPwm();

Thread.Sleep(2000);

while(!(buttonA && buttonB)) 
{
    buttonData = Robot.ReadButtons();
    buttonA = buttonData[0];
    buttonB = buttonData[1];
    buttonC = buttonData[2];

    Console.WriteLine(Robot.AnalogRead(2));
    Thread.Sleep(250);
    Console.WriteLine(Robot.AnalogRead(4));

    if(buttonB&&buttonC)
    {
        for(short i = 0; i < 400; i += 5) {
            Robot.Motors(i, i);
            await Task.Delay(10);
        }

        for(short i = 400; i > 0; i -= 5) {
            Robot.Motors(i, i);
            await Task.Delay(10);
        }
        Robot.Motors(0,0);
    }
    await Task.Delay(500);

    // if(buttonA) Robot.WriteDigitalPin(5,PinValue.High);
    // else        Robot.WriteDigitalPin(5,PinValue.Low);

    // if(buttonB) valueAnalog0 = Robot.AnalogRead(2);
    // else        Console.WriteLine(valueAnalog0);

    // if(buttonC) Robot.ChangePwmDutyCycle(0.5);
    // else        Robot.ChangePwmDutyCycle(0.0);
    //Robot.ChangePwmDutyCycle(0.5-(Robot.AnalogRead(0)/2000));

    await Task.Delay(50);
}
Robot.Motors(0,0);
Robot.WriteDigitalPin(5,PinValue.Low);
Robot.StopPwm();
Robot.PlayNotes("o4l16ceg>c8");
Console.WriteLine("The loop has ended");
//Console.ReadLine();