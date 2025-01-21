const fs = require('fs').promises;
const { execSync } = require('child_process');
const Benchmark = require('benchmark');
const crypto = require('crypto');

class FlexonHelper {
    static runCommand(args) {
        try {
            return execSync(`flexon-cli ${args}`, { encoding: 'utf8' });
        } catch (error) {
            console.error(`Error running Flexon command: ${error.stderr}`);
            throw error;
        }
    }
}

class WeatherMeasurement {
    constructor() {
        this.timestamp = new Date().toISOString();
        this.temperature = normalRandom(20, 5);
        this.humidity = Math.random() * 60 + 30;
        this.pressure = normalRandom(1013.25, 5);
        this.wind_speed = exponentialRandom(5);
        this.wind_direction = ['N', 'NE', 'E', 'SE', 'S', 'SW', 'W', 'NW'][Math.floor(Math.random() * 8)];
        this.precipitation = exponentialRandom(1);
        this.solar_radiation = Math.random() * 1000;
        this.battery_voltage = normalRandom(12, 0.5);
    }
}

class SensorMetadata {
    constructor() {
        this.id = "WS001";
        this.type = "Professional Weather Station";
        this.location = { latitude: 40.7128, longitude: -74.0060 };
        this.calibration_date = new Date().toISOString();
        this.firmware_version = "2.1.0";
        this.maintenance_history = [
            { date: "2024-01-01", action: "Calibration" },
            { date: "2024-01-15", action: "Battery replacement" }
        ];
    }
}

class WeatherStation {
    constructor(numMeasurements = 1000) {
        this.station_id = "NYC_CENTRAL_PARK";
        this.name = "Central Park Weather Station";
        this.metadata = new SensorMetadata();
        this.measurements = Array.from(
            { length: numMeasurements }, 
            () => new WeatherMeasurement()
        );
        this.status = {
            online: true,
            battery_ok: true,
            sensors_ok: true
        };
        this.config = {
            measurement_interval: "5m",
            upload_interval: "1h",
            power_mode: "normal"
        };
    }
}

// Helper functions for random number generation
function normalRandom(mean, stdDev) {
    const u1 = Math.random();
    const u2 = Math.random();
    const z0 = Math.sqrt(-2.0 * Math.log(u1)) * Math.cos(2.0 * Math.PI * u2);
    return mean + z0 * stdDev;
}

function exponentialRandom(lambda) {
    return -Math.log(1.0 - Math.random()) / lambda;
}

async function runBenchmarks() {
    const suite = new Benchmark.Suite;
    const data = new WeatherStation(1000);
    const jsonFile = "weather.json";
    const flexonFile = "weather.flexon";
    const encryptedFile = "weather_encrypted.flexon";

    // Write initial data
    await fs.writeFile(jsonFile, JSON.stringify(data, null, 2));

    // Add tests
    suite.add('JSON.stringify', () => {
        JSON.stringify(data);
    })
    .add('JSON.parse', async () => {
        const content = await fs.readFile(jsonFile, 'utf8');
        JSON.parse(content);
    })
    .add('Flexon serialize', () => {
        FlexonHelper.runCommand(`serialize -i ${jsonFile} -o ${flexonFile}`);
    })
    .add('Flexon serialize (encrypted)', () => {
        FlexonHelper.runCommand(`serialize -i ${jsonFile} -o ${encryptedFile} -e benchmark_key`);
    })
    .add('Flexon deserialize', () => {
        FlexonHelper.runCommand(`deserialize -i ${flexonFile} -o weather_decoded.json`);
    })
    .add('Flexon deserialize (encrypted)', () => {
        FlexonHelper.runCommand(`deserialize -i ${encryptedFile} -o weather_decrypted.json -e benchmark_key`);
    })
    .on('cycle', (event) => {
        console.log(String(event.target));
    })
    .on('complete', function() {
        console.log('Fastest method is ' + this.filter('fastest').map('name'));
    });

    // Run async
    return new Promise((resolve) => {
        suite.run({ 'async': true, resolve });
    });
}

async function main() {
    console.log("Flexon Benchmarking Example (JavaScript)");
    console.log("=======================================\n");

    console.log("Running benchmarks...\n");
    await runBenchmarks();

    // Compare file sizes
    const data = new WeatherStation(1000);
    const jsonFile = "weather.json";
    const flexonFile = "weather.flexon";
    
    await fs.writeFile(jsonFile, JSON.stringify(data, null, 2));
    FlexonHelper.runCommand(`serialize -i ${jsonFile} -o ${flexonFile}`);
    
    // Test different encryption algorithms
    FlexonHelper.runCommand(`serialize -i ${jsonFile} -o weather_aes.flexon -e benchmark_key AES256`);
    FlexonHelper.runCommand(`serialize -i ${jsonFile} -o weather_chacha20.flexon -e benchmark_key ChaCha20`);
    FlexonHelper.runCommand(`serialize -i ${jsonFile} -o weather_tripledes.flexon -e benchmark_key TripleDES`);

    const stats = await Promise.all([
        fs.stat(jsonFile),
        fs.stat(flexonFile),
        fs.stat('weather_aes.flexon'),
        fs.stat('weather_chacha20.flexon'),
        fs.stat('weather_tripledes.flexon')
    ]);

    console.log("\nFile size comparison:");
    console.log(`Original JSON: ${stats[0].size} bytes`);
    console.log(`Flexon: ${stats[1].size} bytes`);
    console.log(`AES-256: ${stats[2].size} bytes`);
    console.log(`ChaCha20: ${stats[3].size} bytes`);
    console.log(`TripleDES: ${stats[4].size} bytes`);
}

main().catch(console.error);
