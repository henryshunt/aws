using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;

namespace AWS.Hardware.Sensors
{
    internal class Satellite
    {
        private SerialPort Device;

        public void Initialise(int id, SatelliteConfiguration configuration)
        {
            foreach (string serialPort in SerialPort.GetPortNames())
            {
                if (!serialPort.StartsWith("/dev/ttyUSB")) continue; // Ignore the internal port
                Console.WriteLine(serialPort);
                SerialPort port = new SerialPort(serialPort);

                //try
                //{
                port.Open();
                Thread.Sleep(1800);
                string response = SendCommand(port, "PING\n");

                if (response != null)
                {
                    if (response.StartsWith("PING ") && int.Parse(response.Replace("PING ", "")) == id)
                    {
                        Console.WriteLine("found");
                        Device = port;
                        Console.WriteLine(SendCommand(port, "CONFIG " + configuration.ToString() + "\n"));
                        break;
                    }
                }

                port.Close();
                //}
                //catch
                //{
                //    port.Close();
                //    continue;
                //}
            }
        }

        public void StartSensors()
        {
            Console.WriteLine("test2");
            string response = SendCommand(Device, "START\n");
            Console.WriteLine(response);
        }

        public void ReadSensors()
        {
            new Thread(() =>
            {

            }).Start();
            string response = SendCommand(Device, "SAMPLE\n");
            //Console.WriteLine(response);
        }

        private string SendCommand(SerialPort serialPort, string command)
        {
            serialPort.Write(command);

            string response = "";
            bool responseEnded = false;

            Stopwatch timeout = new Stopwatch();
            timeout.Start();

            while (!responseEnded)
            {
                if (serialPort.BytesToRead > 0)
                {
                    char readChar = (char)serialPort.ReadChar();

                    if (readChar != '\n')
                        response += readChar;
                    else responseEnded = true;
                }

                if (timeout.ElapsedMilliseconds >= 500)
                    return null;
            }

            return response;
        }

        public class SatelliteConfiguration
        {
            public bool AirTemperatureEnabled { get; set; } = false;
            public bool WindSpeedEnabled { get; set; } = false;
            public int WindSpeedPin { get; set; }
            public bool WindDirectionEnabled { get; set; } = false;
            public int WindDirectionPin { get; set; }
            public bool RainfallEnabled { get; set; } = false;
            public int RainfallPin { get; set; }


            public override string ToString()
            {
                string result = "";

                result += "{\"windSpeedEnabled\":" + WindSpeedEnabled.ToString().ToLower();
                if (WindSpeedEnabled)
                    result += ",\"windSpeedPin\":" + WindSpeedPin.ToString().ToLower();

                result += ",\"windDirectionEnabled\":" + WindDirectionEnabled.ToString().ToLower();
                if (WindDirectionEnabled)
                    result += ",\"windDirectionPin\":" + WindDirectionPin.ToString().ToLower();

                result += "}";
                return result;
            }
        }
    }
}
