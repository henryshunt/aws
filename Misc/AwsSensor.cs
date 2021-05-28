﻿namespace Aws.Misc
{
    /// <summary>
    /// Represents the sensors that the AWS can interface with.
    /// </summary>
    internal enum AwsSensor
    {
        AirTemperature,
        RelativeHumidity,
        Bme680,
        Satellite,
        WindSpeed,
        WindDirection,
        SunshineDuration,
        Rainfall
    }
}
