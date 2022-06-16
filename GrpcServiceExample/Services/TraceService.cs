using Grpc.Core;
using Microsoft.Extensions.Logging;
using NewRelic.Agent.Core.Segments;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GrpcServiceExample
{
	public class TraceService : IngestService.IngestServiceBase
	{
        private readonly ILogger<WeatherService> _logger;

        public TraceService(ILogger<WeatherService> logger) => _logger = logger;

		private static int _channelID = 0;
		public override Task RecordSpan(IAsyncStreamReader<Span> requestStream, IServerStreamWriter<RecordStatus> responseStream, ServerCallContext context)
		{
			var channelID = Interlocked.Increment(ref _channelID);

			Console.WriteLine($"Channel #{channelID}: Created");

			var spanCount = 0;

			Action statsWriterWorker = () =>
			{
				var statVal = Interlocked.Exchange(ref spanCount, 0);
				if (statVal == 0)
				{
					return;
				}
				Console.WriteLine($"Channel #{channelID}: Stats Output: Count Spans: {statVal}");
				responseStream.WriteAsync(new RecordStatus() { MessagesSeen = (ulong)statVal });
			};

			var expireDtm = DateTime.Now.Add(TimeSpan.FromSeconds(60));

			while (requestStream.MoveNext(context.CancellationToken).Result/* && DateTime.Now < expireDtm*/)
			{
				//pretending busy
				Thread.Sleep(100);

				var x = Interlocked.Increment(ref spanCount);
				var span = requestStream.Current;

				//Reply every 50 spans
				if (x % 50 == 0)
				{
					statsWriterWorker();
				}
			}

			Console.WriteLine($"Channel #{channelID}: ending");


			//return Task.CompletedTask;
			return Task.FromException(new RpcException(Status.DefaultSuccess));
		}

	}
}
