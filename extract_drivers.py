import json
import csv

# 1. Load participant info
participants = {}
with open('your_telemetry.jsonl') as f:
    for line in f:
        packet = json.loads(line)
        if packet['packet_type'] == 'participants':
            for i, p in enumerate(packet['participants']):
                name = p['name'].strip()
                participants[i] = name

# 2. Find car indices for Lando and Aaryan
driver_indices = {idx for idx, name in participants.items() if name in ["Lando Norris", "Aaryan Gusain"]}

# 3. Extract telemetry for those drivers
rows = []
with open('your_telemetry.jsonl') as f:
    for line in f:
        packet = json.loads(line)
        if packet['packet_type'] == 'car_telemetry':
            for idx in driver_indices:
                data = packet['car_telemetry_data'][idx]
                row = {
                    "timestamp": packet['timestamp'],
                    "driver": participants[idx],
                    "speed": data["speed"],
                    "rpm": data["engineRPM"],
                    "gear": data["gear"],
                    "throttle": data["throttle"],
                    "brake": data["brake"],
                    "steer": data["steer"]
                }
                rows.append(row)

# 4. Write to CSV
with open('filtered_telemetry.csv', 'w', newline='') as csvfile:
    fieldnames = ["timestamp", "driver", "speed", "rpm", "gear", "throttle", "brake", "steer"]
    writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(rows)
