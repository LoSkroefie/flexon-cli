#!/usr/bin/env ruby

require 'json'
require 'digest'
require 'base64'
require 'matrix'
require 'time'

class FlexonHelper
  def self.run_command(args)
    system("flexon-cli #{args}")
    raise "Flexon command failed" unless $?.success?
  end
end

def create_embeddings(text, dim)
  # Create deterministic embeddings based on text hash
  hash = Digest::SHA256.digest(text)
  seed = hash.bytes.take(8).each_with_index.sum { |b, i| b << (i * 8) }
  
  # Use seed to generate deterministic random numbers
  rng = Random.new(seed)
  embeddings = Array.new(dim) { rng.rand(-1.0..1.0) }
  
  # Normalize embeddings
  norm = Math.sqrt(embeddings.map { |x| x * x }.sum)
  embeddings.map { |x| x / norm }
end

puts "Flexon AI Data Example (Ruby)"
puts "===========================\n"

# Create AI prompt
metadata = {
  'model' => 'gpt-4',
  'created_at' => Time.now.iso8601,
  'version' => '1.0',
  'language' => 'en',
  'domain' => 'programming',
  'security_level' => 'medium'
}

context = [
  {
    'role' => 'system',
    'content' => 'You are a helpful AI assistant specializing in programming.'
  },
  {
    'role' => 'user',
    'content' => 'How do I implement a binary search tree in Ruby?'
  }
]

prompt_text = 'Explain binary search tree implementation with code examples.'
embeddings = create_embeddings(prompt_text, 384)

security = {
  'fingerprint' => Base64.strict_encode64(Digest::SHA256.digest("#{prompt_text}#{Time.now.iso8601}")),
  'encrypted' => true,
  'access_level' => 'user',
  'audit_trail' => [
    {
      'timestamp' => Time.now.iso8601,
      'action' => 'created',
      'user' => 'system'
    }
  ]
}

prompt = {
  'prompt' => prompt_text,
  'metadata' => metadata,
  'context' => context,
  'embeddings' => embeddings,
  'security' => security
}

# Create training dataset
training_pairs = [
  {
    'input' => 'What is a binary search tree?',
    'output' => 'A binary search tree is a data structure where each node has at most two children...'
  },
  {
    'input' => 'How to insert into BST?',
    'output' => 'To insert into a BST, compare the value with the root and recursively insert into left or right subtree...'
  },
  {
    'input' => 'BST search algorithm?',
    'output' => 'To search in a BST, start at root, compare value, and traverse left or right based on comparison...'
  }
]

dataset_metadata = {
  'created_at' => Time.now.iso8601,
  'version' => '1.0',
  'size' => training_pairs.length,
  'language' => 'en',
  'domain' => 'programming'
}

data = training_pairs.map do |pair|
  {
    'input' => pair['input'],
    'output' => pair['output'],
    'embeddings' => create_embeddings(pair['input'], 384),
    'metrics' => {
      'input_tokens' => pair['input'].length,
      'output_tokens' => pair['output'].length,
      'quality_score' => 0.95,
      'diversity_score' => 0.85
    }
  }
end

training_data = {
  'name' => 'bst-tutor-v1',
  'description' => 'Binary Search Tree tutorial dataset',
  'metadata' => dataset_metadata,
  'data' => data
}

# Save prompt and training data
prompt_file = 'prompt.json'
training_file = 'training.json'
flexon_file = 'ai_data.flexon'
schema_file = '../../../FlexonCLI/AI/Schemas/prompt_schema.json'
training_schema_file = '../../../FlexonCLI/AI/Schemas/training_schema.json'
training_flexon_file = 'training.flexon'

puts "1. Saving AI prompt..."
File.write(prompt_file, JSON.pretty_generate(prompt))

puts "2. Saving training data..."
File.write(training_file, JSON.pretty_generate(training_data))

# Validate and serialize prompt
puts "3. Validating and serializing prompt..."
FlexonHelper.run_command("serialize -i #{prompt_file} -o #{flexon_file} -s #{schema_file} -e aikey ChaCha20")

# Validate and serialize training data
puts "4. Validating and serializing training data..."
FlexonHelper.run_command("serialize -i #{training_file} -o #{training_flexon_file} -s #{training_schema_file}")

# Load and verify
puts "5. Loading AI data..."
Dir.mkdir('ai_output') unless Dir.exist?('ai_output')
FlexonHelper.run_command("deserialize -i #{flexon_file} -o ai_output/prompt_decoded.json -e aikey")

loaded_data = File.read('ai_output/prompt_decoded.json')
loaded_prompt = JSON.parse(loaded_data)

puts "\nVerification:"
puts "Prompt loaded: #{!loaded_prompt['prompt'].empty?}"
puts "Metadata present: #{!loaded_prompt['metadata'].empty?}"
puts "Context count: #{loaded_prompt['context'].length}"
puts "Embeddings size: #{loaded_prompt['embeddings'].length}"

# Test different encryption methods
puts "\n6. Testing different encryption methods..."

# AES-256
puts "\nTesting AES-256..."
FlexonHelper.run_command("serialize -i #{prompt_file} -o ai_aes.flexon -e aikey AES256")

# TripleDES
puts "Testing TripleDES..."
FlexonHelper.run_command("serialize -i #{prompt_file} -o ai_tripledes.flexon -e aikey TripleDES")

puts "\nFile size comparison:"
puts "Original JSON: #{File.size(prompt_file)} bytes"
puts "ChaCha20: #{File.size(flexon_file)} bytes"
puts "AES-256: #{File.size('ai_aes.flexon')} bytes"
puts "TripleDES: #{File.size('ai_tripledes.flexon')} bytes"
