using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RingVideos.Models;
using System;
using System.IO;
using System.Linq;
using Serilog;
using Serilog.Events;
using RingVideos.Writers;

namespace RingVideos
{
   class Program
   {
      public static string logFileBaseName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RingVideosData", "ringvideos.log");
      public static string configFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RingVideosData", "ringvideos.log");
      private static IConfigurationRoot Configuration;
      public static void Main(string[] args)
      {
         // Extract and strip verbosity flags (-d / --debug / -t / --trace) before
         // they are handed off to System.CommandLine, which doesn't know about them.
         var (filteredArgs, minimumLevel) = ExtractVerbosityFlags(args);

         Configuration = new ConfigurationBuilder()
          .AddJsonFile(configFileName, optional: true, reloadOnChange: true)
          .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
          .Build();

         // Configure Serilog
         var loggerConfig = new LoggerConfiguration()
             .ReadFrom.Configuration(Configuration)
             .WriteTo.File(logFileBaseName, rollingInterval: RollingInterval.Day);

         if (minimumLevel.HasValue)
         {
            loggerConfig = loggerConfig.MinimumLevel.Is(minimumLevel.Value);
         }

         Log.Logger = loggerConfig.CreateLogger();

         try
         {
            Log.Information("Starting up");
            CreateHostBuilder(filteredArgs).Build().Run();
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

      private static (string[] args, LogEventLevel? level) ExtractVerbosityFlags(string[] args)
      {
         LogEventLevel? level = null;
         var filtered = args.Where(a =>
         {
            var lower = a.ToLowerInvariant();
            if (lower == "-d" || lower == "--debug")
            {
               level = LogEventLevel.Debug;
               return false;
            }
            if (lower == "-t" || lower == "--trace")
            {
               level = LogEventLevel.Verbose;
               return false;
            }
            return true;
         }).ToArray();
         return (filtered, level);
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



