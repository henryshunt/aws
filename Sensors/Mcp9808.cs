using System.Device.I2c;

namespace Aws.Sensors
{
    internal class Mcp9808
    {
        private Iot.Device.Mcp9808.Mcp9808 device;

        public void Open()
        {
            device = new Iot.Device.Mcp9808.Mcp9808(I2cDevice.Create(
                new I2cConnectionSettings(1, Iot.Device.Mcp9808.Mcp9808.DefaultI2cAddress)));
        }

        public double Sample()
        {
            return device.Temperature.DegreesCelsius;
        }
    }
}
