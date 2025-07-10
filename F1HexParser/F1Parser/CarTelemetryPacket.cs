
// CarTelemetryPacket.cs
using System;
using System.Collections.Generic;

namespace F1Parser
{
    public sealed class CarTelemetry
    {
        public ushort Speed    { get; init; }
        public float  Throttle { get; init; }
        public float  Steer    { get; init; }
        public float  Brake    { get; init; }
        public ushort EngineRPM{ get; init; }
        public sbyte  Gear     { get; init; }
    }

    public sealed class CarTelemetryPacket
    {
        public PacketHeader Header { get; init; }
        public IReadOnlyList<CarTelemetry> CarTelemetryData { get; init; } = Array.Empty<CarTelemetry>();

        public static CarTelemetryPacket Parse(PacketHeader header, ref LittleEndianReader r)
        {
            var list = new DynList<CarTelemetry>(UdpSizes.MaxNumCarsInUdpData);

            for (int i = 0; i < UdpSizes.MaxNumCarsInUdpData; i++)
            {
                int structStart = r.Offset;

                ushort speed     = r.ReadUInt16();
                float  throttle  = r.ReadFloat();
                float  steer     = r.ReadFloat();
                float  brake     = r.ReadFloat();
                r.Skip(1);  // clutch
                sbyte gear       = (sbyte)r.ReadInt8();
                ushort rpm       = r.ReadUInt16();
                r.Skip(1 + 1 + 2);  // drs, revLightsPercent, revLightsBitValue

                int bytesRead = r.Offset - structStart;
                if (bytesRead < UdpSizes.CarTelemetryDataSize)
                    r.Skip(UdpSizes.CarTelemetryDataSize - bytesRead);

                list.Add(new CarTelemetry
                {
                    Speed     = speed,
                    Throttle  = throttle,
                    Steer     = steer,
                    Brake     = brake,
                    EngineRPM = rpm,
                    Gear      = gear
                });
            }

            // Skip MFD panel indices + suggested gear (3 bytes)
            r.Skip(3);

            return new CarTelemetryPacket
            {
                Header = header,
                CarTelemetryData = list
            };
        }
    }
}