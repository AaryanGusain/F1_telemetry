import pandas as pd
import matplotlib.pyplot as plt

# Load CSV
df = pd.read_csv('sample_telemetry.csv')

# Preview the first few rows
print(df.head())

# Plot speed vs time
plt.plot(df['TimeStamp'], df['Speed'], label='Speed (km/h)')
plt.xlabel('Time (s)')
plt.ylabel('Speed')
plt.title('Speed vs Time')
plt.grid(True)
plt.legend()
plt.show()
