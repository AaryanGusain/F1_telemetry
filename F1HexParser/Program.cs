// Program.cs  – strict FrameIdentifier synchronisation
//   • Buffers LapData / CarStatus / CarDamage by (carIdx, FrameId)
//   • Writes a CSV row only when LapData exists for that exact frame
//   • CarStatus / CarDamage are optional (default‑zero if absent)
//   • Processes Race session (SessionType 15) only
//   • No external libraries beyond F1Game.UDP + your existing parser helpers

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using F1Parser;          

#region Plain DTO
internal sealed class TelemetrySample
{
    public long   Timestamp            { get; set; }          // Unix ms
    public string DriverName           { get; set; } = null!;
    public int    Lap                  { get; set; }
    public double LapDistance          { get; set; }
    public uint   CurrentLapTimeInMS   { get; set; }
    public int    Speed                { get; set; }
    public int    EngineRpm            { get; set; }
    public int    Gear                 { get; set; }
    public float  Throttle             { get; set; }
    public float  Brake                { get; set; }
    public float  Steer                { get; set; }
    public uint   Sector1TimeInMS      { get; set; }
    public uint   Sector2TimeInMS      { get; set; }
    public uint   Sector3TimeInMS      { get; set; }
    public uint   LapTimeInMS          { get; set; }
    public int    CurrentSector        { get; set; }
    public float  FuelInTank           { get; set; }
    public float  FuelRemainingLaps    { get; set; }
    public int    ActualTyreCompound   { get; set; }
    public int    TyresAgeLaps         { get; set; }
    public float  TyresWearFL          { get; set; }
    public float  TyresWearFR          { get; set; }
    public float  TyresWearRL          { get; set; }
    public float  TyresWearRR          { get; set; }
    public int    CarPosition          { get; set; }
    public int    GridPosition         { get; set; }
    public int    PitStatus            { get; set; }
    public int    DriverStatus         { get; set; }
    public string TrackName            { get; set; } = null!;
    public int    SessionType          { get; set; }
    public int    Weather              { get; set; }
    public int    TrackTemperature     { get; set; }
    public int    AirTemperature       { get; set; }
    public int    TotalLaps            { get; set; }
    public int    TrackLength          { get; set; }
}
#endregion

internal static class Program
{
    // ─────────────────────────────────────────────────────────────────────────
    //   Per‑car frame buffers
    // ─────────────────────────────────────────────────────────────────────────
    private static readonly Dictionary<byte, Dictionary<uint, dynamic>> LapByFrame  = new();
    private static readonly Dictionary<byte, Dictionary<uint, dynamic>> StatByFrame = new();
    private static readonly Dictionary<byte, Dictionary<uint, dynamic>> DmgByFrame  = new();

    // Session meta (keyed by SessionTime)
    private static readonly SortedDictionary<float, Dictionary<string, object>> SessMeta = new();

    private static void AddFrame<T>(Dictionary<byte, Dictionary<uint, T>> buf,
                                    byte idx, uint frame, T pkt, int cap = 1500)
    {
        if (!buf.TryGetValue(idx, out var map))
        {
            map = new Dictionary<uint, T>();
            buf[idx] = map;
        }

        map[frame] = pkt;
        if (map.Count > cap)
            map.Remove(map.Keys.Min()); // simple FIFO cap
    }

    private static Dictionary<string, object> MetaAt(float sessionTime)
    {
        if (SessMeta.Count == 0) return new();
        var kv = SessMeta.LastOrDefault(k => k.Key <= sessionTime);
        return kv.Value ?? new();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //   Main
    // ─────────────────────────────────────────────────────────────────────────
    private static void Main(string[] args)
    {
        string jsonPath = args.FirstOrDefault() ?? "f1_telemetry_spa_jun24.json";
        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"File not found: {jsonPath}");
            return;
        }

