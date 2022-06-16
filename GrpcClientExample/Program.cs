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
            while (Console.ReadKey().Key != ConsoleKey.Escape)
            {
                GrpcCustomLib.GrpcCustomLib.RecordSpan(300).Wait(300000);
            }
        }
    }
}
