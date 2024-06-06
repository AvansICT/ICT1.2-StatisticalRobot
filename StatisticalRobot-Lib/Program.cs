using System.Device.Gpio;
using System.Device.I2c;
using Avans.StatisticalRobot;

Robot.Motors(0,0);

Robot.SetPinMode(5,PinMode.Output);

ushort batteryData = Robot.ReadBatteryMillivolts();
Console.WriteLine(batteryData);

bool [] buttonData = Robot.ReadButtons();
bool buttonA = buttonData[0];
bool buttonB = buttonData[1];
bool buttonC = buttonData[2];

while(!(buttonA && buttonB)) 
{
    buttonData = Robot.ReadButtons();
    buttonA = buttonData[0];
    buttonB = buttonData[1];
    buttonC = buttonData[2];
    Console.WriteLine(buttonA);
    Console.WriteLine(buttonB);
    Console.WriteLine(buttonC);

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

    if(buttonA) Robot.WritePin(5,PinValue.High);
    else        Robot.WritePin(5,PinValue.Low);

    await Task.Delay(1000);
}
Robot.Motors(0,0);
Robot.WritePin(5,PinValue.Low);
Robot.PlayNotes("o4l16ceg>c8");
Console.WriteLine("The loop has ended, press a key to end the program.");
//Console.ReadLine();