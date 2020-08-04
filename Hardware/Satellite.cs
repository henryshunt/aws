using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;

namespace AWS.Hardware
{
    internal class Satellite
    {
        private SerialPort device;
        public SatelliteSample LatestSample { get; private set; }

        public void Initialise(int id, SatelliteConfiguration configuration)
        {
            bool foundDevice = false;
            foreach (string serialPort in SerialPort.GetPortNames())
            {
                if (!serialPort.StartsWith("/dev/ttyUSB"))
                    continue;

                SerialPort device = new SerialPort(serialPort, 115200);

                //try
                //{
                device.Open();
                Thread.Sleep(2000); // Wait for the Arduino to reset after connecting

                string response = SendCommand(device, "PING\n");
                Console.WriteLine("PING: " + response);

                if (response != null && response.Contains("AWS Satellite Device"))
                {
                    response = SendCommand(device, "ID\n");
                    Console.WriteLine("ID: " + response);

                    if (int.Parse(response) == id)
                    {
                        foundDevice = true;
                        this.device = device;

                        response = SendCommand(device, "CONFIG " + configuration.ToString() + "\n");
                        Console.WriteLine("CONFIG: " + response);
                        break;
                    }
                }

                device.Close();
                //}
                //catch (Exception ex)
                //{
                //    device.Close();
                //    throw ex;
                //}
            }

            if (!foundDevice)
                throw new Exception("Satellite device not found");
        }

        public bool Start()
        {
            if (SendCommand(device, "START\n") == "OK")
            {
                Console.WriteLine("started");
                return true;
            }
            else return false;
        }

        public void Sample()
        {
            string response = SendCommand(device, "SAMPLE\n");
            //Console.WriteLine("SAMPLE: " + response);

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.MissingMemberHandling = MissingMemberHandling.Error;

            SatelliteSample sample = JsonConvert.DeserializeObject<SatelliteSample>(response, settings);
            LatestSample = sample;
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

                if (timeout.ElapsedMilliseconds >= 100)
                    return null;
            }

            return response;
        }
    }
}
