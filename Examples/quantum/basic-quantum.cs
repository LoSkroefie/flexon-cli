using System;
using System.IO;
using System.Threading.Tasks;

class QuantumExample
{
    static async Task Main()
    {
        // Basic quantum encryption
        await EncryptWithQuantumAES();
        
        // Quantum compression
        await CompressWithQuantum();
        
        // Full quantum processing
        await ProcessWithQuantum();
    }
    
    static async Task EncryptWithQuantumAES()
    {
        // Create sample data
        var data = "Sensitive data for quantum encryption";
        File.WriteAllText("sensitive.txt", data);
        
        // Encrypt with QuantumAES
        await ExecuteFlexon("encrypt sensitive.txt -e mykey --algorithm QuantumAES");
        
        // Verify encryption
        await ExecuteFlexon("verify sensitive.txt.encrypted --algorithm QuantumAES");
        
        Console.WriteLine("Quantum encryption complete!");
    }
    
    static async Task CompressWithQuantum()
    {
        // Create large sample data
        var largeData = new string('A', 1000000); // 1MB of data
        File.WriteAllText("large.dat", largeData);
        
        // Compress with quantum compression
        await ExecuteFlexon("compress large.dat --method QuantumCompression");
        
        // Check compression ratio
        await ExecuteFlexon("info large.dat.compressed");
        
        Console.WriteLine("Quantum compression complete!");
    }
    
    static async Task ProcessWithQuantum()
    {
        // Create sample data
        var data = "Data for quantum processing";
        File.WriteAllText("data.txt", data);
        
        // Process with full quantum capabilities
        await ExecuteFlexon("process data.txt --mode Quantum");
        
        Console.WriteLine("Quantum processing complete!");
    }
    
    static Task ExecuteFlexon(string arguments)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "flexon",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        
        return Task.Run(() =>
        {
            process.Start();
            process.WaitForExit();
        });
    }
}
