#!/usr/bin/env ruby

require 'json'
require 'benchmark'
require 'time'

class FlexonHelper
  def self.run_command(args)
    system("flexon-cli #{args}")
    raise "Flexon command failed" unless $?.success?
  end
end

def create_test_data
  possible_tags = ['web', 'database', 'cache', 'compute', 'storage']
  records = []

  1000.times do |i|
    records << {
      'id' => "record-#{i}",
      'timestamp' => Time.now.iso8601,
      'value' => rand * 1000,
      'metrics' => {
        'cpu' => rand * 100,
        'memory' => rand * 16384,
        'disk' => rand * 1024
      },
      'tags' => possible_tags.select { rand > 0.5 }
    }
  end

  {
    'id' => "test-#{Time.now.to_i}",
    'name' => 'Benchmark Dataset',
    'description' => 'Large dataset for benchmarking Flexon performance',
    'metadata' => {
      'created_at' => Time.now.iso8601,
      'version' => '1.0',
      'type' => 'benchmark'
    },
    'records' => records
  }
end

def benchmark_json_operations(data, iterations)
  json_ser_time = Benchmark.realtime do
    iterations.times do
      JSON.generate(data)
    end
  end

  json_str = JSON.generate(data)
  json_deser_time = Benchmark.realtime do
    iterations.times do
      JSON.parse(json_str)
    end
  end

  [json_ser_time / iterations, json_deser_time / iterations]
end

def benchmark_flexon_operations(json_file, iterations)
  ser_time = Benchmark.realtime do
    iterations.times do
      FlexonHelper.run_command("serialize -i #{json_file} -o benchmark_test.flexon")
    end
  end

  deser_time = Benchmark.realtime do
    iterations.times do
      FlexonHelper.run_command("deserialize -i benchmark_test.flexon -o benchmark_test.json")
    end
  end

  [ser_time / iterations, deser_time / iterations]
end

puts "Flexon Benchmarking Example (Ruby)"
puts "================================\n"

# Create test data
data = create_test_data
json_file = 'benchmark_data.json'
flexon_file = 'benchmark_data.flexon'
encrypted_file = 'benchmark_encrypted.flexon'

puts "1. Creating test data..."
File.write(json_file, JSON.pretty_generate(data))

# Basic serialization
puts "2. Testing basic serialization..."
FlexonHelper.run_command("serialize -i #{json_file} -o #{flexon_file}")

# Encrypted serialization
puts "3. Testing encrypted serialization..."
FlexonHelper.run_command("serialize -i #{json_file} -o #{encrypted_file} -e benchmarkkey")

# Test different encryption algorithms
puts "\n4. Testing different encryption algorithms..."

# AES-256
puts "Testing AES-256..."
FlexonHelper.run_command("serialize -i #{json_file} -o benchmark_aes.flexon -e benchmarkkey AES256")

# ChaCha20
puts "Testing ChaCha20..."
FlexonHelper.run_command("serialize -i #{json_file} -o benchmark_chacha20.flexon -e benchmarkkey ChaCha20")

# TripleDES
puts "Testing TripleDES..."
FlexonHelper.run_command("serialize -i #{json_file} -o benchmark_tripledes.flexon -e benchmarkkey TripleDES")

# Run benchmarks
puts "\n5. Running benchmarks..."
iterations = 10

puts "\nJSON Operations:"
json_ser_time, json_deser_time = benchmark_json_operations(data, iterations)
puts "Average JSON serialization time: #{json_ser_time} seconds"
puts "Average JSON deserialization time: #{json_deser_time} seconds"

puts "\nFlexon Operations:"
flexon_ser_time, flexon_deser_time = benchmark_flexon_operations(json_file, iterations)
puts "Average Flexon serialization time: #{flexon_ser_time} seconds"
puts "Average Flexon deserialization time: #{flexon_deser_time} seconds"

# Compare file sizes
puts "\nFile size comparison:"
puts "Original JSON: #{File.size(json_file)} bytes"
puts "Flexon: #{File.size(flexon_file)} bytes"
puts "AES-256: #{File.size('benchmark_aes.flexon')} bytes"
puts "ChaCha20: #{File.size('benchmark_chacha20.flexon')} bytes"
puts "TripleDES: #{File.size('benchmark_tripledes.flexon')} bytes"

# Additional benchmarks
puts "\n6. Running detailed benchmarks..."

Benchmark.bm(20) do |x|
  x.report("JSON serialize:") do
    100.times { JSON.generate(data) }
  end

  json_str = JSON.generate(data)
  x.report("JSON deserialize:") do
    100.times { JSON.parse(json_str) }
  end

  x.report("Flexon serialize:") do
    10.times do
      FlexonHelper.run_command("serialize -i #{json_file} -o benchmark_test.flexon")
    end
  end

  x.report("Flexon deserialize:") do
    10.times do
      FlexonHelper.run_command("deserialize -i benchmark_test.flexon -o benchmark_test.json")
    end
  end
end
