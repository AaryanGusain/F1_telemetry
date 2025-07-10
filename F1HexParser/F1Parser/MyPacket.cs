using System;

namespace F1Parser
{
    public sealed class MyPacket
    {
        public PacketHeader Header { get; }
        public PacketType PacketType => Header.PacketId;

        private readonly object _payload;

        internal MyPacket(PacketHeader header, object payload)
        {
            Header = header;
            _payload = payload;
        }

        public bool TryGetSessionDataPacket(out SessionPacket packet)
        {
            packet = _payload as SessionPacket;
            return packet != null;
        }

        public bool TryGetParticipantsDataPacket(out ParticipantsPacket packet)
        {
            packet = _payload as ParticipantsPacket;
            return packet != null;
        }

        public bool TryGetLapDataPacket(out LapDataPacket packet)
        {
            packet = _payload as LapDataPacket;
            return packet != null;
        }

        public bool TryGetCarTelemetryDataPacket(out CarTelemetryPacket packet)
        {
            packet = _payload as CarTelemetryPacket;
            return packet != null;
        }

        public bool TryGetCarStatusDataPacket(out CarStatusPacket packet)
        {
            packet = _payload as CarStatusPacket;
            return packet != null;
        }

        public bool TryGetCarDamageDataPacket(out CarDamagePacket packet)
        {
            packet = _payload as CarDamagePacket;
            return packet != null;
        }
    }
} 