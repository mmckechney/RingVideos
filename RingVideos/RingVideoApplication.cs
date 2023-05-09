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

namespace RingVideos
{
    public class RingVideoApplication
    {

        private readonly ILogger log;
        private Session ringSession;
        private static SemaphoreSlim semaphore = new SemaphoreSlim(5, 10);
        public RingVideoApplication(ILogger<RingVideoApplication> logger)
        {
            log = logger;
        }

        internal async Task<Session> Authenicate(Authentication auth)
        {
            Session session = null;
            if (!string.IsNullOrWhiteSpace(auth.ClearTextRefreshToken))
            {
                // Use refresh token from previous session
                Console.WriteLine("Authenticating using refresh token from previous session");
                try
                {
                    session = Session.GetSessionByRefreshToken(auth.ClearTextRefreshToken).Result;
                } catch
                {
                    Console.WriteLine("Failed to authenticate with refresh token");
                }
            }
            if (session == null)
            {
                // Use the username and password provided
                Console.WriteLine("Authenticating using provided username and password");

                session = new Session(auth.UserName, auth.ClearTextPassword);

                try
                {
                    await session.Authenticate();
                }
                catch (KoenZomers.Ring.Api.Exceptions.TwoFactorAuthenticationRequiredException)
                {
                    // Two factor authentication is enabled on the account. The above Authenticate() will trigger a text message to be sent. Ask for the token sent in that message here.
                    Console.WriteLine($"Two factor authentication enabled on this account, please enter the token received in the text message on your phone:");
                    var token = Console.ReadLine();

                    // Authenticate again using the two factor token
                    await session.Authenticate(twoFactorAuthCode: token);
                }
                catch (KoenZomers.Ring.Api.Exceptions.ThrottledException e)
                {
                    Console.WriteLine(e.Message);
                    //Environment.Exit(1);
                }
                catch (System.Net.WebException)
                {
                    Console.WriteLine("Connection failed. Validate your credentials.");
                    //Environment.Exit(1);
                }
            }

            if (session != null && session.OAuthToken != null)
            {
                auth.ClearTextRefreshToken = session.OAuthToken.RefreshToken;
            }
            return session;
        }

