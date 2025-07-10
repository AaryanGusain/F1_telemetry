using System;
using System.Collections.Generic;

namespace F1Parser
{
    public sealed class LapData
    {
        public uint CurrentLapTimeInMS { get; init; }
        public float LapDistance { get; init; }
        public uint Sector1TimeInMS { get; init; }
        public uint Sector2TimeInMS { get; init; }
        public uint LastLapTimeInMS { get; init; }
        public byte CarPosition { get; init; }
        public byte CurrentLapNum { get; init; }
        public byte PitStatus { get; init; }
        public byte DriverStatus { get; init; }
        public byte GridPosition { get; init; }
    }

    public sealed class LapDataPacket
    {
        public PacketHeader Header { get; init; }
        public IReadOnlyList<LapData> LapData { get; init; } = Array.Empty<LapData>();

        public static LapDataPacket Parse(PacketHeader header, ref LittleEndianReader r)
        {
            var list = new DynList<LapData>(22);
            for (int i = 0; i < 22; i++)
            {
                int structStart = r.Offset;
                uint lastLap = r.ReadUInt32();
                uint currentLap = r.ReadUInt32();
                ushort sector1MS = r.ReadUInt16();
                byte sector1Min = r.ReadUInt8();
                ushort sector2MS = r.ReadUInt16();
                byte sector2Min = r.ReadUInt8();

                // skip delta fields: deltaToCarInFront (2+1) and deltaToRaceLeader (2+1)
                r.Skip(2 + 1 + 2 + 1);

                float lapDistance = r.ReadFloat();
                // totalDistance, safetyCarDelta
                r.Skip(4 + 4);
                byte carPosition = r.ReadUInt8();
                byte currentLapNum = r.ReadUInt8();
                byte pitStatus = r.ReadUInt8();
                byte numPitStops = r.ReadUInt8();
                byte sector = r.ReadUInt8();
                byte currentLapInvalid = r.ReadUInt8();
                byte penalties = r.ReadUInt8();
                byte totalWarnings = r.ReadUInt8();
                byte cornerCuttingWarnings = r.ReadUInt8();
                byte numUnservedDriveThroughPens = r.ReadUInt8();
                byte numUnservedStopGoPens = r.ReadUInt8();
                byte gridPosition = r.ReadUInt8();
                byte driverStatus = r.ReadUInt8();
                byte resultStatus = r.ReadUInt8();

                // skip remaining variable part dynamically to align to struct size (56 bytes total)
                // no-op placeholder removed
                
                // Build object
                uint sector1TimeInMS = (uint)(sector1Min * 60000 + sector1MS);
                uint sector2TimeInMS = (uint)(sector2Min * 60000 + sector2MS);

                // compute bytes consumed so far for this car struct
                int structConsumed = r.Offset - structStart;
                const int structSize = 57; // bytes per spec derived from packet length
                if (structConsumed < structSize)
                {
                    r.Skip(structSize - structConsumed);
                }

                list.Add(new LapData
                {
                    CurrentLapTimeInMS = currentLap,
                    LapDistance = lapDistance,
                    Sector1TimeInMS = sector1TimeInMS,
                    Sector2TimeInMS = sector2TimeInMS,
                    LastLapTimeInMS = lastLap,
                    CarPosition = carPosition,
                    CurrentLapNum = currentLapNum,
                    PitStatus = pitStatus,
                    DriverStatus = driverStatus,
                    GridPosition = gridPosition
                });
            }
            // Skip extra fields: timeTrial PBCarIdx and RivalCarIdx
            r.Skip(2);
            return new LapDataPacket { Header = header, LapData = list };
        }
    }
} 