import React, { useState } from "react";
import Papa from "papaparse";
import { 
  LineChart, 
  Line, 
  XAxis, 
  YAxis, 
  Tooltip, 
  CartesianGrid, 
  Legend,
  ResponsiveContainer,
  ReferenceLine,
  BarChart,
  Bar
} from "recharts";
import './index.css';
// import TelemetryDashboard from "./components/TelemetryDashboard";

export default function App() {
  const [data, setData] = useState([]);
  const [drivers, setDrivers] = useState([]);
  const [selectedDrivers, setSelectedDrivers] = useState([]);
  const [selectedLap, setSelectedLap] = useState(null);
  const [availableLaps, setAvailableLaps] = useState([]);
  const [chartType, setChartType] = useState('speed');
  const [sessionInfo, setSessionInfo] = useState(null);
  const [selectedSessionType, setSelectedSessionType] = useState(null);
  const [availableSessionTypes, setAvailableSessionTypes] = useState([]);

  // Enhanced chart configuration with new telemetry data
  const chartConfigs = {
    speed: {
      title: "Speed (km/h)",
      dataKey: "Speed",
      color: "#e10600",
      yAxisLabel: "Speed (km/h)",
      maxValue: 350,
      minValue: 0,
      format: v => v
    },
    throttle: {
      title: "Throttle Input (%)",
      dataKey: "Throttle",
      color: "#00ff00",
      yAxisLabel: "Throttle (%)",
      maxValue: 100,
      minValue: 0,
      format: v => (v * 100).toFixed(1)
    },
    brake: {
      title: "Brake Input (%)",
      dataKey: "Brake",
      color: "#ff0000",
      yAxisLabel: "Brake (%)",
      maxValue: 100,
      minValue: 0,
      format: v => (v * 100).toFixed(1)
    },
    steer: {
      title: "Steering Input (%)",
      dataKey: "Steer",
      color: "#ffff00",
      yAxisLabel: "Steering (%)",
      maxValue: 100,
      minValue: -100,
      format: v => (v * 100).toFixed(1)
    },
    lapTime: {
      title: "Current Lap Time (s)",
      dataKey: "CurrentLapTimeInMS",
      color: "#00ffff",
      yAxisLabel: "Time (s)",
      maxValue: 120,
      minValue: 0,
      format: v => (v / 1000).toFixed(3)
    }
  };

  const handleFileUpload = (event) => {
    const file = event.target.files[0];
    if (!file) return;
    Papa.parse(file, {
      header: true,
      dynamicTyping: true,
      complete: (results) => {
        console.log('CSV parsing complete:', {
          totalRows: results.data.length,
          sampleRow: results.data[0]
        });
        
        // Filter rows:
        //  • Valid driver + timestamp
        //  • Exclude rows where Lap === 0 (out-lap/formation)
        //  • Require positive lap distance (> 0) so chart starts after leaving pit
        const filtered = results.data.filter(row => 
          row.Speed >= 0 &&
          row.Driver &&
          row.Timestamp &&
          Number(row.Lap) > 0 &&
          Number(row.LapDistance) > 0 &&
          typeof row.Throttle === 'number' && typeof row.Brake === 'number' && typeof row.Steer === 'number'
        );
        
        console.log('After filtering:', {
          filteredRows: filtered.length,
          sampleFilteredRow: filtered[0]
        });
        
        // Map and transform data for correct charting with new fields
        const processedData = filtered.map(row => ({
          ...row,
          // Calculate average tire wear (0-100 %)
          TireWearAvg: row.TyresWearFL && row.TyresWearFR && row.TyresWearRL && row.TyresWearRR ? 
            (row.TyresWearFL + row.TyresWearFR + row.TyresWearRL + row.TyresWearRR) / 4 : 0,
          // Normalise engine RPM field so chart always finds a value
          EngineRpm: row.EngineRpm !== undefined ? row.EngineRpm : (row.EngineRPM !== undefined ? row.EngineRPM : 0),
          // Convert lap time from ms to seconds for display
          CurrentLapTimeInMS: row.CurrentLapTimeInMS || 0,
          // Format timestamps
          time: new Date(row.Timestamp * 24 * 60 * 60 * 1000).toLocaleTimeString(),
          lapTime: row.Timestamp,
          lapLabel: `Lap ${row.Lap}`
        }));
        
        console.log('After processing:', {
          processedRows: processedData.length,
          sampleProcessedRow: processedData[0]
        });
        
        setData(processedData);
        const uniqueDrivers = [...new Set(processedData.map(row => row.Driver))];
        const uniqueLaps = [...new Set(processedData.map(row => row.Lap))].sort((a, b) => a - b);
        const uniqueSessionTypes = [...new Set(processedData.map(row => row.SessionType))].sort((a, b) => a - b);
        
        setDrivers(uniqueDrivers);
        setAvailableLaps(uniqueLaps);
        setAvailableSessionTypes(uniqueSessionTypes);
        setSelectedDrivers([uniqueDrivers[0]]);
        setSelectedLap(uniqueLaps[0]);
        setSelectedSessionType(uniqueSessionTypes[0]);
        
        console.log('Extracted drivers and laps:', {
          drivers: uniqueDrivers,
          laps: uniqueLaps,
          selectedDriver: uniqueDrivers[0],
          selectedLap: uniqueLaps[0]
        });
        
        // Extract session information from first row
        if (processedData.length > 0) {
          const firstRow = processedData[0];
          setSessionInfo({
            trackName: firstRow.TrackName || "Unknown",
            sessionType: firstRow.SessionType || 0,
            weather: firstRow.Weather || 0,
            trackTemperature: firstRow.TrackTemperature || 0,
            airTemperature: firstRow.AirTemperature || 0,
            totalLaps: firstRow.TotalLaps || 0,
            trackLength: firstRow.TrackLength || 0
          });
        }
      },
    });
  };

  const getFilteredData = () => {
    if (selectedDrivers.length === 0 || !selectedLap || !selectedSessionType) return [];
    let filtered = data.filter(row => 
      selectedDrivers.includes(row.Driver) && 
      row.Lap === selectedLap && 
      row.SessionType === selectedSessionType
    );
    // Sort by lap distance so traces progress from start/finish line (0 m) to chequered flag (~ track length)
    filtered.sort((a, b) => a.LapDistance - b.LapDistance);
    
    // Debug logging
    if (filtered.length > 0) {
      const minLapDistance = Math.min(...filtered.map(d => d.LapDistance));
      const maxLapDistance = Math.max(...filtered.map(d => d.LapDistance));
      const sampleValues = filtered.slice(0, 5).map(d => ({ 
        lapDistance: d.LapDistance, 
        speed: d.Speed,
        lapTime: d.CurrentLapTimeInMS 
      }));
      
      console.log('Filtered data for debugging:', {
        selectedDrivers,
        selectedLap,
        selectedSessionType,
        totalDataPoints: data.length,
        filteredDataPoints: filtered.length,
        lapDistanceRange: {
          min: minLapDistance,
          max: maxLapDistance,
          range: maxLapDistance - minLapDistance,
          minMeters: minLapDistance.toFixed(0),
          maxMeters: maxLapDistance.toFixed(0)
        },
        sampleValues: sampleValues
      });
      
      // Additional debug: check lap distance values
      const uniqueLapDistances = [...new Set(filtered.map(d => d.LapDistance))];
      console.log('Unique LapDistance values (first 10):', uniqueLapDistances.slice(0, 10));
      console.log('Total unique LapDistance values:', uniqueLapDistances.length);
      
      // Show first 5 raw data points
      console.log('First 5 raw data points:', filtered.slice(0, 5).map(d => ({
        driver: d.Driver,
        lap: d.Lap,
        lapDistance: d.LapDistance,
        speed: d.Speed,
        throttle: d.Throttle,
        brake: d.Brake,
        steer: d.Steer,
        lapTime: d.CurrentLapTimeInMS,
        lapTimeSeconds: (d.CurrentLapTimeInMS / 1000).toFixed(3),
        // Show timestamp for potential use
        timestamp: d.Timestamp,
        // Show other potential fields
        engineRpm: d.EngineRpm,
        gear: d.Gear
      })));
      
      // Check what fields have non-zero values
      const firstRow = filtered[0];
      if (firstRow) {
        console.log('Available fields with non-zero values:', {
          CurrentLapTimeInMS: firstRow.CurrentLapTimeInMS,
          LapDistance: firstRow.LapDistance,
          LapTimeInMS: firstRow.CurrentLapTimeInMS,
          Speed: firstRow.Speed,
          Throttle: firstRow.Throttle,
          Brake: firstRow.Brake,
          Steer: firstRow.Steer,
          Timestamp: firstRow.Timestamp
        });
      }
      
      // Check if LapDistance has good range
      if (maxLapDistance - minLapDistance < 1000) {
        console.warn('⚠️ Lap distance range is very small (< 1 second). This might indicate timing issues.');
      } else {
        console.log('✅ Good lap distance range detected:', (maxLapDistance - minLapDistance).toFixed(0), 'meters');
      }
    } else {
      console.log('No filtered data found');
    }
    
    // Down-sample to avoid browser lock-ups but keep full-lap coverage
    const MAX_POINTS = 3000; // per driver per lap
    const grouped = {};
    filtered.forEach(r => {
      if (!grouped[r.Driver]) grouped[r.Driver] = [];
      grouped[r.Driver].push(r);
    });
    const sampled = Object.values(grouped).flatMap(list => {
      if (list.length <= MAX_POINTS) return list;
      const step = Math.ceil(list.length / MAX_POINTS);
      return list.filter((_, idx) => idx % step === 0);
    });
    return sampled;
  };

  const getDriverColor = (driverName, index) => {
    const colors = ['#e10600', '#00ff00', '#0066ff', '#ffff00', '#ff00ff', '#00ffff'];
    return colors[index % colors.length];
  };

  const getSessionTypeName = (sessionType) => {
    const types = {
      0: "Unknown", 1: "Practice 1", 2: "Practice 2", 3: "Practice 3", 4: "Short Practice",
      5: "Qualifying 1", 6: "Qualifying 2", 7: "Qualifying 3", 8: "Short Qualifying",
      9: "One Shot Qualifying", 10: "Race", 11: "Race 2", 12: "Time Trial"
    };
    return types[sessionType] || `Session ${sessionType}`;
  };

  const getWeatherName = (weather) => {
    const weathers = {
      0: "Clear", 1: "Light Cloud", 2: "Overcast", 3: "Light Rain", 4: "Heavy Rain", 5: "Storm"
    };
    return weathers[weather] || `Weather ${weather}`;
  };

  const getTireCompoundName = (compound) => {
    const compounds = {
      0: "C0", 1: "C1", 2: "C2", 3: "C3", 4: "C4", 5: "C5", 6: "Intermediate", 7: "Wet"
    };
    return compounds[compound] || `Compound ${compound}`;
  };

  const renderChart = () => {
    const filteredData = getFilteredData();
    if (filteredData.length === 0) return null;
    const config = chartConfigs[chartType];
    // dynamic y-axis range based on visible data (adds 5% padding)
    const yVals = filteredData.map(d => d[config.dataKey]);
    const yMin = Math.min(...yVals);
    const yMax = Math.max(...yVals);
    const pad = (yMax - yMin) * 0.05 || 1;
    return (
      <LineChart data={filteredData} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="#333" />
        <XAxis
          dataKey="LapDistance"
          type="number"
          domain={['dataMin', 'dataMax']}
          allowDataOverflow={false}
          tick={{ fontSize: 10, fill: "#bbb" }}
          stroke="#555"
          tickFormatter={v => `${v.toFixed(0)} m`}
        />
        <YAxis
          tick={{ fontSize: 12, fill: "#bbb" }}
          stroke="#555"
          domain={[Math.max(config.minValue ?? yMin - pad, yMin - pad), Math.min(config.maxValue ?? yMax + pad, yMax + pad)]}
        />
        <Tooltip
          contentStyle={{ backgroundColor: "#18181b", borderColor: config.color, color: "#fff" }}
          labelStyle={{ color: config.color }}
          labelFormatter={v => `Lap Dist: ${v.toFixed(0)} m`}
          formatter={config.format}
        />
        <Legend />
        {selectedDrivers.map((driver, index) => {
          const driverData = filteredData.filter(d => d.Driver === driver);
          return (
            <Line
              key={driver}
              type="monotone"
              dataKey={config.dataKey}
              stroke={getDriverColor(driver, index)}
              strokeWidth={2}
              dot={false}
              name={`${driver} - ${config.title}`}
              data={driverData}
            />
          );
        })}
      </LineChart>
    );
  };

  const renderDriverStats = () => {
    const filteredData = getFilteredData();
    if (filteredData.length === 0) return null;
    const stats = selectedDrivers.map(driver => {
      const driverData = filteredData.filter(d => d.Driver === driver);
      const maxSpeed = Math.max(...driverData.map(d => d.Speed));
      const avgSpeed = driverData.reduce((sum, d) => sum + d.Speed, 0) / driverData.length;
      const avgThrottle = driverData.reduce((sum, d) => sum + (d.Throttle * 100), 0) / driverData.length;
      const avgBrake = driverData.reduce((sum, d) => sum + (d.Brake * 100), 0) / driverData.length;
      const avgSteer = driverData.reduce((sum, d) => sum + (d.Steer * 100), 0) / driverData.length;
      const carPosition = driverData.length > 0 ? driverData[driverData.length - 1].CarPosition : 0;
      // Calculate lap time (take the last available CurrentLapTimeInMS value for the lap)
      const lapTimeMs = driverData.length > 0 ? driverData[driverData.length - 1].CurrentLapTimeInMS : 0;
      const formatTime = (ms) => {
        if (!ms || ms === 0) return '-';
        const minutes = Math.floor(ms / 60000);
        const seconds = Math.floor((ms % 60000) / 1000);
        const milliseconds = Math.floor(ms % 1000);
        return `${minutes}:${seconds.toString().padStart(2, '0')}.${milliseconds.toString().padStart(3, '0')}`;
      };
      const lapTimeFormatted = formatTime(lapTimeMs);
      
      return { driver, maxSpeed, avgSpeed, avgThrottle, avgBrake, avgSteer, carPosition, lapTimeFormatted };
    });
    
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-6">
        {stats.map((stat, index) => (
          <div key={stat.driver} className="bg-zinc-800 rounded-lg p-4 border border-zinc-700">
            <h3 className="text-lg font-bold text-white mb-2" style={{ color: getDriverColor(stat.driver, index) }}>
              {stat.driver} (P{stat.carPosition})
            </h3>
            <div className="space-y-2 text-sm">
              <div className="flex justify-between"><span className="text-gray-400">Max Speed:</span><span className="text-white font-mono">{stat.maxSpeed} km/h</span></div>
              <div className="flex justify-between"><span className="text-gray-400">Avg Speed:</span><span className="text-white font-mono">{stat.avgSpeed.toFixed(1)} km/h</span></div>
              <div className="flex justify-between"><span className="text-gray-400">Avg Throttle:</span><span className="text-white font-mono">{stat.avgThrottle.toFixed(1)}%</span></div>
              <div className="flex justify-between"><span className="text-gray-400">Avg Brake:</span><span className="text-white font-mono">{stat.avgBrake.toFixed(1)}%</span></div>
              <div className="flex justify-between"><span className="text-gray-400">Avg Steer:</span><span className="text-white font-mono">{stat.avgSteer.toFixed(1)}%</span></div>
              <div className="flex justify-between"><span className="text-gray-400">Lap Time:</span><span className="text-white font-mono">{stat.lapTimeFormatted}</span></div>
            </div>
          </div>
        ))}
      </div>
    );
  };

  const renderSessionInfo = () => {
    if (!sessionInfo) return null;
    return (
      <div className="bg-zinc-900 rounded-lg p-4 mb-4 border border-zinc-800">
        <h3 className="text-white font-semibold mb-3">Session Information</h3>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
          <div><span className="text-gray-400">Track:</span><span className="text-white ml-2">{sessionInfo.trackName}</span></div>
          <div><span className="text-gray-400">Session:</span><span className="text-white ml-2">{getSessionTypeName(sessionInfo.sessionType)}</span></div>
          <div><span className="text-gray-400">Weather:</span><span className="text-white ml-2">{getWeatherName(sessionInfo.weather)}</span></div>
          <div><span className="text-gray-400">Track Temp:</span><span className="text-white ml-2">{sessionInfo.trackTemperature}°C</span></div>
          <div><span className="text-gray-400">Air Temp:</span><span className="text-white ml-2">{sessionInfo.airTemperature}°C</span></div>
          <div><span className="text-gray-400">Total Laps:</span><span className="text-white ml-2">{sessionInfo.totalLaps}</span></div>
          <div><span className="text-gray-400">Track Length:</span><span className="text-white ml-2">{sessionInfo.trackLength}m</span></div>
        </div>
      </div>
    );
  };

  return (
    <div className="min-h-screen bg-black flex flex-col items-center py-10 px-4 font-sans">
      {/* HEADER */}
      <header className="mb-10">
        <h1 className="text-4xl md:text-5xl font-extrabold text-white tracking-tight flex items-center gap-2">
          <span className="text-red-600">F1</span>
          <span className="text-white">Telemetry</span>
          <span className="text-zinc-400">Analyzer</span>
        </h1>
        <div className="mt-2 border-t-2 border-red-600 w-32 mx-auto rounded" />
      </header>
      
      {/* UPLOAD */}
      <div className="flex flex-col items-center mb-8">
        <label htmlFor="csv-upload" className="block text-gray-200 text-sm font-medium mb-2">Upload your F1 telemetry CSV</label>
        <input id="csv-upload" type="file" accept=".csv" onChange={handleFileUpload} className="block w-full text-sm text-gray-100 file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-red-600 file:text-white hover:file:bg-red-700 transition duration-150" />
      </div>
      
      {/* SESSION INFO */}
      {sessionInfo && renderSessionInfo()}
      
      {/* CONTROLS */}
      {data.length > 0 && (
        <div className="w-full max-w-6xl mb-6">
          {/* Driver Selection */}
          <div className="bg-zinc-900 rounded-lg p-4 mb-4 border border-zinc-800">
            <h3 className="text-white font-semibold mb-3">Driver Selection</h3>
            <div className="flex flex-wrap gap-2">
              {drivers.map((driver) => (
                <button key={driver} onClick={() => {
                  if (selectedDrivers.includes(driver)) {
                    setSelectedDrivers(selectedDrivers.filter(d => d !== driver));
                  } else {
                    setSelectedDrivers([...selectedDrivers, driver]);
                  }
                }} className={`px-3 py-1 rounded-full text-sm font-medium transition-colors ${selectedDrivers.includes(driver) ? 'bg-red-600 text-white' : 'bg-zinc-700 text-gray-300 hover:bg-zinc-600'}`}>{driver}</button>
              ))}
            </div>
          </div>
          
          {/* Lap Selection */}
          <div className="bg-zinc-900 rounded-lg p-4 mb-4 border border-zinc-800">
            <h3 className="text-white font-semibold mb-3">Lap Selection</h3>
            <div className="flex flex-wrap gap-2">
              {availableLaps.slice(0, 20).map((lap) => (
                <button key={lap} onClick={() => setSelectedLap(lap)} className={`px-3 py-1 rounded-full text-sm font-medium transition-colors ${selectedLap === lap ? 'bg-blue-600 text-white' : 'bg-zinc-700 text-gray-300 hover:bg-zinc-600'}`}>Lap {lap}</button>
              ))}
              {availableLaps.length > 20 && (<span className="text-gray-400 text-sm px-3 py-1">+{availableLaps.length - 20} more</span>)}
            </div>
          </div>
          
          {/* Session Type Selection */}
          <div className="bg-zinc-900 rounded-lg p-4 mb-4 border border-zinc-800">
            <h3 className="text-white font-semibold mb-3">Session Type</h3>
            <div className="flex flex-wrap gap-2">
              {availableSessionTypes.map((sessionType) => (
                <button key={sessionType} onClick={() => setSelectedSessionType(sessionType)} className={`px-3 py-1 rounded-full text-sm font-medium transition-colors ${selectedSessionType === sessionType ? 'bg-purple-600 text-white' : 'bg-zinc-700 text-gray-300 hover:bg-zinc-600'}`}>{getSessionTypeName(sessionType)}</button>
              ))}
            </div>
          </div>
          
          {/* Chart Type Selection */}
          <div className="bg-zinc-900 rounded-lg p-4 mb-4 border border-zinc-800">
            <h3 className="text-white font-semibold mb-3">Chart Type</h3>
            <div className="flex flex-wrap gap-2">
              {Object.entries(chartConfigs).map(([key, config]) => (
                <button key={key} onClick={() => setChartType(key)} className={`px-3 py-1 rounded-full text-sm font-medium transition-colors ${chartType === key ? 'bg-green-600 text-white' : 'bg-zinc-700 text-gray-300 hover:bg-zinc-600'}`}>{config.title}</button>
              ))}
            </div>
          </div>
        </div>
      )}
      
      {/* DRIVER STATS */}
      {data.length > 0 && renderDriverStats()}
      
      {/* CHART ZONE */}
      <div className="bg-zinc-900 rounded-2xl shadow-xl p-6 w-full max-w-6xl mb-6 border border-zinc-800">
        {data.length === 0 ? (
          <div className="text-gray-400 text-center py-24">
            <p>No telemetry loaded.</p>
            <p className="text-xs mt-2 text-zinc-500">Upload your F1 telemetry CSV to get started.</p>
          </div>
        ) : (
          <div>
            <h3 className="text-white font-semibold mb-4 text-center">
              {chartConfigs[chartType].title} - {selectedDrivers.join(' vs ')} - Lap {selectedLap} - {getSessionTypeName(selectedSessionType)}
            </h3>
            <p className="text-gray-400 text-sm text-center mb-4">
              X-axis: Lap Distance (m) | Y-axis: {chartConfigs[chartType].title}
            </p>
            <ResponsiveContainer width="100%" height={400}>{renderChart()}</ResponsiveContainer>
          </div>
        )}
      </div>
      
      {/* FOOTER / CREDITS */}
      <footer className="mt-10 text-zinc-600 text-xs tracking-wide text-center">
        Built by <span className="text-red-500 font-bold">Aaryan</span> · F1 Telemetry Analysis · Open source on <a href="https://github.com/" className="underline text-red-600 hover:text-white transition">GitHub</a>
      </footer>
    </div>
  );
}


