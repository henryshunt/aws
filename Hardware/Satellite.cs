using Aws.Routines;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace Aws.Hardware
{
    /// <summary>
    /// Represents a device that allows for sensors to be placed far away from the main system.
    /// </summary>
    internal class Satellite
    {
        /// <summary>
        /// The number of the USB port the device is connected to. This refers to the directories listed in /sys/bus/
        /// usb/devices. Only ports represented by directories of the form "1-1.X:1.0" are supported, where X is the
        /// port number. This means that, on a Raspberry Pi, the device must be connected directly to one of the USB
        /// ports on the board.
        /// </summary>
        private readonly int port;

        /// <summary>
        /// The configuration information for the device
        /// </summary>
        private readonly SatelliteConfiguration config;

        /// <summary>
        /// The serial connection to the device.
        /// </summary>
        private SerialPort device;

        /// <summary>
        /// Initialises a new instance of the <see cref="Satellite"/> class.
        /// </summary>
        /// <param name="port">
        /// The number of the USB port the device is connected to. This refers to the directories listed in /sys/bus/
        /// usb/devices. Only ports represented by directories of the form "1-1.X:1.0" are supported, where X is the
        /// port number. This means that, on a Raspberry Pi, the device must be connected directly to one of the USB
        /// ports on the board.
        /// </param>
        /// <param name="config">
        /// The configuration information for the device.
        /// </param>
        public Satellite(int port, SatelliteConfiguration config)
        {
            this.port = port;
            this.config = config;
        }

        /// <summary>
        /// Opens a connection to the device.
        /// </summary>
        public void Open()
        {
            // Only USB ports directly on the Raspberry Pi board are supported for now
            string portPath = string.Format("/sys/bus/usb/devices/1-1.{0}:1.0", port);

            if (!Directory.Exists(portPath))
                throw new SensorException("Device not connected");

            // The device path can be found inside the above directory
            string[] devicePath = Directory.GetDirectories(portPath, "ttyUSB*");

            if (devicePath.Length == 0)
                throw new SensorException("Device not connected");

            device = new SerialPort("/dev/" + new FileInfo(devicePath[0]).Name, 115200);
            device.Open();

            // Wait for the Arduino to reset after connecting
            Thread.Sleep(2000);

            string response = SendCommand("CONFIG " + config.ToString() + "\n");

            if (response == null)
                throw new SensorException("CONFIG command timed out");
            // Use Contains() as the first transmission sometimes has extra characters
            else if (!response.Contains("OK"))
                throw new SensorException("CONFIG command failed");
        }

        /// <summary>
        /// Closes the connection to the device.
        /// </summary>
        public void Close()
        {
            device.Close();
        }

        /// <summary>
        /// Samples the sensors connected to the device.
        /// </summary>
        /// <returns>
        /// The sampled values.
        /// </returns>
        public SatelliteSample Sample()
        {
            string response = SendCommand("SAMPLE\n");

            if (response == null)
                throw new SensorException("SAMPLE command timed out");
            else if (response == "ERROR")
                throw new SensorException("SAMPLE command failed");

            return JsonConvert.DeserializeObject<SatelliteSample>(response);
        }

        /// <summary>
        /// Sends a command to the device and waits for a response. Times out if no response is received after 100
        /// milliseconds.
        /// </summary>
        /// <param name="command">
        /// The command to send.
        /// </param>
        /// <returns>
        /// The response from the device, otherwise <see langword="null"/> if the command times out.
        /// </returns>
        private string SendCommand(string command)
        {
            device.Write(command);

            string response = "";

            Stopwatch timeout = new Stopwatch();
            timeout.Start();

            while (true)
            {
                if (device.BytesToRead > 0)
                {
                    char readChar = (char)device.ReadChar();

                    if (readChar != '\n')
                        response += readChar;
                    else return response;
                }

                if (timeout.ElapsedMilliseconds >= 100)
                    return null;
            }
        }
    }
}
