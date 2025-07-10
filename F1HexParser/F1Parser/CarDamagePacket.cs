// CarDamagePacket.cs
using System;
using System.Collections.Generic;

namespace F1Parser
{
    public sealed class CarDamage
    {
        public TyreWearData TyresWear { get; init; } = new();
        public sealed class TyreWearData
        {
            public float FrontLeft  { get; init; }
            public float FrontRight { get; init; }
            public float RearLeft   { get; init; }
            public float RearRight  { get; init; }
        }
    }

    public sealed class CarDamagePacket
    {
        public PacketHeader Header { get; init; }
        public IReadOnlyList<CarDamage> CarDamageData { get; init; } = Array.Empty<CarDamage>();

        public static CarDamagePacket Parse(PacketHeader header, ref LittleEndianReader r)
        {
            var list = new DynList<CarDamage>(UdpSizes.MaxNumCarsInUdpData);

            for (int i = 0; i < UdpSizes.MaxNumCarsInUdpData; i++)
            {
                int structStart = r.Offset;

                float wearFL = r.ReadFloat();
                float wearFR = r.ReadFloat();
                float wearRL = r.ReadFloat();
                float wearRR = r.ReadFloat();

                int bytesRead = r.Offset - structStart;
                if (bytesRead < UdpSizes.CarDamageDataSize)
                    r.Skip(UdpSizes.CarDamageDataSize - bytesRead);

                list.Add(new CarDamage
                {
                    TyresWear = new CarDamage.TyreWearData
                    {
                        FrontLeft  = wearFL,
                        FrontRight = wearFR,
                        RearLeft   = wearRL,
                        RearRight  = wearRR
                    }
                });
            }

            return new CarDamagePacket
            {
                Header = header,
                CarDamageData = list
            };
        }
    }
}