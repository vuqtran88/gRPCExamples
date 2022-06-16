using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Grpc.Net.Client;
using GrpcCustomLib;

namespace GrpcClientExample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Client. Press any key to start.");
            while (Console.ReadKey().Key != ConsoleKey.Escape)
            {
                GrpcCustomLib.GrpcCustomLib.RecordSpan(300000).Wait(300000);
                //var a = GrpcCustomLib.GrpcCustomLib.SayHello("Vu").Result;
                //GrpcCustomLib.GrpcCustomLib.GetWeatherStream(300000).Wait(300000);
            }
        }
    }
}