        var wantedDrivers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AARYAN GUSAIN",
            "MAYER",
            "LECLERC"
        };

        var idxToDriver = new Dictionary<byte, string>();
        var samples     = new List<TelemetrySample>();

        foreach (string raw in File.ReadLines(jsonPath))
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            string line = raw.TrimStart('\uFEFF');           // strip BOM
            if (!line.StartsWith('{')) continue;

            using var doc = JsonDocument.Parse(line);
            if (!doc.RootElement.TryGetProperty("packet_hex", out var hexProp)) continue;

            byte[] bytes = Convert.FromHexString(hexProp.GetString()!);
            var    pkt   = bytes.ToPacket();                 // your extension

            // ── Session packet
            if (pkt.PacketType == PacketType.Session &&
                pkt.TryGetSessionDataPacket(out var sess))
            {
                SessMeta[pkt.Header.SessionTime] = new Dictionary<string, object>
                {
                    ["sessionType"]      = (int)sess.SessionType,
                    ["weather"]          = (int)sess.Weather,
                    ["trackId"]          = (int)sess.TrackId,
                    ["trackTemperature"] = (int)sess.TrackTemperature,
                    ["airTemperature"]   = (int)sess.AirTemperature,
                    ["totalLaps"]        = (int)sess.TotalLaps,
                    ["trackLength"]      = (int)sess.TrackLength,
                    ["sector2Start"]     = sess.Sector2LapDistanceStart,
                    ["sector3Start"]     = sess.Sector3LapDistanceStart
                };
                continue;
            }

            // ── Participants packet
            if (pkt.PacketType == PacketType.Participants &&
                pkt.TryGetParticipantsDataPacket(out var parts))
            {
                idxToDriver.Clear();
                byte i = 0;
                foreach (var p in parts.Participants)
                    idxToDriver[i++] = p.Name.ToString().Trim('\0');
                continue;
            }

            // ── Buffer Lap / Status / Damage packets by frame
            switch (pkt.PacketType)
            {
                case PacketType.LapData
                    when pkt.TryGetLapDataPacket(out var lapPkt):
                {
                    byte i = 0; uint f = lapPkt.Header.FrameIdentifier;
                    foreach (var l in lapPkt.LapData)
                        AddFrame(LapByFrame, i++, f, l);
                    continue;
                }
                case PacketType.CarStatus
                    when pkt.TryGetCarStatusDataPacket(out var statPkt):
                {
                    byte i = 0; uint f = statPkt.Header.FrameIdentifier;
                    foreach (var s in statPkt.CarStatusData)
                        AddFrame(StatByFrame, i++, f, s);
                    continue;
                }
                case PacketType.CarDamage
                    when pkt.TryGetCarDamageDataPacket(out var dmgPkt):
                {
                    byte i = 0; uint f = dmgPkt.Header.FrameIdentifier;
                    foreach (var d in dmgPkt.CarDamageData)
                        AddFrame(DmgByFrame, i++, f, d);
                    continue;
                }
            }

            // ── Telemetry packet: strict frame match
            if (pkt.PacketType != PacketType.CarTelemetry ||
                !pkt.TryGetCarTelemetryDataPacket(out var telPkt)) continue;

            var meta = MetaAt(pkt.Header.SessionTime);
            if ((int)meta.GetValueOrDefault("sessionType", 0) != 15) continue; // keep race only

            long unixMs = doc.RootElement.TryGetProperty("timestamp", out var tsProp)
                            ? DateTimeOffset.Parse(tsProp.GetString()!)
                                          .ToUnixTimeMilliseconds()
                            : 0;

            uint frameId = telPkt.Header.FrameIdentifier;
            byte carIdx  = 0;

            foreach (var car in telPkt.CarTelemetryData)
            {
                if (!idxToDriver.TryGetValue(carIdx, out var driver) ||
                    !wantedDrivers.Contains(driver))
                { carIdx++; continue; }

                // ── ensure variables are definitely assigned
                dynamic? stat = null;
                dynamic? dmg  = null;

                // LapData is mandatory
                if (!LapByFrame.TryGetValue(carIdx, out var lapDict) ||
                    !lapDict.TryGetValue(frameId, out var lap))
                { carIdx++; continue; }

                // optional packets
                if (StatByFrame.TryGetValue(carIdx, out var sdict))
                    sdict.TryGetValue(frameId, out stat);

                if (DmgByFrame.TryGetValue(carIdx, out var ddict))
                    ddict.TryGetValue(frameId, out dmg);

                if (car.Speed == 0) { carIdx++; continue; }

                // sector calculations (handle minute+ms split)
                uint s1 = (uint)lap.Sector1TimeMSPart +
                          (uint)lap.Sector1TimeMinutesPart * 60000u;
                uint s2 = (uint)lap.Sector2TimeMSPart +
                          (uint)lap.Sector2TimeMinutesPart * 60000u;
                uint lapTime = (uint)lap.LastLapTimeInMS;
                uint s3 = lapTime > 0 ? lapTime - s1 - s2 : 0;

                // build sample
                samples.Add(new TelemetrySample
                {
                    Timestamp            = unixMs,
                    DriverName           = driver,
                    Lap                  = lap.CurrentLapNum,
                    LapDistance          = lap.LapDistance,
                    CurrentLapTimeInMS   = lap.CurrentLapTimeInMS,
                    Speed                = car.Speed,
                    EngineRpm            = car.EngineRPM,
                    Gear                 = car.Gear,
                    Throttle             = car.Throttle,
                    Brake                = car.Brake,
                    Steer                = car.Steer,
                    Sector1TimeInMS      = s1,
                    Sector2TimeInMS      = s2,
                    Sector3TimeInMS      = s3,
                    LapTimeInMS          = lapTime,
                    CurrentSector        = DetermineSector(lap.LapDistance, meta),
                    FuelInTank           = stat?.FuelInTank ?? 0f,
                    FuelRemainingLaps    = stat?.FuelRemainingLaps ?? 0f,
                    ActualTyreCompound   = stat != null ? (int)stat.ActualTyreCompound : 0,
                    TyresAgeLaps         = stat?.TyresAgeLaps ?? 0,
                    TyresWearFL          = dmg?.TyresWear.FrontLeft ?? 0f,
                    TyresWearFR          = dmg?.TyresWear.FrontRight ?? 0f,
                    TyresWearRL          = dmg?.TyresWear.RearLeft ?? 0f,
                    TyresWearRR          = dmg?.TyresWear.RearRight ?? 0f,
                    CarPosition          = lap.CarPosition,
                    GridPosition         = lap.GridPosition,
                    PitStatus            = (int)lap.PitStatus,
                    DriverStatus         = (int)lap.DriverStatus,
                    TrackName            = GetTrackName((int)meta.GetValueOrDefault("trackId", -1)),
                    SessionType          = (int)meta["sessionType"],
                    Weather              = (int)meta["weather"],
                    TrackTemperature     = (int)meta["trackTemperature"],
                    AirTemperature       = (int)meta["airTemperature"],
                    TotalLaps            = (int)meta["totalLaps"],
                    TrackLength          = (int)meta["trackLength"]
                });

                carIdx++;
            }
        }

        // ── CSV output
        using var sw = new StreamWriter("telemetry_output.csv");
        sw.WriteLine("Timestamp,Driver,Lap,LapDistance,CurrentLapTimeInMS,Speed,EngineRPM,Gear,Throttle,Brake,Steer," +
                     "Sector1TimeInMS,Sector2TimeInMS,Sector3TimeInMS,LapTimeInMS,CurrentSector," +
                     "FuelInTank,FuelRemainingLaps,ActualTyreCompound,TyresAgeLaps,TyresWearFL,TyresWearFR,TyresWearRL,TyresWearRR," +
                     "CarPosition,GridPosition,PitStatus,DriverStatus,TrackName,SessionType,Weather,TrackTemperature," +
                     "AirTemperature,TotalLaps,TrackLength");

        foreach (var s in samples)
        {
            sw.WriteLine(string.Join(',',
                s.Timestamp, s.DriverName, s.Lap,
                s.LapDistance.ToString("F3"), s.CurrentLapTimeInMS,
                s.Speed, s.EngineRpm, s.Gear,
                s.Throttle, s.Brake, s.Steer,
                s.Sector1TimeInMS, s.Sector2TimeInMS, s.Sector3TimeInMS, s.LapTimeInMS, s.CurrentSector,
                s.FuelInTank, s.FuelRemainingLaps, s.ActualTyreCompound, s.TyresAgeLaps,
                s.TyresWearFL, s.TyresWearFR, s.TyresWearRL, s.TyresWearRR,
                s.CarPosition, s.GridPosition, s.PitStatus, s.DriverStatus,
                s.TrackName, s.SessionType, s.Weather, s.TrackTemperature,
                s.AirTemperature, s.TotalLaps, s.TrackLength));
        }

        Console.WriteLine($"Done – wrote {samples.Count} rows to telemetry_output.csv");
    }

    // ───────────────────────────────────────────────────────── helpers ───────
    private static int DetermineSector(double lapDistFraction, Dictionary<string, object> meta)
    {
        float trackLen = Convert.ToSingle(meta.GetValueOrDefault("trackLength", 0));
        if (trackLen == 0) return 0;
        float m = (float)lapDistFraction * trackLen;                  // distance in metres
        float s2 = Convert.ToSingle(meta.GetValueOrDefault("sector2Start", 0));
        float s3 = Convert.ToSingle(meta.GetValueOrDefault("sector3Start", 0));
        return m >= s3 ? 2 : (m >= s2 ? 1 : 0);
    }

    private static string GetTrackName(int id) => id switch
    {
        7  => "Silverstone",
        10 => "Spa",
        // add more track IDs as desired …
        _  => $"Track {id}"
    };
}
