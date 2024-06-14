using System.Device.Gpio;

// warning: Console.Read or Console.ReadKey won't work when remote debugging
// Use Console.ReadLine instead.

Console.WriteLine("Hello World!");

// Blinking LED
var gpio = new GpioController();
gpio.OpenPin(17, PinMode.Output);
gpio.Write(17, PinValue.Low);

bool ledIsOn = false;
while(true) 
{
    gpio.Write(17, ledIsOn ? PinValue.Low : PinValue.High);
    ledIsOn = !ledIsOn;

    await Task.Delay(1000); // Or Thread.Sleep(1000);
}