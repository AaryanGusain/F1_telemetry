import json
import struct
import csv

# ---- User Settings ---- #
LOG_FILE = 'F1Telemetry_Jun24_2025_Silverstone.json'
CONFIG_FILE = 'driver_filter_config.json'
OUT_CSV = 'f1_telemetry_selected_drivers.csv'

# ---- Struct Formats ---- #
HEADER_FMT = '<HBBBBBQfIIBB'  # 29 bytes, little-endian
HEADER_SIZE = struct.calcsize(HEADER_FMT)

NUM_CARS = 22

# --- Packet IDs (from enum) ---
PACKET_ID_PARTICIPANTS = 4
PACKET_ID_CAR_TELEMETRY = 6

# --- Participant Struct (2025): name is 32 bytes, UTF-8, null-terminated
PARTICIPANT_FMT = '<BBBBBBBxB32sBBHBBBx4s4s4s4s'  # x for alignment/unknown, 32s for name (max 32), then other fields
PARTICIPANT_SIZE = 48  # You may need to adjust this if names are 48 bytes in your data

# --- Car Telemetry Struct ---
CAR_TELEMETRY_FMT = '<HfffBbHBBH4H4B4BHI4f4B'  # Only the main fields; can add more if needed
CAR_TELEMETRY_SIZE = 55  # Check and adjust this to match your log’s real binary size

# ---- Helper: Convert null-terminated bytes to string ----
def clean_name(b):
    try:
        return b.split(b'\x00', 1)[0].decode('utf-8')
    except:
        return b.decode('utf-8', errors='replace')

def main():
    # --- Load driver config ---
    with open(CONFIG_FILE) as f:
        filter_config = json.load(f)
    wanted_names = set(n.lower() for n in filter_config['drivers'])

    car_idx_to_name = {}
    driver_indices = set()

    # --- First pass: Find car indices for wanted drivers ---
    with open(LOG_FILE) as infile:
        for line in infile:
            packet = json.loads(line)
            packet_hex = packet['packet_hex']
            packet_bytes = bytes.fromhex(packet_hex)
            header = struct.unpack(HEADER_FMT, packet_bytes[:HEADER_SIZE])
            packet_id = header[5]

            if packet_id == PACKET_ID_PARTICIPANTS:
                # The participant struct array starts after header
                # Each participant has a fixed struct, get their names
                for car_idx in range(NUM_CARS):
                    base = HEADER_SIZE + car_idx * PARTICIPANT_SIZE
                    end = base + PARTICIPANT_SIZE
                    p_bytes = packet_bytes[base:end]
                    # Name is at offset 8, for 32 bytes
                    name = clean_name(p_bytes[8:8+32])
                    car_idx_to_name[car_idx] = name
                    if name.lower() in wanted_names:
                        driver_indices.add(car_idx)
                if driver_indices:
                    print("Found driver(s):", {car_idx: car_idx_to_name[car_idx] for car_idx in driver_indices})

    if not driver_indices:
        print("Could not find any drivers matching the names in the config!")
        return

    # --- Second pass: Extract telemetry for those car indices ---
    rows = []
    with open(LOG_FILE) as infile:
        for line in infile:
            packet = json.loads(line)
            packet_hex = packet['packet_hex']
            packet_bytes = bytes.fromhex(packet_hex)
            header = struct.unpack(HEADER_FMT, packet_bytes[:HEADER_SIZE])
            packet_id = header[5]
            session_time = header[7]

            if packet_id == PACKET_ID_CAR_TELEMETRY:
                for car_idx in driver_indices:
                    base = HEADER_SIZE + car_idx * CAR_TELEMETRY_SIZE
                    end = base + CAR_TELEMETRY_SIZE
                    t_bytes = packet_bytes[base:end]
                    if len(t_bytes) < CAR_TELEMETRY_SIZE:
                        continue
                    unpacked = struct.unpack('<HfffBbHBBH4H4B4BHI4f4B', t_bytes)
                    speed = unpacked[0]
                    throttle = unpacked[1]
                    steer = unpacked[2]
                    brake = unpacked[3]
                    gear = unpacked[5]
                    rpm = unpacked[6]
                    # Add more as needed

                    rows.append({
                        'session_time': session_time,
                        'car_idx': car_idx,
                        'driver': car_idx_to_name.get(car_idx, str(car_idx)),
                        'speed': speed,
                        'rpm': rpm,
                        'gear': gear,
                        'throttle': throttle,
                        'brake': brake,
                        'steer': steer
                    })

    # --- Write to CSV ---
    with open(OUT_CSV, 'w', newline='') as csvfile:
        fieldnames = ['session_time', 'car_idx', 'driver', 'speed', 'rpm', 'gear', 'throttle', 'brake', 'steer']
        writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
        writer.writeheader()
        for r in rows:
            writer.writerow(r)

    print(f"Wrote {len(rows)} telemetry samples to {OUT_CSV}")

if __name__ == "__main__":
    main()
