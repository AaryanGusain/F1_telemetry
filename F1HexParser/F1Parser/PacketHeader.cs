using System;

namespace F1Parser
{
    public readonly struct PacketHeader
    {
        public ushort PacketFormat { get; }
        public byte GameYear { get; }
        public byte GameMajorVersion { get; }
        public byte GameMinorVersion { get; }
        public byte PacketVersion { get; }
        public PacketType PacketId { get; }
        public ulong SessionUID { get; }
        public float SessionTime { get; }
        public uint FrameIdentifier { get; }
        public uint OverallFrameIdentifier { get; }
        public byte PlayerCarIndex { get; }
        public byte SecondaryPlayerCarIndex { get; }

        public PacketHeader(ushort packetFormat, byte gameYear, byte gameMajorVersion, byte gameMinorVersion,
            byte packetVersion, PacketType packetId, ulong sessionUID, float sessionTime, uint frameIdentifier,
            uint overallFrameIdentifier, byte playerCarIndex, byte secondaryPlayerCarIndex)
        {
            PacketFormat = packetFormat;
            GameYear = gameYear;
            GameMajorVersion = gameMajorVersion;
            GameMinorVersion = gameMinorVersion;
            PacketVersion = packetVersion;
            PacketId = packetId;
            SessionUID = sessionUID;
            SessionTime = sessionTime;
            FrameIdentifier = frameIdentifier;
            OverallFrameIdentifier = overallFrameIdentifier;
            PlayerCarIndex = playerCarIndex;
            SecondaryPlayerCarIndex = secondaryPlayerCarIndex;
        }

        public static PacketHeader Parse(ref LittleEndianReader r)
        {
            var packetFormat = r.ReadUInt16();
            var gameYear = r.ReadUInt8();
            var gameMajor = r.ReadUInt8();
            var gameMinor = r.ReadUInt8();
            var packetVersion = r.ReadUInt8();
            var packetId = (PacketType)r.ReadUInt8();
            var sessionUID = r.ReadUInt64();
            var sessionTime = r.ReadFloat();
            var frameId = r.ReadUInt32();
            var overallFrameId = r.ReadUInt32();
            var playerCarIdx = r.ReadUInt8();
            var secondaryPlayerIdx = r.ReadUInt8();

            return new PacketHeader(packetFormat, gameYear, gameMajor, gameMinor, packetVersion, packetId,
                sessionUID, sessionTime, frameId, overallFrameId, playerCarIdx, secondaryPlayerIdx);
        }
    }
} 