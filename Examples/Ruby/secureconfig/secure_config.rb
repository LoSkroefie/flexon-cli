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

class DatabaseConfig
  attr_accessor :host, :port, :database, :username, :password, :options

  def initialize(host, port, database, username, password, options)
    @host = host
    @port = port
    @database = database
    @username = username
    @password = password
    @options = options
  end

  def to_json(*args)
    {
      host: @host,
      port: @port,
      database: @database,
      username: @username,
      password: @password,
      options: @options
    }.to_json(*args)
  end
end

class APIConfig
  attr_accessor :endpoint, :api_key, :timeout, :headers, :allowed_methods

  def initialize(endpoint, api_key, timeout, headers, allowed_methods)
    @endpoint = endpoint
    @api_key = api_key
    @timeout = timeout
    @headers = headers
    @allowed_methods = allowed_methods
  end

  def to_json(*args)
    {
      endpoint: @endpoint,
      api_key: @api_key,
      timeout: @timeout,
      headers: @headers,
      allowed_methods: @allowed_methods
    }.to_json(*args)
  end
end

class CacheConfig
  attr_accessor :provider, :ttl, :settings

  def initialize(provider, ttl, settings)
    @provider = provider
    @ttl = ttl
    @settings = settings
  end

  def to_json(*args)
    {
      provider: @provider,
      ttl: @ttl,
      settings: @settings
    }.to_json(*args)
  end
end

class SecurityConfig
  attr_accessor :authentication, :authorization, :encryption

  def initialize(authentication, authorization, encryption)
    @authentication = authentication
    @authorization = authorization
    @encryption = encryption
  end

  def to_json(*args)
    {
      authentication: @authentication,
      authorization: @authorization,
      encryption: @encryption
    }.to_json(*args)
  end
end

class AppConfig
  attr_accessor :version, :environment, :database, :api, :cache, :security, :metadata

  def initialize(version, environment, database, api, cache, security, metadata)
    @version = version
    @environment = environment
    @database = database
    @api = api
    @cache = cache
    @security = security
    @metadata = metadata
  end

  def to_json(*args)
    {
      version: @version,
      environment: @environment,
      database: @database,
      api: @api,
      cache: @cache,
      security: @security,
      metadata: @metadata
    }.to_json(*args)
  end
end

puts "Flexon Secure Config Example (Ruby)"
puts "================================\n"

# Create configuration
config = AppConfig.new(
  '1.0.0',
  'production',
  DatabaseConfig.new(
    'localhost',
    5432,
    'myapp',
    'admin',
    'supersecret123',
    {
      'maxConnections' => 100,
      'timeout' => 30,
      'ssl' => true
    }
  ),
  APIConfig.new(
    'https://api.example.com/v1',
    'sk_live_abcdef123456789',
    60,
    {
      'User-Agent' => 'MyApp/1.0',
      'Accept' => 'application/json'
    },
    ['GET', 'POST', 'PUT', 'DELETE']
  ),
  CacheConfig.new(
    'redis',
    3600,
    {
      'host' => 'localhost',
      'port' => 6379,
      'maxMemory' => '2gb'
    }
  ),
  SecurityConfig.new(
    {
      'provider' => 'oauth2',
      'clientId' => 'myapp_client',
      'clientSecret' => 'client_secret_xyz',
      'tokenUrl' => 'https://auth.example.com/token'
    },
    {
      'type' => 'rbac',
      'defaultRole' => 'user',
      'adminRoles' => ['admin', 'superadmin']
    },
    {
      'algorithm' => 'AES256',
      'keySize' => 256,
      'provider' => 'default'
    }
  ),
  {
    'created_at' => Time.now.iso8601,
    'updated_at' => Time.now.iso8601,
    'created_by' => 'system',
    'fingerprint' => Base64.strict_encode64(
      Digest::SHA256.digest("1.0.0production#{Time.now.iso8601}")
    )
  }
)

config_file = 'config.json'
encrypted_file = 'config.flexon'
schema_file = '../../../FlexonCLI/Config/Schemas/config_schema.json'

# Save configuration
puts "1. Saving configuration..."
File.write(config_file, JSON.pretty_generate(config))

# Encrypt with different algorithms
puts "2. Testing encryption algorithms..."

# AES-256
puts "\nUsing AES-256..."
FlexonHelper.run_command("serialize -i #{config_file} -o config_aes.flexon -e configkey AES256")

# ChaCha20
puts "\nUsing ChaCha20..."
FlexonHelper.run_command("serialize -i #{config_file} -o config_chacha20.flexon -e configkey ChaCha20")

# TripleDES
puts "\nUsing TripleDES..."
FlexonHelper.run_command("serialize -i #{config_file} -o config_tripledes.flexon -e configkey TripleDES")

# Validate and encrypt with schema
puts "\n3. Validating and encrypting with schema..."
FlexonHelper.run_command("serialize -i #{config_file} -o #{encrypted_file} -s #{schema_file} -e configkey")

# Load and verify
puts "4. Loading configuration..."
FlexonHelper.run_command("deserialize -i #{encrypted_file} -o config_decoded.json -e configkey")

loaded_data = File.read('config_decoded.json')
loaded_config = JSON.parse(loaded_data)

puts "\nVerification:"
puts "Version: #{loaded_config['version']}"
puts "Environment: #{loaded_config['environment']}"
puts "Database host: #{loaded_config['database']['host']}"
puts "API endpoint: #{loaded_config['api']['endpoint']}"
puts "Cache provider: #{loaded_config['cache']['provider']}"
puts "Auth provider: #{loaded_config['security']['authentication']['provider']}"

# Compare file sizes
puts "\nFile size comparison:"
puts "Original JSON: #{File.size(config_file)} bytes"
puts "AES-256: #{File.size('config_aes.flexon')} bytes"
puts "ChaCha20: #{File.size('config_chacha20.flexon')} bytes"
puts "TripleDES: #{File.size('config_tripledes.flexon')} bytes"
