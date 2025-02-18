using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using RingVideos.Models;
using System;
using System.IO;
using System.Linq;
using Serilog;
using RingVideos.Writers;

namespace RingVideos
{
   class Program
   {
      private static IConfigurationRoot Configuration;
      public static void Main(string[] args)
      {

         var level = LogLevel.Information;

         if (args.Any(a => a.ToLower().EndsWith("-d") || a.ToLower().EndsWith("-debug")))
         {
            level = LogLevel.Debug;
         }
         else if (args.Any(a => a.ToLower().EndsWith("-t") || a.ToLower().EndsWith("-trace")))
         {
            level = LogLevel.Trace;
         }


         Configuration = new ConfigurationBuilder()
          .AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RingVideos", "RingVideosConfig.json"), optional: true, reloadOnChange: true)
          .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
          .Build();

         // Configure Serilog
         Log.Logger = new LoggerConfiguration()
             .ReadFrom.Configuration(Configuration)
             .WriteTo.File(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RingVideos", "ringvideos.log"), rollingInterval: RollingInterval.Day)
             .CreateLogger();

         try
         {
            Log.Information("Starting up");
            CreateHostBuilder(args).Build().Run();
         }
         catch (Exception ex)
         {
            Log.Fatal(ex, "Application start-up failed");
         }
         finally
         {
            Log.CloseAndFlush();
         }
      }
      public static IHostBuilder CreateHostBuilder(string[] args)
      {
       




         var builder = new HostBuilder()
             .UseSerilog()
             .ConfigureServices((hostContext, services) =>
             {
                services.AddHostedService<Worker>();
                services.AddSingleton<StartArgs>(new StartArgs(args));
                services.AddSingleton<RingVideoApplication>();
                services.AddSingleton<CommandHelper>();
                services.AddSingleton<Filter>();
                services.AddSingleton<Authentication>();
                //services.AddSingleton<ConsoleFormatter, CustomConsoleFormatter>();
                services.AddSingleton<ConsoleWriter>();

             });


         return builder;
      }
   }

   public class StartArgs(string[] args)
   {
      public string[] Args { get; set; } = args;
   }
}


