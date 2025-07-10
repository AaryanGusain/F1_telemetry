namespace F1Parser
{
    public static class PacketExtensions
    {
        public static MyPacket ToPacket(this byte[] data)
        {
            return PacketFactory.Parse(data)!; // assume not null, caller checks PacketType switch
        }
    }
} 