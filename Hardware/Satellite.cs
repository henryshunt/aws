using Aws.Routines;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;

namespace Aws.Hardware
{
    internal class Satellite
    {
        private readonly int id;
        private readonly SatelliteConfiguration config;
        private SerialPort device;

        public Satellite(int id, SatelliteConfiguration config)
        {
            this.id = id;
            this.config = config;
        }

        public void Open()
        {
            foreach (string serialPort in SerialPort.GetPortNames())
            {
                if (!serialPort.StartsWith("/dev/ttyUSB"))
                    continue;

                SerialPort device = null;

                try
                {
                    device = new SerialPort(serialPort, 115200);
                    device.Open();
                }
                catch { continue; }

                Thread.Sleep(2000); // Wait for the Arduino to reset after connecting

                string response = SendCommand(device, "PING\n");

                // We use Contains() because the first transmission from the device sometimes has
                // extra characters at the ends
                if (response != null && response.Contains("AWS Satellite Device"))
                {
                    response = SendCommand(device, "ID\n");

                    try
                    {
                        if (int.Parse(response) == id)
                        {
                            response = SendCommand(device, "CONFIG " + config.ToString() + "\n");

                            if (response == "OK")
                            {
                                this.device = device;
                                break;
                            }
                        }
                    }
                    catch { }
                }

                device.Close();
            }

            if (device == null)
                throw new SensorException("Satellite not found");
        }

        public bool Start()
        {
            return SendCommand(device, "START\n") == "OK";
        }

        public SatelliteSample Sample()
        {
            string response = SendCommand(device, "SAMPLE\n");

            if (response == null || response == "ERROR")
                throw new SensorException("Error sampling satellite");

            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.MissingMemberHandling = MissingMemberHandling.Error;
                return JsonConvert.DeserializeObject<SatelliteSample>(response, settings);
            }
            catch
            {
                throw new SensorException("Error sampling satellite");
            }
        }

        private string SendCommand(SerialPort serialPort, string command)
        {
            try
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
            catch { return null; }
        }
    }
}
