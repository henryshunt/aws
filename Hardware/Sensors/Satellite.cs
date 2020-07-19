using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;

namespace AWS.Hardware.Sensors
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
                if (!serialPort.StartsWith("/dev/ttyUSB")) continue;

                SerialPort device = new SerialPort(serialPort);

                try
                {
                    device.Open();
                    Thread.Sleep(2000); // Wait for the Arduino to reset after connecting

                    string response = SendCommand(device, "PING\n");
                    if (response != null)
                    {
                        if (response.StartsWith("PING ") && int.Parse(response.Replace("PING ", "")) == id)
                        {
                            foundDevice = true;
                            this.device = device;

                            SendCommand(device, "CONFIG " + configuration.ToString() + "\n");
                            break;
                        }
                    }

                    device.Close();
                }
                catch (Exception ex)
                {
                    device.Close();
                    throw ex;
                }
            }

            if (!foundDevice) throw new Exception();
        }

        public void StartSensors()
        {
            string response = SendCommand(device, "START\n");
            Console.WriteLine(response);
        }

        public Thread SampleSensors()
        {
            Thread thread = new Thread(() =>
            {
                string response = SendCommand(device, "SAMPLE\n").Replace("SAMPLE ", "");

                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.MissingMemberHandling = MissingMemberHandling.Error;

                SatelliteSample sample = JsonConvert.DeserializeObject<SatelliteSample>(response, settings);
                LatestSample = sample;
            });

            thread.Start();
            return thread;
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

        public class SatelliteSample
        {
            [JsonProperty("windSpeed")]
            public double? WindSpeed { get; set; } = null;

            [JsonProperty("windDirection")]
            public double? WindDirection { get; set; } = null;
        }
    }
}
