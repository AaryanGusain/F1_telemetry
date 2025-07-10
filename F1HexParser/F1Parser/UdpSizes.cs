// UdpConstants.cs
// Centralized UDP packet and struct size constants

namespace F1Parser
{
    public static class UdpSizes
    {
        // General
        public const int MaxNumCarsInUdpData    = 22;

        // Struct sizes (bytes) according to the 2024 UDP spec
        public const int ParticipantDataSize     = 60; // ParticipantData: 60 bytes per entry
        public const int LapDataStructSize       = 57; // LapData: 57 bytes per car
        public const int CarStatusDataSize       = 55; // CarStatusData: 55 bytes per car
        public const int CarDamageDataSize       = 42; // CarDamageData: 42 bytes per car
        public const int CarTelemetryDataSize    = 60; // CarTelemetryData: 60 bytes per car

        // Add additional sizes here as you implement other packet types
        // e.g. CarSetupDataSize, SessionDataSize, etc.
    }
}