        internal async Task<int> Run(Filter filter, Authentication auth)
        {
            try
            {
                this.ringSession = await Authenicate(auth);
                if (this.ringSession == null || !this.ringSession.IsAuthenticated)
                {
                    return 999;
                }
                var expandedPath = Environment.ExpandEnvironmentVariables(filter.DownloadPath);
                if (!string.IsNullOrWhiteSpace(expandedPath))
                {
                    if (!Directory.Exists(expandedPath))
                    {
                        Directory.CreateDirectory(expandedPath);
                    }
                }
                else
                {
                    log.LogError("A valid download path '--path' argument is required");
                    return -1;
                }

                StringBuilder message = new StringBuilder($"Fetching videos with the following settings:\r\n");
                if (filter.StartDateTime.HasValue)
                {
                    message.AppendLine($"Start Date:\t{filter.StartDateTime.Value} [UTC: {filter.StartDateTimeUtc.Value}]");
                }
                if (filter.EndDateTime.HasValue)
                {
                    message.AppendLine($"End Date:\t\t{filter.EndDateTime.Value} [UTC: {filter.EndDateTimeUtc.Value}]");
                }
                if (!string.IsNullOrWhiteSpace(expandedPath))
                {
                    message.AppendLine($"Download Path:\t{expandedPath}");
                }
                if (filter.VideoCount != 10000)
                {
                    message.AppendLine($"Max downloads:\t{filter.VideoCount}");
                }
                message.AppendLine($"Only Starred:\t{filter.OnlyStarred}");
                message.AppendLine($"Snapshots:\t{filter.Snapshots}");

                log.LogInformation(message.ToString());
                if (!filter.Snapshots)
                {
                    log.LogInformation("Querying for videos to download...");
                    var dings = await ringSession.GetDoorbotsHistory(filter.StartDateTimeUtc.Value, filter.EndDateTimeUtc);
                    if (filter.OnlyStarred)
                    {
                        dings = dings.Where(d => d.Favorite == true).ToList();
                    }
                    log.LogInformation($"Found {dings.Count()} videos to download");

                    var messages = new List<string>();

                    List<Task> tasks = new List<Task>();
                    foreach (var ding in dings)
                    {
                        tasks.Add(SaveRecordingAsync(ding, filter));
                    }
                    Task.WaitAll(tasks.ToArray());
                }
                else
                {
                    await DownloadSnapshots(filter);
                }

                log.LogInformation("Done!");
                return 0;
            }
            catch(Exception exe)
            {
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
            foreach(var d in devices.Doorbots)
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
            await this.ringSession.GetLatestSnapshot(doorbotId,fileName);
            log.LogInformation($"Downloaded snapshot {fileName}");

            return true;
        }

        internal async Task<bool> SaveRecordingAsync(KoenZomers.Ring.Api.Entities.DoorbotHistoryEvent ding, Filter filter)
        {
            semaphore.Wait();
            try
            {
                string filename = string.Empty;
                var expandedPath = Environment.ExpandEnvironmentVariables(filter.DownloadPath);

                TimeZoneInfo.Local.GetUtcOffset(ding.CreatedAtDateTime.Value);
                var est = ding.CreatedAtDateTime.Value.ToLocalTime();
                filename = Path.Combine(expandedPath,
                    $"{est.Year}-{est.Month.ToString().PadLeft(2, '0')}-{est.Day.ToString().PadLeft(2, '0')}-T{est.Hour.ToString().PadLeft(2, '0')}_{est.Minute.ToString().PadLeft(2, '0')}_{est.Second.ToString().PadLeft(2, '0')}--{ding.Doorbot.Description}.mp4");

                string msg = $"Downloading:{filename}\r\n";
                //msg = msg + $"CreatedAt (UTC): {ding.CreatedAtDateTime}\r\n";
                msg = msg + $"Created At (local): {ding.CreatedAtDateTime.Value.ToLocalTime()}\r\n";
                //msg = msg + $"Answered: {ding.Answered}\r\n";
                msg = msg + $"Id: {ding.Id}\r\n";
                //msg = msg + $"RecordingIsReady: {ding.Recording.Status}\r\n";
                //msg = msg + $"Type: {ding.Kind}\r\n";
                msg = msg + $"Device Name: {ding.Doorbot.Description}\r\n";
                msg = msg + $"--------------";
                log.LogInformation(msg);

                int attempt = 1;
                do
                {
                    attempt++;

                    //log.LogInformation($"{itemCount + 1} - {filename}... ");
                    try
                    {
                        await this.ringSession.GetDoorbotHistoryRecording(ding, filename);
                        log.LogInformation($"Complete - {filename} ({new FileInfo(filename).Length / 1048576} MB)");
                        break;
                    }
                    catch (AggregateException e)
                    {
                        if (e.InnerException != null && e.InnerException.GetType() == typeof(System.Net.WebException) && ((System.Net.WebException)e.InnerException).Response != null)
                        {
                            var webException = (System.Net.WebException)e.InnerException;
                            var response = new StreamReader(webException.Response.GetResponseStream()).ReadToEnd();

                            log.LogError($"Failed ({(e.InnerException != null ? e.InnerException.Message : e.Message)} - {response})");
                        }
                        else
                        {
                            log.LogError($"Failed ({(e.InnerException != null ? e.InnerException.Message : e.Message)})");
                        }
                    }
                    catch (Exception exe)
                    {
                        log.LogError($"Failed ({(exe.InnerException != null ? exe.InnerException.Message : exe.Message)})");
                    }

                    if (attempt >= 10)
                    {
                        log.LogWarning($"Giving up on {filename} after {attempt} tries.");
                        return false;
                    }
                    else
                    {
                        log.LogInformation($"Retrying: {attempt + 1}/10 for {filename}.");
                    }

                } while (attempt < 10);

                return true;
            }
            finally
            {
                semaphore.Release();
            }
        }

    }
}
