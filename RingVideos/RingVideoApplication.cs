using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RingVideos.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.Threading;
using System.Text;
using Newtonsoft.Json.Serialization;
using KoenZomers.Ring.Api;
using MoreLinq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using static System.Net.WebRequestMethods;
using System.Xml.Linq;
using System.Text.Json;
using eNt = KoenZomers.Ring.Api.Entities;
using Spectre.Console;
using System.Net.NetworkInformation;
using System.Runtime.Intrinsics.X86;
using RingVideos.Writers;


namespace RingVideos
{
   public class RingVideoApplication
   {

      private readonly ILogger log;
      private Session ringSession;
      private static SemaphoreSlim semaphore = new SemaphoreSlim(3, 5);
      public Filter Filter { get; set; } = new();
      public Authentication Auth { get; set; } = new();
      IConfiguration config;
      public readonly string SavedSettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"RingVideosData");
      public readonly string SavedSettingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RingVideosData","RingVideosConfig.json");
      private ConsoleWriter cw;
      public RingVideoApplication(ILogger<RingVideoApplication> logger, IConfiguration config, ConsoleWriter consoleWriter)
      {
         this.log = logger;
         this.config = config;
         this.cw = consoleWriter;
         try
         {
            ReadSettings();
         }
         catch(Exception exe)
         {
            cw.Warning("Failed to load saved settings");
         }
      }

      private void ReadSettings()
      {
         
         try
         {
            var contents = System.IO.File.ReadAllText(SavedSettingsFile);
            var settings = JsonSerializer.Deserialize<Config>(contents);
            this.Auth = settings.Authentication.Decrypt();
            this.Filter = settings.Filter;
         }
         catch(Exception exe)
         {
            var tmpAuth = config.GetSection("Authentication").Get<Authentication>();
            if (tmpAuth != null)
            {
               this.Auth = tmpAuth.Decrypt();
            }
            else
            {
               this.Auth = new Authentication();
            }
            this.Filter = config.GetSection("Filter").Get<Filter>();
            if (this.Filter == null)
            {
               this.Filter = new Filter();
            }
         }

      }
      private void SaveSettings(DateTime? lastSuccessUtc, DateTime? lastFailureUtc)
      {

         Auth.Encrypt();
         //Set "next dates" on filter
         if (lastFailureUtc.HasValue && !Filter.Snapshots)
         {
            Filter.StartDateTime = lastFailureUtc.Value.AddMinutes(-1).ToLocalTime();
            Filter.EndDateTime = null;
         }
         else if (lastSuccessUtc.HasValue && !Filter.Snapshots)
         {
            Filter.StartDateTime = lastSuccessUtc.Value.ToLocalTime();
            Filter.EndDateTime = null;
         }

         if (!Directory.Exists(this.SavedSettingsFolder))
         {
            Directory.CreateDirectory(this.SavedSettingsFolder);
         }

         var conf = new Config()
         {
            Authentication = this.Auth,
            Filter = this.Filter
         };
         var config = JsonSerializer.Serialize(conf, new JsonSerializerOptions() { WriteIndented = true});

         System.IO.File.WriteAllText(this.SavedSettingsFile, config);
      }
      
