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
using RingVideos.Writers;
using Serilog;
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
      private static ConsoleWriter cw;

      public Worker(ILogger<Worker> log, IConfiguration config, RingVideoApplication ringApp,  StartArgs sArgs, CommandHelper cmdHelper, ConsoleWriter  consoleWriter)
      {
         Worker.log = log;
         Worker.ringApp = ringApp;
         Worker.sArgs = sArgs;
         Worker.config = config;
         Worker.cmdHelper = cmdHelper;
         Worker.cw = consoleWriter;

      }
      protected override async Task ExecuteAsync(CancellationToken stoppingToken)
      {

         try
         {
            rootParser = cmdHelper.SetupCommands();
            bool showFilter = false;
            if (Worker.sArgs.Args.Length == 0)
            {
               Worker.sArgs.Args = new string[] { "-h" };
               showFilter = true;
            }

            int val = await rootParser.InvokeAsync(Worker.sArgs.Args);
            //if (showFilter)
            //{
            //   ringApp.FilterMessage("Saved filter settings (use command flags to override):");
            //}
            if (Worker.sArgs.Args.Contains("-x") || Worker.sArgs.Args.Contains("--exit"))
            {
               Environment.Exit(val);
            }
            showFilter = false;
            while (true)
            {
               ringApp.FilterMessage("Saved filter settings (use command flags to override):");
               Console.ForegroundColor = ConsoleColor.White;
               Console.WriteLine();
               Console.Write("RingVideos> ");
               var line = Console.ReadLine();

               if (line.Length == 0)
               {
                  line = "-h";
                  showFilter = true;
               }

               try
               {
                  val = await rootParser.InvokeAsync(line);
                  //if (showFilter)
                  //{
                  //   ringApp.FilterMessage("Saved filter settings (use command flags to override):");
                  //}
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

     


      public static async Task<int> GetSnapshotImages(string username, string password, string path, DateTime start, DateTime end, long? deviceId)
      {
         return await GetVideos(username, password, path, start, end, false, true, 1000, deviceId);
      }

      public static async Task<int> GetAllVideos(string username, string password, string path, DateTime start, DateTime end, int maxcount, long? deviceId)
      {
         return await GetVideos(username, password, path, start, end, false, false, maxcount, deviceId);
      }
      public static async Task<int> GetStarredVideos(string username, string password, string path, DateTime start, DateTime end, int maxcount, long? deviceId)
      {
         return await GetVideos(username, password, path, start, end, true, false, maxcount, deviceId);
      }
      private static async Task<int> GetVideos(string username, string password, string path, DateTime start, DateTime end, bool starred, bool snapshot, int maxcount, long? deviceId)
      {

         SetFilterAndAuthValues(username, password, path, start, end, starred, snapshot, maxcount, deviceId);

         if (SetAuthenticationValues())
         {
            return await ringApp.Run();
         }
         else
         {
            return -200;
         }


      }
     
      private static void SetFilterAndAuthValues(string username, string password, string path, DateTime start, DateTime end, bool starred, bool snapshot, int maxcount, long? deviceId)
      {
         if (!string.IsNullOrEmpty(username))
         {
            ringApp.Auth.UserName = username;
         }
         if (!string.IsNullOrEmpty(password))
         {
            ringApp.Auth.ClearTextPassword = password;
            ringApp.Auth.ClearTextRefreshToken = "";
            ringApp.Auth.RefreshToken = "";
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
     
         if (maxcount != 0)
         {
            ringApp.Filter.VideoCount = maxcount;
         }
         if (!ringApp.Filter.StartDateTime.HasValue)
         { 
            ringApp.Filter.StartDateTime = DateTime.MinValue;
         }
         if (!ringApp.Filter.EndDateTime.HasValue)
         {
            ringApp.Filter.EndDateTime = DateTime.MaxValue;
         }
         if(deviceId.HasValue && deviceId.Value > 0)
         {
            ringApp.Filter.DeviceId = deviceId;
         }
      }
         
     
      public static void QuitApplication()
      {
         Environment.Exit(0);
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

      internal static void ShowLog(object t)
      {
         var folder = Path.GetDirectoryName(Program.logFileBaseName);
         var fileRoot = Path.GetFileNameWithoutExtension(Program.logFileBaseName);
         var dirInf = new DirectoryInfo(folder);
         var currentLogFile = dirInf.GetFiles($"{fileRoot}*.log").OrderByDescending(f => f.LastWriteTime).FirstOrDefault();


         cw.Warning($"Log file can be found here: {currentLogFile}");
         cw.Info("Last 100 lines from log file:");
         cw.Info("");

         try
         {
            string filecontent;
            using (FileStream fileStream = new FileStream(currentLogFile.FullName,FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(fileStream))
            {
               filecontent = reader.ReadToEnd();
            }
            var lines = filecontent.Split(Environment.NewLine);
            var last100 = lines.Skip(Math.Max(0, lines.Count()) - 100);
            foreach (var line in last100)
            {
               cw.Info(line);
            }

         }
         catch (Exception exe)
         {
            cw.Error(exe.Message);
            log.LogError(exe.ToString());
         }

      }

      internal static async Task DeviceList(string username, string password)
      {
         await ringApp.GetDevicesList(username, password);
      }
   }
}
