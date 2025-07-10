using System;
using System.Collections.Generic;

namespace F1Parser
{
    public sealed class Participant
    {
        public string Name { get; init; } = string.Empty;
    }

    public sealed class ParticipantsPacket
    {
        public PacketHeader Header { get; init; }
        public byte NumActiveCars { get; init; }
        public IReadOnlyList<Participant> Participants { get; init; } = Array.Empty<Participant>();

        public static ParticipantsPacket Parse(PacketHeader header, ref LittleEndianReader r)
        {
            byte numActive = r.ReadUInt8();
            var list = new DynList<Participant>(22);
            for (int i = 0; i < 22; i++)
            {
                if (r.Remaining < 57) break;
                // Skip aiControlled, driverId, networkId, teamId, myTeam, raceNumber, nationality (7 bytes)
                r.Skip(7);
                string name = r.ReadFixedString(48);
                // skip yourTelemetry and showOnlineNames (2 bytes)
                r.Skip(2);
                list.Add(new Participant { Name = name });
            }
            return new ParticipantsPacket { Header = header, NumActiveCars = numActive, Participants = list };
        }
    }
} 