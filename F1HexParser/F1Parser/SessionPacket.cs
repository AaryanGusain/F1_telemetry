using System;

namespace F1Parser
{
    public sealed class SessionPacket
    {
        public PacketHeader Header { get; init; }

        public byte Weather { get; init; }
        public int TrackTemperature { get; init; }
        public int AirTemperature { get; init; }
        public byte TotalLaps { get; init; }
        public ushort TrackLength { get; init; }
        public byte SessionType { get; init; }
        public sbyte TrackId { get; init; }
        public float Sector2LapDistanceStart { get; init; }
        public float Sector3LapDistanceStart { get; init; }

        public static SessionPacket Parse(PacketHeader header, ref LittleEndianReader r)
        {
            var weather = r.ReadUInt8();
            var trackTemp = r.ReadInt8();
            var airTemp = r.ReadInt8();
            var totalLaps = r.ReadUInt8();
            var trackLength = r.ReadUInt16();
            var sessionType = r.ReadUInt8();
            var trackId = r.ReadInt8();

            // We have consumed 8 bytes after header so far. We now need to advance until only the last 8 bytes remain
            // (sector2 & sector3 starts). For 2024 spec total payload size is 753-29 = 724 bytes.
            // We have already read 8, so skip (724 - 8 - 8) = 708 bytes to land just before the last two floats.
            r.Skip(708);

            var sector2Start = r.ReadFloat();
            var sector3Start = r.ReadFloat();

            return new SessionPacket
            {
                Header = header,
                Weather = weather,
                TrackTemperature = trackTemp,
                AirTemperature = airTemp,
                TotalLaps = totalLaps,
                TrackLength = trackLength,
                SessionType = sessionType,
                TrackId = trackId,
                Sector2LapDistanceStart = sector2Start,
                Sector3LapDistanceStart = sector3Start
            };
        }
    }
} 