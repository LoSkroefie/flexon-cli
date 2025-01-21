import json
import subprocess
import os
from datetime import datetime
import time
import numpy as np
from dataclasses import dataclass, asdict
import pytest
from typing import List, Dict

class FlexonHelper:
    @staticmethod
    def run_command(args):
        try:
            result = subprocess.run(['flexon-cli'] + args.split(), 
                                  capture_output=True, text=True, check=True)
            return result.stdout
        except subprocess.CalledProcessError as e:
            print(f"Error running Flexon command: {e.stderr}")
            raise

@dataclass
class WeatherMeasurement:
    timestamp: str
    temperature: float
    humidity: float
    pressure: float
    wind_speed: float
    wind_direction: str
    precipitation: float
    solar_radiation: float
    battery_voltage: float

@dataclass
class SensorMetadata:
    id: str
    type: str
    location: Dict[str, float]
    calibration_date: str
    firmware_version: str
    maintenance_history: List[Dict[str, str]]

@dataclass
class WeatherStation:
    station_id: str
    name: str
    metadata: SensorMetadata
    measurements: List[WeatherMeasurement]
    status: Dict[str, bool]
    config: Dict[str, str]

def create_sample_data(num_measurements: int = 1000) -> WeatherStation:
    """Create sample weather station data"""
    np.random.seed(42)
    
    measurements = []
    for i in range(num_measurements):
        timestamp = datetime.utcnow().isoformat()
        measurements.append(WeatherMeasurement(
            timestamp=timestamp,
            temperature=np.random.normal(20, 5),
            humidity=np.random.uniform(30, 90),
            pressure=np.random.normal(1013.25, 5),
            wind_speed=np.random.exponential(5),
            wind_direction=np.random.choice(['N', 'NE', 'E', 'SE', 'S', 'SW', 'W', 'NW']),
            precipitation=np.random.exponential(1),
            solar_radiation=np.random.uniform(0, 1000),
            battery_voltage=np.random.normal(12, 0.5)
        ))

    metadata = SensorMetadata(
        id="WS001",
        type="Professional Weather Station",
        location={"latitude": 40.7128, "longitude": -74.0060},
        calibration_date=datetime.utcnow().isoformat(),
        firmware_version="2.1.0",
        maintenance_history=[
            {"date": "2024-01-01", "action": "Calibration"},
            {"date": "2024-01-15", "action": "Battery replacement"}
        ]
    )

    return WeatherStation(
        station_id="NYC_CENTRAL_PARK",
        name="Central Park Weather Station",
        metadata=metadata,
        measurements=measurements,
        status={
            "online": True,
            "battery_ok": True,
            "sensors_ok": True
        },
        config={
            "measurement_interval": "5m",
            "upload_interval": "1h",
            "power_mode": "normal"
        }
    )

def run_benchmark(benchmark, func, *args, **kwargs):
    """Run a benchmark with proper setup and cleanup"""
    result = benchmark(func, *args, **kwargs)
    return result

@pytest.mark.benchmark(
    group="serialization",
    min_rounds=100,
    warmup=True
)
def test_json_serialize(benchmark):
    """Benchmark JSON serialization"""
    data = create_sample_data()
    def serialize():
        return json.dumps(asdict(data))
    
    run_benchmark(benchmark, serialize)

@pytest.mark.benchmark(
    group="serialization",
    min_rounds=100,
    warmup=True
)
def test_flexon_serialize(benchmark):
    """Benchmark Flexon serialization"""
    data = create_sample_data()
    json_file = "weather.json"
    flexon_file = "weather.flexon"
    
    with open(json_file, 'w') as f:
        json.dump(asdict(data), f)
    
    def serialize():
        FlexonHelper.run_command(f"serialize -i {json_file} -o {flexon_file}")
    
    run_benchmark(benchmark, serialize)

@pytest.mark.benchmark(
    group="serialization",
    min_rounds=100,
    warmup=True
)
def test_flexon_serialize_encrypted(benchmark):
    """Benchmark Flexon encrypted serialization"""
    data = create_sample_data()
    json_file = "weather.json"
    encrypted_file = "weather_encrypted.flexon"
    
    with open(json_file, 'w') as f:
        json.dump(asdict(data), f)
    
    def serialize_encrypted():
        FlexonHelper.run_command(f"serialize -i {json_file} -o {encrypted_file} -e benchmark_key")
    
    run_benchmark(benchmark, serialize_encrypted)

def main():
    print("Flexon Benchmarking Example (Python)")
    print("====================================\n")

    print("Running benchmarks...")
    pytest.main([__file__, "-v", "--benchmark-only"])

if __name__ == "__main__":
    main()
