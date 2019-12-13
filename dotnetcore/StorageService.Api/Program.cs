using System.Threading.Tasks;
using Dapr.Client.Grpc;
using Google.Protobuf;
using Grpc.Net.Client;

namespace StorageService.Api
{
    using System;
    using System.Linq;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().MigrateDatabase<StorageContext>().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
        }

        /// <summary>
        /// Ç¨ÒÆ.
        /// </summary>
        /// <typeparam name="T">T.</typeparam>
        /// <param name="host">host.</param>
        /// <returns>IHost.</returns>
        public static IHost MigrateDatabase<T>(this IHost host)
            where T : DbContext
        {
            using (IServiceScope scope = host.Services.CreateScope())
            {
                IServiceProvider services = scope.ServiceProvider;
                var db = services.GetRequiredService<StorageContext>();
                if (db.Database.GetPendingMigrations().Any())
                {
                    db.Database.Migrate();
                }
            }

            return host;
        }


        public static async Task SomeTest()
        {
            string defaultPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? "54681";

            // Set correct switch to make insecure gRPC service calls. This switch must be set before creating the GrpcChannel.
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // Create Client
            string daprUri = $"http://127.0.0.1:{defaultPort}";
            GrpcChannel channel = GrpcChannel.ForAddress(daprUri);
            var client = new Dapr.Client.Grpc.Dapr.DaprClient(channel);
            Console.WriteLine(daprUri);

            InvokeServiceResponseEnvelope result = await client.InvokeServiceAsync(new InvokeServiceEnvelope
            {
                Method = "MyMethod",
                Id = "productService",
                Data = new Google.Protobuf.WellKnownTypes.Any
                {
                    Value = ByteString.CopyFromUtf8("Hello ProductService")
                }
            });
            Console.WriteLine("this is call result:" + result.Data.CalculateSize());
            Console.WriteLine("this is call result:" + result.Data.Value);
            var productResult = result.Data.Unpack<ProductList.V1.ProductList>();
            Console.WriteLine("this is call result:" + productResult.Results.FirstOrDefault());
        }
    }
}