#!/usr/bin/env ruby

require 'json'
require 'rmagick'
require 'time'

class FlexonHelper
  def self.run_command(args)
    system("flexon-cli #{args}")
    raise "Flexon command failed" unless $?.success?
  end
end

class Vector2
  attr_accessor :x, :y

  def initialize(x, y)
    @x = x
    @y = y
  end

  def to_json(*args)
    { x: @x, y: @y }.to_json(*args)
  end
end

class PlayerState
  attr_accessor :name, :level, :health, :mana, :position, :inventory

  def initialize(name, level, health, mana, position, inventory)
    @name = name
    @level = level
    @health = health
    @mana = mana
    @position = position
    @inventory = inventory
  end

  def to_json(*args)
    {
      name: @name,
      level: @level,
      health: @health,
      mana: @mana,
      position: @position,
      inventory: @inventory
    }.to_json(*args)
  end
end

class WorldState
  attr_accessor :name, :current_zone, :discovered_locations, :weather, :time_of_day

  def initialize(name, current_zone, discovered_locations, weather, time_of_day)
    @name = name
    @current_zone = current_zone
    @discovered_locations = discovered_locations
    @weather = weather
    @time_of_day = time_of_day
  end

  def to_json(*args)
    {
      name: @name,
      current_zone: @current_zone,
      discovered_locations: @discovered_locations,
      weather: @weather,
      time_of_day: @time_of_day
    }.to_json(*args)
  end
end

class GameData
  attr_accessor :version, :save_time, :player, :world, :quests, :achievements

  def initialize(version, save_time, player, world, quests, achievements)
    @version = version
    @save_time = save_time
    @player = player
    @world = world
    @quests = quests
    @achievements = achievements
  end

  def to_json(*args)
    {
      version: @version,
      save_time: @save_time,
      player: @player,
      world: @world,
      quests: @quests,
      achievements: @achievements
    }.to_json(*args)
  end
end

def create_game_screenshot
  canvas = Magick::Image.new(800, 600) { self.background_color = 'navy' }

  # Draw border
  border = Magick::Draw.new
  border.stroke('white')
  border.stroke_width(2)
  border.fill_opacity(0)
  border.rectangle(50, 50, 750, 550)
  border.draw(canvas)

  # Draw player position
  player = Magick::Draw.new
  player.fill('red')
  player.circle(400, 345, 405, 350)
  player.draw(canvas)

  canvas.write('screenshot.png')
end

puts "Flexon Game State Example (Ruby)"
puts "==============================\n"

# Create game state
game_state = GameData.new(
  '1.0.0',
  Time.now.iso8601,
  PlayerState.new(
    'DragonSlayer',
    35,
    100,
    85,
    Vector2.new(123.45, 678.90),
    [
      {
        'id' => 'sword_123',
        'name' => 'Excalibur',
        'damage' => 50
      },
      {
        'id' => 'shield_456',
        'name' => 'Dragon Shield',
        'defense' => 30
      },
      {
        'id' => 'potion_789',
        'name' => 'Health Potion',
        'healing' => 100
      }
    ]
  ),
  WorldState.new(
    'Mythical Realm',
    'Dragon\'s Lair',
    ['Starting Village', 'Dark Forest', 'Crystal Cave', 'Dragon\'s Lair'],
    'stormy',
    'night'
  ),
  {
    'active' => [
      {
        'id' => 'quest_123',
        'name' => 'Slay the Dragon',
        'progress' => 0.75
      },
      {
        'id' => 'quest_456',
        'name' => 'Find the Sacred Sword',
        'progress' => 0.33
      }
    ],
    'completed' => [
      'Rescue the Villagers',
      'Defeat the Dark Knight',
      'Find the Ancient Map'
    ]
  },
  {
    'FirstBattle' => true,
    'DragonSlayer' => false,
    'LegendaryWeapon' => true,
    'ExplorerSupreme' => false
  }
)

state_file = 'gamestate.json'
save_file = 'savegame.flexon'

# Create screenshot
puts "1. Creating game screenshot..."
create_game_screenshot

# Save game state
puts "2. Saving game state..."
File.write(state_file, JSON.pretty_generate(game_state))

# Create save package with state and screenshot
puts "3. Creating save package..."
FlexonHelper.run_command("serialize -i #{state_file} -i screenshot.png -o #{save_file} -e savegamekey")

# Load save package
puts "4. Loading save package..."
Dir.mkdir('loaded_save') unless Dir.exist?('loaded_save')
FlexonHelper.run_command("deserialize -i #{save_file} -o loaded_save -e savegamekey")

# Verify loaded state
puts "\nVerifying save data:"
loaded_data = File.read('loaded_save/gamestate.json')
loaded_state = JSON.parse(loaded_data)

puts "Player name: #{loaded_state['player']['name']}"
puts "Level: #{loaded_state['player']['level']}"
puts "Current zone: #{loaded_state['world']['current_zone']}"
puts "Active quests: #{loaded_state['quests']['active'].length}"
puts "Screenshot recovered: #{File.exist?('loaded_save/screenshot.png')}"

# Test different encryption algorithms
puts "\n5. Testing different encryption algorithms..."

# ChaCha20
puts "\nTesting ChaCha20..."
FlexonHelper.run_command("serialize -i #{state_file} -o savegame_chacha20.flexon -e savegamekey ChaCha20")

# TripleDES
puts "Testing TripleDES..."
FlexonHelper.run_command("serialize -i #{state_file} -o savegame_tripledes.flexon -e savegamekey TripleDES")

# Compare file sizes
puts "\nFile size comparison:"
puts "Original JSON: #{File.size(state_file)} bytes"
puts "AES-256: #{File.size(save_file)} bytes"
puts "ChaCha20: #{File.size('savegame_chacha20.flexon')} bytes"
puts "TripleDES: #{File.size('savegame_tripledes.flexon')} bytes"
