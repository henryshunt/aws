using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace Aws.Hardware
{
    /// <summary>
    /// Represents a device that allows for sensors to be placed far away from the main system.
    /// </summary>
    internal class Satellite : IDisposable
    {
        /// <summary>
        /// The number of milliseconds to wait for a response to a command before timing out.
        /// </summary>
        private const int COMMAND_TIMEOUT = 100;

        /// <summary>
        /// The number of the USB port the device is connected to. This refers to the directories listed in /sys/bus/
        /// usb/devices. Only ports represented by directories of the form "1-1.X:1.0" are supported, where X is the
        /// port number. This means that, on a Raspberry Pi, the device must be connected directly to one of the USB
        /// ports on the board.
        /// </summary>
        private readonly int port;

        /// <summary>
        /// The configuration data for the device.
        /// </summary>
        private readonly SatelliteConfiguration config;

        /// <summary>
        /// The connection to the device.
        /// </summary>
        private SerialPort serialPort;

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
        /// The configuration data for the device.
        /// </param>
        public Satellite(int port, SatelliteConfiguration config)
        {
            this.port = port;
            this.config = config;
        }

        /// <summary>
        /// Opens the device.
        /// </summary>
        /// <exception cref="SatelliteException">
        /// Thrown if the device is not connected, communication times out, or configuration fails on the device side.
        /// </exception>
        public void Open()
        {
            // Only USB ports directly on the Raspberry Pi board are supported for now
            string portPath = string.Format("/sys/bus/usb/devices/1-1.{0}:1.0", port);

            if (!Directory.Exists(portPath))
                throw new SatelliteException("Device not connected");

            // The device path can be found inside the above directory
            string[] devicePath = Directory.GetDirectories(portPath, "ttyUSB*");

            if (devicePath.Length == 0)
                throw new SatelliteException("Device not connected");

            serialPort = new SerialPort("/dev/" + new FileInfo(devicePath[0]).Name, 115200);
            serialPort.Open();

            // Wait for the Arduino to reset after connecting
            Thread.Sleep(2000);

            string response = SendCommand("CONFIG " + config.ToJsonString() + "\n");

            if (response == null)
                throw new SatelliteException("CONFIG command timed out");
            // Use Contains() as the first transmission sometimes has extra characters
            else if (!response.Contains("OK"))
                throw new SatelliteException("CONFIG command failed");
        }

        /// <summary>
        /// Closes the device.
        /// </summary>
        public void Close()
        {
            serialPort.Close();
        }

        /// <summary>
        /// Samples the sensors connected to the device.
        /// </summary>
        /// <returns>
        /// The sampled values.
        /// </returns>
        /// <exception cref="SatelliteException">
        /// Thrown if communication times out or sampling fails on the device side.
        /// </exception>
        public SatelliteSample Sample()
        {
            string response = SendCommand("SAMPLE\n");

            if (response == null)
                throw new SatelliteException("SAMPLE command timed out");
            else if (response == "ERROR")
                throw new SatelliteException("SAMPLE command failed");

            return JsonConvert.DeserializeObject<SatelliteSample>(response);
        }

        /// <summary>
        /// Sends a command to the device and waits for a response. Times out if no response is received after
        /// <see cref="COMMAND_TIMEOUT"/> milliseconds.
        /// </summary>
        /// <param name="command">
        /// The command to send.
        /// </param>
        /// <returns>
        /// The response from the device, otherwise <see langword="null"/> if the command times out.
        /// </returns>
        private string SendCommand(string command)
        {
            serialPort.Write(command);
            string response = "";

            Stopwatch timeout = new Stopwatch();
            timeout.Start();

            while (true)
            {
                if (serialPort.BytesToRead > 0)
                {
                    char readChar = (char)serialPort.ReadChar();

                    if (readChar != '\n')
                        response += readChar;
                    else return response;
                }

                if (timeout.ElapsedMilliseconds >= COMMAND_TIMEOUT)
                    return null;
            }
        }

        /// <summary>
        /// Disposes the device.
        /// </summary>
        public void Dispose()
        {
            Close();
            serialPort.Dispose();
        }
    }
}
