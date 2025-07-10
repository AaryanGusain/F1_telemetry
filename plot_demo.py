import pandas as pd
import matplotlib.pyplot as plt

df = pd.read_csv("bahrain2023_max.csv")

# Fix: Convert 'date' column to datetime
df['date'] = pd.to_datetime(df['date'])
df = df.sort_values('date')

# Optional: Filter out rows with no speed data
df = df[df['speed'] > 0]

plt.plot(df['date'], df['speed'], label='Speed (km/h)')
plt.xlabel('Time')
plt.ylabel('Speed')
plt.title("Max Verstappen - Bahrain 2023 Speed vs Time")
plt.grid(True)
plt.legend()
plt.tight_layout()
plt.show()
