# F1 Telemetry Analysis System

A comprehensive F1 telemetry analysis system that processes F1 game UDP packets and provides interactive visualizations.

## 🏎️ Features

- **F1 UDP Packet Parser** (C#) - Extracts telemetry data from F1 game packets
- **Interactive Web Dashboard** (React) - Visualize and compare driver telemetry
- **Lap-by-Lap Analysis** - Filter and analyze data by specific laps
- **Multi-Driver Comparison** - Compare multiple drivers simultaneously
- **Multiple Chart Types** - Speed, Throttle, Brake, Steering, RPM, Gear selection

## 🚀 Quick Start

### 1. Generate Telemetry Data

```bash
cd F1HexParser
dotnet run
```

This will:
- Parse `F1Telemetry_Jun24_2025_Silverstone.json` 
- Extract telemetry for drivers: Norris, Aaryan Gusain, Leclerc, Russell, Mayer
- Generate `telemetry_output.csv` with lap-integrated data

### 2. Launch Visualization Dashboard

```bash
cd f125-telemetry-app
npm install
npm run dev
```

Visit `http://localhost:5174` to access the dashboard.

### 3. Upload and Analyze

1. Upload the generated `telemetry_output.csv` file
2. Select drivers to compare
3. Choose specific laps or view all laps
4. Switch between different telemetry metrics

## 📊 Data Structure

The CSV contains the following columns:
- `Timestamp` - OADate timestamp
- `Driver` - Driver name
- `Lap` - Current lap number
- `Speed` - Speed in km/h
- `EngineRPM` - Engine RPM
- `Gear` - Current gear
- `Throttle` - Throttle input (0-1)
- `Brake` - Brake input (0-1)
- `Steer` - Steering input (-1 to 1)

## 🛠️ Technical Details

### F1HexParser (C#)
- Uses F1Game.UDP library to parse hex-encoded UDP packets
- Integrates `CarTelemetryDataPacket` with `LapDataPacket` for lap information
- Filters for specific drivers of interest
- Outputs clean CSV format

### React Dashboard
- Built with React 19 + Vite
- Uses Recharts for data visualization
- Tailwind CSS for styling
- PapaParse for CSV parsing
- Responsive design with driver comparison features

## 🎯 Key Features

### Driver Selection
- Multi-driver selection with color coding
- Real-time driver statistics
- Individual driver performance metrics

### Lap Filtering
- View all laps or specific lap numbers
- Lap-by-lap performance comparison
- Automatic lap range detection

### Chart Types
- **Speed** - Vehicle speed over time
- **Throttle** - Throttle input percentage
- **Brake** - Brake input percentage  
- **Steering** - Steering wheel position
- **Engine RPM** - Engine revolutions per minute
- **Gear Selection** - Current gear (bar chart)

### Performance Optimizations
- Data point limiting to prevent frontend crashes
- Efficient filtering and data management
- Responsive chart rendering

## 📁 Project Structure

```
f1_telemetry/
├── F1HexParser/                 # C# UDP packet parser
│   ├── Program.cs              # Main parsing logic
│   └── telemetry_output.csv    # Generated telemetry data
├── f125-telemetry-app/         # React visualization dashboard
│   ├── src/App.jsx            # Main dashboard component
│   ├── package.json           # Dependencies
│   └── telemetry_output.csv   # Sample data
├── extract_drivers.py         # Python utilities
├── fetch_openf1.py           # OpenF1 data fetching
└── README.md                 # This file
```

## 🔧 Configuration

### Adding New Drivers
Edit `F1HexParser/Program.cs` and modify the `driversOfInterest` set:

```csharp
var driversOfInterest = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "Norris",
    "Aaryan Gusain", 
    "LECLERC",
    "RUSSELL",
    "MAYER",
    "NEW_DRIVER"  // Add new drivers here
};
```

### Changing Input File
Modify the `path` variable in `Program.cs`:

```csharp
string path = "your_telemetry_file.json";
```

## 🎨 Customization

### Chart Colors
Modify the `getDriverColor` function in `App.jsx`:

```javascript
const colors = ['#e10600', '#00ff00', '#0066ff', '#ffff00', '#ff00ff', '#00ffff'];
```

### Chart Configuration
Add new chart types in the `chartConfigs` object:

```javascript
const chartConfigs = {
  // ... existing configs
  newMetric: {
    title: "New Metric",
    dataKey: "NewMetric",
    color: "#ff6600",
    yAxisLabel: "Units",
    maxValue: 100
  }
};
```

## 🚀 Future Enhancements

- [ ] Track map visualization
- [ ] Sector timing analysis
- [ ] Tire wear tracking
- [ ] Weather data integration
- [ ] Real-time data streaming
- [ ] Export to various formats
- [ ] Advanced statistical analysis

## 📝 License

Built by Aaryan - F1 Telemetry Analysis System

---

**Note**: This system is designed for educational and analysis purposes. Make sure to comply with F1 game terms of service when collecting telemetry data.
