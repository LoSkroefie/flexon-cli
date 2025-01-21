using FlexonCLI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace FlexonExamples.Integration
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Add FLEXON formatters to MVC
            services.AddControllers()
                .AddFlexonFormatters(options =>
                {
                    options.EnableCompression = true;
                    options.CompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
                    options.EnableValidation = true;
                });

            // Add FLEXON serializer as a service
            services.AddSingleton<IFlexonSerializer>(provider =>
            {
                var options = new FlexonOptions
                {
                    EnableCompression = true,
                    EnableValidation = true,
                    UsePooledBuffers = true
                };
                return new FlexonSerializer(options);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    public class WeatherForecast
    {
        public DateTime Date { get; set; }
        public int TemperatureC { get; set; }
        public string Summary { get; set; }
        public Guid Id { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IFlexonSerializer _serializer;

        public WeatherForecastController(IFlexonSerializer serializer)
        {
            _serializer = serializer;
        }

        [HttpGet]
        [Produces("application/x-flexon")]
        public async Task<WeatherForecast> Get()
        {
            var forecast = new WeatherForecast
            {
                Date = DateTime.Now,
                TemperatureC = 23,
                Summary = "Warm",
                Id = Guid.NewGuid()
            };

            return forecast;
        }

        [HttpPost]
        [Consumes("application/x-flexon")]
        [Produces("application/x-flexon")]
        public async Task<IActionResult> Post([FromBody] WeatherForecast forecast)
        {
            // Process the forecast
            // The FLEXON formatter will handle serialization/deserialization

            return Ok(forecast);
        }

        [HttpGet("stream")]
        public async Task StreamForecasts()
        {
            Response.ContentType = "application/x-flexon";

            using var writer = new FlexonStreamWriter(Response.Body);
            
            for (int i = 0; i < 1000; i++)
            {
                var forecast = new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(i),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = "Generated",
                    Id = Guid.NewGuid()
                };

                await writer.WriteAsync(forecast);
                await Response.Body.FlushAsync();
            }
        }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
