using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RingVideos.Models;
namespace RingVideos
{
   public class Worker : BackgroundService
   {
      private static string[] StartArgs { get; set; }
      private static ILogger<Worker> log;
      private static RingVideoApplication ringApp;
      private static StartArgs sArgs;
      private static IConfiguration config;
      private static CommandHelper cmdHelper;
      private static Parser rootParser;

      public Worker(ILogger<Worker> log, IConfiguration config, RingVideoApplication ringApp,  StartArgs sArgs, CommandHelper cmdHelper )
      {
         Worker.log = log;
         Worker.ringApp = ringApp;
         Worker.sArgs = sArgs;
         Worker.config = config;
         Worker.cmdHelper = cmdHelper;

      }
      protected override async Task ExecuteAsync(CancellationToken stoppingToken)
      {

         try
         {
            string[] exitKeywords = ["exit", "quit", "q"];
            rootParser = cmdHelper.SetupCommands();
            if(Worker.sArgs.Args.Length == 0 ) Worker.sArgs.Args = new string[] { "-h" };
            int val = await rootParser.InvokeAsync(Worker.sArgs.Args);
            while (true)
            {
               Console.ForegroundColor = ConsoleColor.White;
               Console.WriteLine();
               Console.Write("RingVideos> ");
               var line = Console.ReadLine();

               if (line == null || (line.Length == 1 && exitKeywords.Contains(line.Trim().ToLower())))
               {
                  return;
               }

               if (line.Length == 0) line = "-h";

               try
               {
                  val = await rootParser.InvokeAsync(line);
               }
               catch (Exception exe)
               {
                  if (exe.Message != "Nullable object must have a value.") 
                     log.LogError($"❌ Failed to run command: {exe.Message}");
               }
            }
     
         }
         catch(Exception exe)
         {
            log.LogError(exe.Message);
         }
      }

     


      public static async Task<int> GetSnapshotImages(string username, string password, string path, DateTime start, DateTime end)
      {
         return await GetVideos(username, password, path, start, end, false, true, 1000);
      }

      public static async Task<int> GetAllVideos(string username, string password, string path, DateTime start, DateTime end, int maxcount)
      {
         return await GetVideos(username, password, path, start, end, false, false, maxcount);
      }
      public static async Task<int> GetStarredVideos(string username, string password, string path, DateTime start, DateTime end, int maxcount)
      {
         return await GetVideos(username, password, path, start, end, true, false, maxcount);
      }
      private static async Task<int> GetVideos(string username, string password, string path, DateTime start, DateTime end, bool starred, bool snapshot, int maxcount)
      {

         SetFilterAndAuthValues(username, password, path, start, end, starred, snapshot, maxcount);

         if (SetAuthenticationValues())
         {
            return await ringApp.Run();
         }
         else
         {
            return -200;
         }


      }
     
      private static void SetFilterAndAuthValues(string username, string password, string path, DateTime start, DateTime end, bool starred, bool snapshot, int maxcount)
      {
         if (!string.IsNullOrEmpty(username))
         {
            ringApp.Auth.UserName = username;
         }
         if (!string.IsNullOrEmpty(password))
         {
            ringApp.Auth.ClearTextPassword = password;
         }
         if (!string.IsNullOrEmpty(path))
         {
            ringApp.Filter.DownloadPath = path;
         }
         if (start != DateTime.MinValue)
         {
            ringApp.Filter.StartDateTime = start;
         }
         if (end != DateTime.MaxValue)
         {
            ringApp.Filter.EndDateTime = end;
         }
         else
         {
            ringApp.Filter.EndDateTime = DateTime.Now;
         }
  
         ringApp.Filter.OnlyStarred = starred;
         ringApp.Filter.Snapshots = snapshot;
     
         if (maxcount != 1000)
         {
            ringApp.Filter.VideoCount = maxcount;
         }
         if (ringApp.Filter.StartDateTime.HasValue)
         {
            ringApp.Filter.StartDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(ringApp.Filter.StartDateTime.Value, TimeZoneInfo.Local);
         }
         else
         {
            ringApp.Filter.StartDateTime = DateTime.MinValue;
            ringApp.Filter.StartDateTimeUtc = DateTime.MinValue;
         }
         if (ringApp.Filter.EndDateTime.HasValue)
         {
            ringApp.Filter.EndDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(ringApp.Filter.EndDateTime.Value, TimeZoneInfo.Local);
         }
         else
         {
            ringApp.Filter.EndDateTime = DateTime.MaxValue;
            ringApp.Filter.EndDateTimeUtc = DateTime.MaxValue;
         }
      }
         
     

      private static bool SetAuthenticationValues()
      {
         if (string.IsNullOrWhiteSpace(ringApp.Auth.UserName))
         {
            var un = System.Environment.GetEnvironmentVariable("RingUsername");
            if (!string.IsNullOrWhiteSpace(un))
            {
               ringApp.Auth.UserName = un;
            }
            else
            {
               Console.WriteLine("A Ring username is required");
               return false;
            }
         }
         if (string.IsNullOrWhiteSpace(ringApp.Auth.ClearTextPassword))
         {
            var pw = System.Environment.GetEnvironmentVariable("RingPassword");
            if (!string.IsNullOrWhiteSpace(pw))
            {
               ringApp.Auth.ClearTextPassword = pw;
            }
            else
            {
               Console.WriteLine("A Ring password is required");
               return false;
            }
         }

         if (string.IsNullOrWhiteSpace(ringApp.Auth.RefreshToken))
         {
            var rt = System.Environment.GetEnvironmentVariable("RefreshToken");
            if (!string.IsNullOrWhiteSpace(rt))
            {
               ringApp.Auth.RefreshToken = rt;
            }
         }
         return true;
      }

   }
}
