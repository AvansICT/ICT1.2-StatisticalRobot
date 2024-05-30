using System.Device.I2c;
using Avans.StatisticalRobot;

//I2cDevice romi = Robot.CreateI2cDevice(20);


Robot.Motors(0,0);

ushort batteryData = Robot.ReadBatteryMillivolts();
Console.WriteLine(batteryData);

bool [] buttonData = Robot.ReadButtons();
bool buttonA = buttonData[0];
bool buttonB = buttonData[1];
bool buttonC = buttonData[2];

while(!buttonA) 
{
    buttonData = Robot.ReadButtons();
    buttonA = buttonData[0];
    buttonB = buttonData[1];
    buttonC = buttonData[2];
    Console.WriteLine(buttonA);
    Console.WriteLine(buttonB);
    Console.WriteLine(buttonC);

    for(short i = 0; i < 400; i += 5) {
        Robot.Motors(i, i);
        await Task.Delay(10);
    }

    for(short i = 400; i > 0; i -= 5) {
        Robot.Motors(i, i);
        await Task.Delay(10);
    }
    Robot.Motors(0,0);

    await Task.Delay(1000);
}

Console.WriteLine("Hello World!");
Console.ReadLine();