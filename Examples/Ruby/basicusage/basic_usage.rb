#!/usr/bin/env ruby

require 'json'
require 'digest'
require 'base64'
require 'time'

class FlexonHelper
  def self.run_command(args)
    system("flexon-cli #{args}")
    raise "Flexon command failed" unless $?.success?
  end
end

class User
  attr_accessor :name, :age, :email, :interests, :metadata

  def initialize(name, age, email, interests, metadata)
    @name = name
    @age = age
    @email = email
    @interests = interests
    @metadata = metadata
  end

  def to_json(*args)
    {
      name: @name,
      age: @age,
      email: @email,
      interests: @interests,
      metadata: @metadata
    }.to_json(*args)
  end

  def self.from_json(json)
    data = JSON.parse(json)
    new(
      data['name'],
      data['age'],
      data['email'],
      data['interests'],
      data['metadata']
    )
  end
end

puts "Flexon Basic Usage Example (Ruby)"
puts "================================\n"

# Create sample data
metadata = {
  'created_at' => Time.now.iso8601,
  'last_login' => Time.now.iso8601,
  'preferences' => {
    'theme' => 'dark',
    'notifications' => true
  }
}

user = User.new(
  'Alice Smith',
  28,
  'alice@example.com',
  ['programming', 'AI', 'data science'],
  metadata
)

# Save to JSON
json_file = 'user.json'
flexon_file = 'user.flexon'
encrypted_file = 'user_encrypted.flexon'
decrypted_file = 'user_decrypted.json'

puts "1. Creating JSON file..."
File.write(json_file, JSON.pretty_generate(user))

# Basic serialization
puts "2. Converting to Flexon format..."
FlexonHelper.run_command("serialize -i #{json_file} -o #{flexon_file}")

# Encryption
puts "3. Creating encrypted Flexon file..."
FlexonHelper.run_command("serialize -i #{json_file} -o #{encrypted_file} -e mysecretkey")

# Decryption
puts "4. Decrypting Flexon file..."
FlexonHelper.run_command("deserialize -i #{encrypted_file} -o #{decrypted_file} -e mysecretkey")

# Verify results
puts "\nResults:"
json_size = File.size(json_file)
flexon_size = File.size(flexon_file)
encrypted_size = File.size(encrypted_file)

puts "Original JSON size: #{json_size} bytes"
puts "Flexon size: #{flexon_size} bytes"
puts "Encrypted size: #{encrypted_size} bytes"

# Compare contents
original_data = File.read(json_file)
decrypted_data = File.read(decrypted_file)

original_user = User.from_json(original_data)
decrypted_user = User.from_json(decrypted_data)

puts "\nData verification:"
puts "Name matches: #{original_user.name == decrypted_user.name}"
puts "Age matches: #{original_user.age == decrypted_user.age}"
puts "Email matches: #{original_user.email == decrypted_user.email}"
puts "Interests match: #{original_user.interests == decrypted_user.interests}"

# Test different encryption algorithms
puts "\n5. Testing different encryption algorithms..."

# ChaCha20
puts "\nTesting ChaCha20..."
FlexonHelper.run_command("serialize -i #{json_file} -o user_chacha20.flexon -e mysecretkey ChaCha20")

# TripleDES
puts "Testing TripleDES..."
FlexonHelper.run_command("serialize -i #{json_file} -o user_tripledes.flexon -e mysecretkey TripleDES")

# Compare file sizes
puts "\nFile size comparison:"
puts "Original JSON: #{json_size} bytes"
puts "AES-256: #{encrypted_size} bytes"
puts "ChaCha20: #{File.size('user_chacha20.flexon')} bytes"
puts "TripleDES: #{File.size('user_tripledes.flexon')} bytes"
