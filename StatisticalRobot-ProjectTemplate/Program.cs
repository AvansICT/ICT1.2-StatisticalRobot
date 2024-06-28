using System.Device.Gpio;
using Avans.StatisticalRobot;

// warning: Console.Read or Console.ReadKey won't work when debugging
// Use Console.ReadLine instead.

Console.WriteLine("Hello World!");

Robot.SetDigitalPinMode(17, PinMode.Output);
Robot.WriteDigitalPin(17, PinValue.Low /* or false or 0 */);

bool ledIsOn = false;
while(true) 
{
    Robot.WriteDigitalPin(17, !ledIsOn);
    ledIsOn = !ledIsOn;

    Console.Write("The LED is ");
    if(ledIsOn) 
    {
        Console.WriteLine("On!");
    }
    else
    {
        Console.WriteLine("Off!");
    }

    Robot.Wait(1000);
}