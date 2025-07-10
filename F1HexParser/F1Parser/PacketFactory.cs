using System;

namespace F1Parser
{
    public static class PacketFactory
    {
        public static MyPacket? Parse(byte[] data)
        {
            var reader = new LittleEndianReader(data);
            var header = PacketHeader.Parse(ref reader);
            object payload = header.PacketId switch
            {
                PacketType.Session => SessionPacket.Parse(header, ref reader),
                PacketType.Participants => ParticipantsPacket.Parse(header, ref reader),
                PacketType.LapData => LapDataPacket.Parse(header, ref reader),
                PacketType.CarTelemetry => CarTelemetryPacket.Parse(header, ref reader),
                PacketType.CarStatus => null!,
                PacketType.CarDamage => null!,
                _ => null!
            };
            payload ??= new object();
            return new MyPacket(header, payload);
        }
    }
} 