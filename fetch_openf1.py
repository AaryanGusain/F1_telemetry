import requests
import pandas as pd

session_key = 9157  # Bahrain 2023
driver_number = 1   # Max Verstappen

url = f"https://api.openf1.org/v1/car_data?session_key={session_key}&driver_number={driver_number}"

response = requests.get(url)

if response.status_code == 200:
    data = response.json()
    df = pd.DataFrame(data)
    print("✅ Pulled data:", len(df), "rows")
    print(df.head())
    df.to_csv("bahrain2023_max.csv", index=False)
    print("✅ Saved to bahrain2023_max.csv")
else:
    print("❌ Failed:", response.status_code)
