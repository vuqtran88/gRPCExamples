using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using NewRelic.Agent.Core.Segments;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GrpcCustomLib
{
    public class GrpcCustomLib
    {
        public static async Task<string> SayHello(string name)
        {
#if !NETSTANDARD2_0
            Console.WriteLine($"This app target framework is NOT netstandard2.0. Framework: {Environment.Version}");
#else
            Console.WriteLine($"This app target framework is netstandard2.0. Framework: {Environment.Version}");
#endif

            var channel = GrpcChannel.ForAddress("https://localhost:5005");

            var client = new Greeter.GreeterClient(channel);

            var request = new HelloRequest();
            request.Name = name ?? string.Empty;

            var rp = await client.SayHelloAsync(request);

            return rp.Message;

        }

        public static async Task GetWeatherStream(int timeout)
        {
            var grpcOption = new GrpcChannelOptions();

#if NETFRAMEWORK
            grpcOption.HttpHandler = new WinHttpHandler();
#endif

            using var channel = GrpcChannel.ForAddress("https://localhost:5005", grpcOption);
            var client = new WeatherForecasts.WeatherForecastsClient(channel);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));

            using var streams = client.GetWeatherStream(cancellationToken: cts.Token);

            var requestStream = streams.RequestStream;
            var responseStream = streams.ResponseStream;

            var task1 = Task.Run(async () =>
            {
                try
                {
                    var requestData = new RequestContext();

                    requestData.Condition = "FailedPrecondition";

                    await requestStream.WriteAsync(requestData);
                }
                catch (Exception ex) 
                {
                    Console.WriteLine(ex.ToString());
                }
            });

            var task2 = Task.Run(async () =>
            {
                try
                {
                    var success = await responseStream.MoveNext(cts.Token);
                    while (success)
                    {
                        var weatherData = responseStream.Current;

                        Console.WriteLine($"{weatherData.DateTimeStamp.ToDateTime():s} | {weatherData.TemperatureC} C");

                        success = await responseStream.MoveNext(cts.Token);
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    Console.WriteLine(ex.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });

            await Task.WhenAll(task1, task2, Task.Delay(TimeSpan.FromSeconds(timeout)));
        }

        public static async Task RecordSpan(int timeoutMs) 
        {

            var grpcOption = new GrpcChannelOptions();

            grpcOption.Credentials = new SslCredentials();
#if NETFRAMEWORK
            grpcOption.HttpHandler = new Http2CustomHandler();
#endif

            using var channel = GrpcChannel.ForAddress("https://localhost:5005", grpcOption);
            var client = new IngestService.IngestServiceClient(channel);

            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));

            using var streams = client.RecordSpan(cancellationToken: cts.Token);

            var requestStream = streams.RequestStream;
            var responseStream = streams.ResponseStream;

            var tasks = new List<Task>();

            tasks.Add(Task.Run(async () =>
            {
                for (int i = 0; i < 200; i++)
                {
                    try
                    {

                        var span = new Span();

                        await requestStream.WriteAsync(span);

                        if (i % 50 == 0)
                            Console.WriteLine($"Sent: 50 spans");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }

                }
            }));

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var success = await responseStream.MoveNext(cts.Token);
                    while (success)
                    {
                        var status = responseStream.Current;

                        Console.WriteLine($"Server Received: {status.MessagesSeen}");

                        success = await responseStream.MoveNext(cts.Token);
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    Console.WriteLine(ex.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }));

            tasks.Add(Task.Delay(TimeSpan.FromSeconds(timeoutMs)));

            await Task.WhenAll(tasks);
        }

    }


    public class Http2CustomHandler : WinHttpHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            request.Version = new Version("2.0");
            return base.SendAsync(request, cancellationToken);
        }
    }
}




