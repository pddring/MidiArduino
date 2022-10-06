using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MidiArduino
{
    class Program
    {
        static SerialPort port = new SerialPort();
        static bool running = true;
        static int instrumentNumber = 0;
        static int midiOutputDeviceNumber = 0;
        static bool verbose = false;

        static void Main(string[] args)
        {
            // Show midi devices
            Console.WriteLine("Midi output devices:");
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                Console.WriteLine($"Device {i}: {MidiOut.DeviceInfo(i).ProductName}");
            }
            Console.WriteLine();

            string[] portNames = SerialPort.GetPortNames();
            Thread readThread = new Thread(Read);
            

            // show all port names
            Console.WriteLine("All available ports:");
            foreach (string portName in portNames)
            {
                Console.WriteLine(portName);
                port.PortName = portName;
            }

            port.BaudRate = 115200;
            Console.WriteLine($"Selected port: {port.PortName} at {port.BaudRate}bps");
            readThread.Start();
            string input = "";

            while (input != "exit")
            {
                Console.Write("Enter instrument number or press exit to quit: ");
                input = Console.ReadLine();

                // enable verbose mode (to show note on and note off messages)
                if (input == "show")
                {
                    verbose = true;
                }

                // disable verbose mode (to hide note on and note off messages)
                if (input == "hide")
                {
                    verbose = false;
                }
                int.TryParse(input, out instrumentNumber);
            }
            
            running = false;
            readThread.Join();
        }

        private static void Read()
        {
            MidiOut midiOut = new MidiOut(midiOutputDeviceNumber);
            
            port.Open();
            int currentInstrument = instrumentNumber;
            midiOut.SendBuffer(new byte[] { 0xC0, 92 });
            while (running)
            {
                if(instrumentNumber != currentInstrument)
                {
                    midiOut.SendBuffer(new byte[] { 0xC0, (byte)instrumentNumber });
                }

                int b = port.ReadByte();
                
                // detect note down
                if (b == 0x90)
                {
                    byte note = (byte)port.ReadByte();
                    byte velocity = (byte)port.ReadByte();
                    if (verbose)
                    {
                        if (velocity > 0)
                        {
                            Console.WriteLine($"Note On: {note} velocity {velocity}");
                        }
                        else
                        {
                            Console.WriteLine($"Note Off: {note}");
                        }
                    }
                    midiOut.SendBuffer(new byte[] { 0x90, note, velocity });
                }
                
            }
            port.Close();
        }
    }
}