      public void FilterMessage(string firstLine)
      {
         try
         {
            var expandedPath = Environment.ExpandEnvironmentVariables(Filter.DownloadPath);
            cw.Info("----------------------------");
            cw.Highlight(firstLine);
            StringBuilder message = new StringBuilder();
            if (Filter.StartDateTime.HasValue)
            {
               message.AppendLine($"Start Date:\t{Filter.StartDateTime.Value} [UTC: {Filter.StartDateTimeUtc.Value}]");
            }
            if (Filter.EndDateTime.HasValue)
            {
               message.AppendLine($"End Date:\t{Filter.EndDateTime.Value} [UTC: {Filter.EndDateTimeUtc.Value}]");
            }
            else
            {
               message.AppendLine($"End Date:\tCurrent Time");
            }
            if (Filter.VideoCount != 10000)
            {
               message.AppendLine($"Max downloads:\t{Filter.VideoCount}");
            }
            message.AppendLine($"Only Starred:\t{Filter.OnlyStarred}");
            message.AppendLine($"Snapshots:\t{Filter.Snapshots}");
            if (!string.IsNullOrWhiteSpace(expandedPath))
            {
               message.AppendLine($"Download Path:\t{expandedPath}");
            }
            message.AppendLine("----------------------------");
            cw.Info(message.ToString());

         }
         catch(Exception)
         {
           
         }
      }
      internal async Task<Session> Authenicate()
      {
         Session session = null;
         if (!string.IsNullOrWhiteSpace(this.Auth.ClearTextRefreshToken))
         {
            try
            {
               // Use refresh token from previous session
               await AnsiConsole.Status()
                   .StartAsync("Authenticating using refresh token from previous session...", async ctx =>
                   {
                      ctx.Spinner(Spinner.Known.Dots2); ;
                      ctx.SpinnerStyle(Style.Parse("yellow"));
                      session = await Session.GetSessionByRefreshToken(this.Auth.ClearTextRefreshToken);
                      Thread.Sleep(500);
                   });
            }
            catch(Exception exe)
            {
               cw.Error("Failed to authenticate with refresh token");
            }
         }
         if (session == null)
         {
            try
            {
               // Use the username and password provided
               await AnsiConsole.Status()
                   .StartAsync("Authenticating using provided username and password.", async ctx => {
                      ctx.Spinner(Spinner.Known.Dots2); ;
                      ctx.SpinnerStyle(Style.Parse("yellow"));
                      session = new Session(this.Auth.UserName, this.Auth.ClearTextPassword);
                      await session.Authenticate();
                      Thread.Sleep(500);
                   });
               
            }
            catch (KoenZomers.Ring.Api.Exceptions.TwoFactorAuthenticationRequiredException)
            {
               // Two factor authentication is enabled on the account. The above Authenticate() will trigger a text message to be sent. Ask for the token sent in that message here.
               cw.Info($"Two factor authentication enabled on this account, please enter the token received in the text message on your phone:");
               var token = Console.ReadLine();

               // Authenticate again using the two factor token
               await session.Authenticate(twoFactorAuthCode: token);
            }
            catch (KoenZomers.Ring.Api.Exceptions.ThrottledException e)
            {
               Console.WriteLine(e.Message);
            }
            catch (KoenZomers.Ring.Api.Exceptions.AuthenticationFailedException e)
            {
               cw.Error($"{e.Message}: Please validate your credentials");
            }
            catch (System.Net.WebException e)
            {
               cw.Error($"{e.Message}: Connection failed, please validate your credentials.");
            }
            catch(Exception exe)
            {
               cw.Error($"{exe.Message}");
            }
         }

         if (session != null && session.OAuthToken != null)
         {
           this.Auth.ClearTextRefreshToken = session.OAuthToken.RefreshToken;
            SaveSettings(null, null);
         }
         return session;
      }

