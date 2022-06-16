using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace GrpcServiceExample
{
	public class WeatherService : WeatherForecasts.WeatherForecastsBase
	{
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(ILogger<WeatherService> logger) => _logger = logger;

        public override async Task GetWeatherStream(RequestContext request, IServerStreamWriter<WeatherData> responseStream, ServerCallContext context)
        {

            if (request.Condition == "FailedPrecondition")
            {
                throw new RpcException(new Status(Grpc.Core.StatusCode.FailedPrecondition, "My exception!"));
            }

            var rng = new Random();
            var now = DateTime.UtcNow;

            var i = 0;
            while (!context.CancellationToken.IsCancellationRequested && i < 10)
            {
                await Task.Delay(100); // Gotta look busy

                var forecast = new WeatherData
                {
                    DateTimeStamp = Timestamp.FromDateTime(now.AddDays(i++)),
                    TemperatureC = rng.Next(-20, 55),
                };

                _logger.LogInformation("Sending WeatherData response");

                await responseStream.WriteAsync(forecast);
            }
        }
    }
}
