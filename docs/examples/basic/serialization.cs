using FlexonCLI;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FlexonExamples.Basic
{
    public class SerializationExample
    {
        public class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public DateTime BirthDate { get; set; }
            public Guid Id { get; set; }
        }

        public static void BasicSerialization()
        {
            // Create test data
            var person = new Person
            {
                Name = "John Doe",
                Age = 30,
                BirthDate = new DateTime(1993, 5, 15),
                Id = Guid.NewGuid()
            };

            // Initialize serializer
            var serializer = new FlexonSerializer();

            // Serialize to binary
            byte[] binary = serializer.Serialize(person);
            Console.WriteLine($"Serialized size: {binary.Length} bytes");

            // Deserialize from binary
            var restored = serializer.Deserialize<Person>(binary);
            Console.WriteLine($"Restored: {restored.Name}, Age: {restored.Age}");
        }

        public static async Task StreamingSerialization()
        {
            var person = new Person
            {
                Name = "Jane Smith",
                Age = 25,
                BirthDate = new DateTime(1998, 8, 20),
                Id = Guid.NewGuid()
            };

            // Serialize to file
            using (var file = File.Create("person.flexon"))
            {
                var serializer = new FlexonSerializer();
                await serializer.SerializeAsync(person, file);
            }

            // Read from file
            using (var file = File.OpenRead("person.flexon"))
            {
                var serializer = new FlexonSerializer();
                var restored = await serializer.DeserializeAsync<Person>(file);
                Console.WriteLine($"Restored from file: {restored.Name}");
            }
        }

        public static void CompressedSerialization()
        {
            var person = new Person
            {
                Name = "Bob Johnson",
                Age = 40,
                BirthDate = new DateTime(1983, 3, 10),
                Id = Guid.NewGuid()
            };

            // Configure compression
            var options = new FlexonOptions
            {
                EnableCompression = true,
                CompressionLevel = System.IO.Compression.CompressionLevel.Optimal
            };

            var serializer = new FlexonSerializer(options);

            // Serialize with compression
            byte[] compressed = serializer.Serialize(person);
            Console.WriteLine($"Compressed size: {compressed.Length} bytes");

            // Deserialize compressed data
            var restored = serializer.Deserialize<Person>(compressed);
            Console.WriteLine($"Restored from compressed: {restored.Name}");
        }

        public static void ValidatedSerialization()
        {
            var person = new Person
            {
                Name = "Alice Brown",
                Age = 35,
                BirthDate = new DateTime(1988, 12, 25),
                Id = Guid.NewGuid()
            };

            // Define schema
            var schema = @"{
                'type': 'object',
                'properties': {
                    'name': { 'type': 'string' },
                    'age': { 'type': 'integer', 'minimum': 0 },
                    'birthDate': { 'type': 'string', 'format': 'date-time' },
                    'id': { 'type': 'string', 'format': 'uuid' }
                },
                'required': ['name', 'age', 'birthDate', 'id']
            }";

            // Configure validation
            var options = new FlexonOptions
            {
                EnableValidation = true,
                Schema = FlexonSchema.FromString(schema)
            };

            var serializer = new FlexonSerializer(options);

            // Serialize with validation
            byte[] binary = serializer.Serialize(person);
            Console.WriteLine($"Validated serialization size: {binary.Length} bytes");

            // Deserialize with validation
            var restored = serializer.Deserialize<Person>(binary);
            Console.WriteLine($"Restored with validation: {restored.Name}");
        }

        public static void Main()
        {
            Console.WriteLine("Basic Serialization Example:");
            BasicSerialization();

            Console.WriteLine("\nStreaming Serialization Example:");
            StreamingSerialization().Wait();

            Console.WriteLine("\nCompressed Serialization Example:");
            CompressedSerialization();

            Console.WriteLine("\nValidated Serialization Example:");
            ValidatedSerialization();
        }
    }
}