      internal async Task<int> Run()
      {

         try
         {
            DateTime? lastSuccess = null;
            DateTime? firstFailure = null;
            int failedCount = 0;
            List<(bool success, eNt.DoorbotHistoryEvent ding)> results = new();
            this.ringSession = await Authenicate();
            this.Auth.ClearTextRefreshToken = this.ringSession.OAuthToken.RefreshToken;
            if (this.ringSession == null || !this.ringSession.IsAuthenticated)
            {
               return 999;
            }

            if (Filter.DownloadPath == null)
            {
               cw.Error("A valid download path '--path' argument is required");
               return -1;
            }

            var expandedPath = Environment.ExpandEnvironmentVariables(Filter.DownloadPath);
            if (!string.IsNullOrWhiteSpace(expandedPath))
            {
               if (!Directory.Exists(expandedPath))
               {
                  Directory.CreateDirectory(expandedPath);
               }
            }
            else
            {
               cw.Error("A valid download path '--path' argument is required");
               return -1;
            }



            this.FilterMessage("Fetching videos with the following settings:");
            if (!Filter.Snapshots)
            {
               DeviceList deviceList = new();
               if (Filter.DeviceId.HasValue && Filter.DeviceId.Value > 0)
               {
                  deviceList.Devices.Add(new DeviceInfo() { Id = Filter.DeviceId.Value });
               }
               else
               {
                  deviceList = await GetDevicesList();
               }
               List<eNt.DoorbotHistoryEvent> dings = new();
               try
               {
                  foreach (var dev in deviceList.Devices)
                  {
                     await AnsiConsole.Status()
                            .StartAsync($"Querying for videos to download from {dev.Name}...", async ctx =>
                            {
                               ctx.Spinner(Spinner.Known.Dots2); ;
                               ctx.SpinnerStyle(Style.Parse("yellow"));
                               dings.AddRange(await ringSession.GetDoorbotsHistory(Filter.StartDateTimeUtc.Value, Filter.EndDateTimeUtc, dev.Id));
                               Thread.Sleep(500);
                            });
                  }
               }
               catch(Exception exe)
               {
                  cw.Error(exe.Message);
                  log.LogError(exe.ToString());
                  return -1;
               }
             
               if (Filter.OnlyStarred)
               {
                  dings = dings.Where(d => d.Favorite == true).ToList();
               }
               dings = dings.OrderBy(d => d.CreatedAtDateTime).ToList();
               string limitmessage = "";
               if(dings.Count() > Filter.VideoCount)
               {
                  limitmessage = $"Will download the first {Filter.VideoCount} based on MaxCount setting";
               }
               cw.Info("");
               cw.Highlight($"Found {dings.Count()} videos to download. {limitmessage}");

               //Order by date
               
               var messages = new List<string>();

               List<Task<(bool success, eNt.DoorbotHistoryEvent ding)>> tasks = new();
               int videoCount = 0;
               if(dings.Count >= Filter.VideoCount)
               {
                  videoCount = Filter.VideoCount;
               }
               else
               {
                  videoCount = dings.Count;
               }

               var byDevice = dings.Take(videoCount).GroupBy(d => d.Doorbot.Id).ToList();
               StringBuilder sb = new();
               if (!Filter.DeviceId.HasValue)
               {
                  foreach (var grp in byDevice)
                  {
                     var name = deviceList.Devices.Where(d => d.Id == grp.FirstOrDefault().Doorbot.Id).FirstOrDefault().Name;
                     var count = grp.Count();
                     var s = "";
                     if (count > 1)
                     {
                        s = "s";
                     }
                     sb.Append($"{grp.Count()} video{s} from {name} and ");
                  }
                  if (sb.Length > 4)
                  {
                     sb.Length = sb.Length - 4;
                     cw.Info($"Will download {sb.ToString()}");
                  }
               }


               for (int i = 0; i < videoCount; i++)
               {
                  tasks.Add(SaveRecordingAsync(i + 1, dings[i], Filter));
               }



               results = (await Task.WhenAll(tasks.ToArray())).ToList();
               var success = results.Count(r => r.success == true);
               lastSuccess = results.ToList().Where(r => r.success == true)?.Select(r => r.ding.CreatedAtDateTime).Max();
               failedCount = results.Count(r => r.success == false);
               cw.Highlight($"{Environment.NewLine}Successfully downloaded {success} videos");
              
            }
            else
            {
               await DownloadSnapshots(Filter);
            }
            
            SaveSettings(lastSuccess, firstFailure);
            if(failedCount > 0)
            {
               cw.Error($"{Environment.NewLine}Failed to download {failedCount} videos.");
               firstFailure = results.ToList().Where(r => r.success == false)?.Select(r => r.ding.CreatedAtDateTime).Min();
               TimeZoneInfo.Local.GetUtcOffset(firstFailure.Value);
               var est = firstFailure.Value.ToLocalTime();
               cw.Warning($"Date of first failed download recorded ({est}). Rerun without a --start value to retry the downloads starting at that point");

            }

            cw.Info($"{Environment.NewLine}Done!");
            cw.ClearLineWriters();
            return 0;
         }
         catch (Exception exe)
         {
            cw.Error(exe.ToString());
            log.LogError(exe.ToString());
            return -1;
         }
      }

      internal async Task<bool> DownloadSnapshots(Filter filter)
      {
         var expandedPath = Environment.ExpandEnvironmentVariables(filter.DownloadPath);
         DateTime est = DateTime.Now;
         string fileNameFormat = Path.Combine(expandedPath,
             $"{est.Year}-{est.Month.ToString().PadLeft(2, '0')}-{est.Day.ToString().PadLeft(2, '0')}-T{est.Hour.ToString().PadLeft(2, '0')}_{est.Minute.ToString().PadLeft(2, '0')}_{est.Second.ToString().PadLeft(2, '0')}" + "--{0}.jpg");

         string fileName = string.Empty;
         var devices = await this.ringSession.GetRingDevices();
         foreach (var d in devices.Doorbots)
         {
            fileName = string.Format(fileNameFormat, d.Description);
            await GetSnapshot(d.Id, fileName);
         }
         foreach (var d in devices.Chimes)
         {
            fileName = string.Format(fileNameFormat, d.Description);
            await GetSnapshot(d.Id, fileName);
         }
         foreach (var d in devices.AuthorizedDoorbots)
         {
            fileName = string.Format(fileNameFormat, d.Description);
            await GetSnapshot(d.Id, fileName);
         }
         foreach (var d in devices.StickupCams)
         {
            fileName = string.Format(fileNameFormat, d.Description);
            await GetSnapshot((int)d.Id, fileName);
         }

         return true;

      }
      internal async Task<bool> GetSnapshot(int doorbotId, string fileName)
      {
         await this.ringSession.UpdateSnapshot(doorbotId);
         await this.ringSession.GetLatestSnapshot(doorbotId, fileName);
         cw.Info($"Downloaded snapshot {fileName}");
         log.LogInformation($"Downloaded snapshot {fileName}");

         return true;
      }

