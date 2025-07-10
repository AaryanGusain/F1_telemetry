// CarStatusPacket.cs
using System;
using System.Collections.Generic;

namespace F1Parser
{
    public sealed class CarStatus
    {
        public float FuelInTank { get; init; }
        public float FuelRemainingLaps { get; init; }
        public byte ActualTyreCompound { get; init; }
        public byte TyresAgeLaps { get; init; }
    }

    public sealed class CarStatusPacket
    {
        public PacketHeader Header { get; init; }
        public IReadOnlyList<CarStatus> CarStatusData { get; init; } = Array.Empty<CarStatus>();

        public static CarStatusPacket Parse(PacketHeader header, ref LittleEndianReader r)
        {
            var list = new DynList<CarStatus>(UdpSizes.MaxNumCarsInUdpData);

            for (int i = 0; i < UdpSizes.MaxNumCarsInUdpData; i++)
            {
                int structStart = r.Offset;

                // Skip tractionControl, antiLockBrakes, fuelMix, frontBrakeBias, pitLimiterStatus
                r.Skip(5);

                float fuelInTank          = r.ReadFloat();
                float _fuelCapacity       = r.ReadFloat(); // read and discard
                float fuelRemainingLaps   = r.ReadFloat();

                // Skip maxRPM (2), idleRPM (2), maxGears (1), drsAllowed (1), drsActivationDistance (2)
                r.Skip(2 + 2 + 1 + 1 + 2);

                byte actualTyreCompound = r.ReadUInt8();
                r.Skip(1);  // visualTyreCompound
                byte tyresAgeLaps       = r.ReadUInt8();

                int bytesRead = r.Offset - structStart;
                if (bytesRead < UdpSizes.CarStatusDataSize)
                    r.Skip(UdpSizes.CarStatusDataSize - bytesRead);

                list.Add(new CarStatus
                {
                    FuelInTank          = fuelInTank,
                    FuelRemainingLaps   = fuelRemainingLaps,
                    ActualTyreCompound  = actualTyreCompound,
                    TyresAgeLaps        = tyresAgeLaps
                });
            }

            return new CarStatusPacket
            {
                Header = header,
                CarStatusData = list
            };
        }
    }
}
