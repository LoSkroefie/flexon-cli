using FlexonCLI;
using System;
using System.Collections.Generic;

namespace FlexonExamples.Advanced
{
    public class CustomTypesExample
    {
        [FlexonType(TypeCode = 0x0A)]
        public class GeoPoint : IFlexonType
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }

            public void Serialize(FlexonWriter writer)
            {
                writer.WriteDouble(Latitude);
                writer.WriteDouble(Longitude);
            }

            public void Deserialize(FlexonReader reader)
            {
                Latitude = reader.ReadDouble();
                Longitude = reader.ReadDouble();
            }

            public override string ToString() =>
                $"({Latitude}, {Longitude})";
        }

        [FlexonType(TypeCode = 0x0B)]
        public class TimeRange : IFlexonType
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }

            public void Serialize(FlexonWriter writer)
            {
                writer.WriteDateTime(Start);
                writer.WriteDateTime(End);
            }

            public void Deserialize(FlexonReader reader)
            {
                Start = reader.ReadDateTime();
                End = reader.ReadDateTime();
            }

            public override string ToString() =>
                $"{Start} to {End}";
        }

        public class LocationData
        {
            public string Name { get; set; }
            public GeoPoint Location { get; set; }
            public TimeRange VisitPeriod { get; set; }
            public Dictionary<string, object> Metadata { get; set; }
        }

        public static void CustomTypeExample()
        {
            // Register custom types
            FlexonConfiguration.RegisterType<GeoPoint>();
            FlexonConfiguration.RegisterType<TimeRange>();

            // Create test data
            var locationData = new LocationData
            {
                Name = "Central Park",
                Location = new GeoPoint
                {
                    Latitude = 40.7829,
                    Longitude = -73.9654
                },
                VisitPeriod = new TimeRange
                {
                    Start = DateTime.Now,
                    End = DateTime.Now.AddHours(2)
                },
                Metadata = new Dictionary<string, object>
                {
                    ["type"] = "park",
                    ["rating"] = 4.5,
                    ["tags"] = new[] { "nature", "recreation" }
                }
            };

            // Initialize serializer
            var serializer = new FlexonSerializer();

            // Serialize
            byte[] binary = serializer.Serialize(locationData);
            Console.WriteLine($"Serialized size: {binary.Length} bytes");

            // Deserialize
            var restored = serializer.Deserialize<LocationData>(binary);
            
            // Verify results
            Console.WriteLine($"Location: {restored.Name}");
            Console.WriteLine($"Coordinates: {restored.Location}");
            Console.WriteLine($"Visit Period: {restored.VisitPeriod}");
            Console.WriteLine("Metadata:");
            foreach (var item in restored.Metadata)
            {
                Console.WriteLine($"  {item.Key}: {item.Value}");
            }
        }

        public static void SchemaValidationExample()
        {
            // Define schema for custom types
            var schema = @"{
                'type': 'object',
                'properties': {
                    'name': { 'type': 'string' },
                    'location': {
                        'type': 'object',
                        'properties': {
                            'latitude': { 'type': 'number' },
                            'longitude': { 'type': 'number' }
                        },
                        'required': ['latitude', 'longitude']
                    },
                    'visitPeriod': {
                        'type': 'object',
                        'properties': {
                            'start': { 'type': 'string', 'format': 'date-time' },
                            'end': { 'type': 'string', 'format': 'date-time' }
                        },
                        'required': ['start', 'end']
                    },
                    'metadata': {
                        'type': 'object',
                        'additionalProperties': true
                    }
                },
                'required': ['name', 'location', 'visitPeriod']
            }";

            // Configure serializer with schema
            var options = new FlexonOptions
            {
                EnableValidation = true,
                Schema = FlexonSchema.FromString(schema)
            };

            var serializer = new FlexonSerializer(options);

            // Create test data
            var locationData = new LocationData
            {
                Name = "Times Square",
                Location = new GeoPoint
                {
                    Latitude = 40.7580,
                    Longitude = -73.9855
                },
                VisitPeriod = new TimeRange
                {
                    Start = DateTime.Now,
                    End = DateTime.Now.AddHours(3)
                },
                Metadata = new Dictionary<string, object>
                {
                    ["type"] = "landmark",
                    ["rating"] = 4.8,
                    ["tags"] = new[] { "tourist", "shopping" }
                }
            };

            try
            {
                // Serialize with validation
                byte[] binary = serializer.Serialize(locationData);
                Console.WriteLine($"Validated serialization size: {binary.Length} bytes");

                // Deserialize with validation
                var restored = serializer.Deserialize<LocationData>(binary);
                Console.WriteLine($"Successfully validated and restored: {restored.Name}");
            }
            catch (FlexonValidationException ex)
            {
                Console.WriteLine($"Validation failed: {ex.Message}");
                foreach (var error in ex.ValidationErrors)
                {
                    Console.WriteLine($"  {error.Path}: {error.Message}");
                }
            }
        }

        public static void Main()
        {
            Console.WriteLine("Custom Type Example:");
            CustomTypeExample();

            Console.WriteLine("\nSchema Validation Example:");
            SchemaValidationExample();
        }
    }
}