      internal async Task<(bool, eNt.DoorbotHistoryEvent ding)> SaveRecordingAsync(int index, KoenZomers.Ring.Api.Entities.DoorbotHistoryEvent ding, Filter filter)
      {

         LineWriter lw = cw.GetLineWriter();
         semaphore.Wait();
         try
         {
            
            string filename = string.Empty;
            var expandedPath = Environment.ExpandEnvironmentVariables(filter.DownloadPath);

            TimeZoneInfo.Local.GetUtcOffset(ding.CreatedAtDateTime.Value);
            var est = ding.CreatedAtDateTime.Value.ToLocalTime();
            var timestamp = $"{est.Year}-{est.Month.ToString().PadLeft(2, '0')}-{est.Day.ToString().PadLeft(2, '0')}-T{est.Hour.ToString().PadLeft(2, '0')}_{est.Minute.ToString().PadLeft(2, '0')}_{est.Second.ToString().PadLeft(2, '0')}";
            var shortFileName = $"{timestamp}--{ding.Doorbot.Description}.mp4";
            filename = Path.Combine(expandedPath, shortFileName);

            string msg = $"{index.ToString().PadLeft(3,'0')}) {shortFileName} | {ding.CreatedAtDateTime.Value.ToLocalTime().ToString("MM/dd/yyyy hh:mm:ss tt")} | {ding.Kind} | {ding.Doorbot.Description} :: ";
            cw.Write(lw,msg);
            cw.Update(lw,"Downloading");

            int attempt = 1;
            do
            {
               attempt++;

               //log.LogInformation($"{itemCount + 1} - {filename}... ");
               try
               {
                  await this.ringSession.GetDoorbotHistoryRecording(ding, filename);
                  cw.UpdateFinal(lw, $"Complete - ({new FileInfo(filename).Length / 1048576} MB)");
                  break;
               }
               catch (AggregateException e)
               {
                  if (e.InnerException != null && e.InnerException.GetType() == typeof(System.Net.WebException) && ((System.Net.WebException)e.InnerException).Response != null)
                  {
                     var webException = (System.Net.WebException)e.InnerException;
                     var response = new StreamReader(webException.Response.GetResponseStream()).ReadToEnd();

                     cw.UpdateError(lw, $"Failed: ({(e.InnerException != null ? e.InnerException.Message : e.Message)} - {response})");
                  }
                  else
                  {
                     cw.UpdateError(lw, $"Failed: ({(e.InnerException != null ? e.InnerException.Message : e.Message)})");
                  }
               }
               catch (Exception exe)
               {
                  cw.UpdateError(lw, $"Failed ({(exe.InnerException != null ? exe.InnerException.Message : exe.Message)})");
               }

               if (attempt >= 10)
               {
                  cw.UpdateWarning(lw, $"Giving up after {attempt} tries.");
                  return (false, ding);
               }
               else
               {
                  cw.UpdateWarning(lw, $"Retrying: {attempt + 1}/10.");
               }

            } while (attempt < 10);

            return (true, ding);
         }
         finally
         {
            semaphore.Release();
         }
      }

      internal async Task<DeviceList> GetDevicesList(string username = "", string password = "")
      {
         if (this.ringSession == null)
         {
            this.Auth.UserName = username;
            this.Auth.ClearTextPassword = password;
            this.ringSession = await Authenicate();
         }

         eNt.Devices devices = new();
         try
         {
            await AnsiConsole.Status()
                   .StartAsync("Getting list of registered devices...", async ctx =>
                   {
                      ctx.Spinner(Spinner.Known.Dots2); ;
                      ctx.SpinnerStyle(Style.Parse("yellow"));
                      devices = await ringSession.GetRingDevices();
                      Thread.Sleep(500);
                   });
         }
         catch (Exception exe)
         {
            cw.Error(exe.Message);
            log.LogError(exe.ToString());
            return null;
         }

         DeviceList deviceList = new DeviceList().ExtractDevices(devices);
         cw.Highlight("Found registered devices:");
         foreach (var x in deviceList.Devices)
         {
            cw.Info($"{x.Name}\tId: {x.Id}");
         }

         return deviceList;
      }
   }
}
