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
//using KoenZomers.Ring.Api.Entities;

namespace RingVideos
{
    public class RingVideoApplication
    {

        private readonly ILogger log;
        //private readonly RingClient client;
        private Session ringSession;
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

                session = Session.GetSessionByRefreshToken(auth.ClearTextRefreshToken).Result;
            }
            else
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
                if (this.ringSession == null)
                {
                    return 999;
                }
                //this.client.Initialize(auth.UserName, auth.ClearTextPassword).Wait();
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

                log.LogInformation(message.ToString());

                log.LogInformation("Querying for videos to download...");
                var dings = await ringSession.GetDoorbotsHistory(filter.StartDateTimeUtc.Value, filter.EndDateTimeUtc);
                if (filter.OnlyStarred)
                {
                    dings = dings.Where(d => d.Favorite == true).ToList();
                }
                //var dings = await client.GetDingsAsync(filter.StartDateTimeUtc,filter.EndDateTimeUtc,filter.VideoCount, filter.OnlyStarred);
                log.LogInformation($"Found {dings.Count()} videos to download");

                var tasks =  dings.Select(x => SaveRecordingAsync(x, filter));
                var results = await Task.WhenAll(tasks);

                //foreach (var d in dings)
                //{
                //    await SaveRecordingAsync(d, filter);
                //}
                log.LogInformation("Done!");
                return 0;
            }catch(Exception exe)
            {
                log.LogError(exe.ToString());
                return -1;
            }
        }

        internal async Task<bool> SaveRecordingAsync(KoenZomers.Ring.Api.Entities.DoorbotHistoryEvent ding, Filter filter)
        {
            log.LogInformation($"--------------\r\nDevice: {ding.Doorbot.Kind}\r\nCreatedAt (UTC): {ding.CreatedAtDateTime}\r\n" +
                $"Created At (local): {ding.CreatedAtDateTime.Value.ToLocalTime()}\r\nAnswered: {ding.Answered}\r\nId: {ding.Id}\r\n" +
                         $"RecordingIsReady: {ding.Recording.Status}\r\nType: {ding.Kind}\r\nDevice Name: {ding.Doorbot.Description}\r\n--------------");
            //log.LogDebug($"Getting url for {ding.Id}");
            string filename = string.Empty;
            var expandedPath = Environment.ExpandEnvironmentVariables(filter.DownloadPath);
            int attempt = 1, itemCount = 0;

            TimeZoneInfo.Local.GetUtcOffset(ding.CreatedAtDateTime.Value);
            var est = ding.CreatedAtDateTime.Value.AddHours(TimeZoneInfo.Local.GetUtcOffset(ding.CreatedAtDateTime.Value).Hours);
            filename = Path.Combine(expandedPath,
                $"{est.Year}-{est.Month.ToString().PadLeft(2, '0')}-{est.Day.ToString().PadLeft(2, '0')}-T{est.Hour.ToString().PadLeft(2, '0')}_{est.Minute.ToString().PadLeft(2, '0')}_{est.Second.ToString().PadLeft(2, '0')}--{ding.Doorbot.Description}-{ding.Doorbot.Kind}.mp4");

            do
            {
                attempt++;

                log.LogInformation($"{itemCount + 1} - {filename}... ");
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

                if (attempt >=10)
                {
                    log.LogWarning("Giving up.");
                    return false;
                }
                else
                {
                    log.LogInformation($"Retrying {attempt + 1}/10.");
                }

            } while (attempt < 10);
            return true;
        }

        //internal async Task<bool> SaveRecordingAsync(Ding ding, Filter filter)
        //{
        //    log.LogInformation($"--------------\r\nDevice: {ding.Device.Type}\r\nCreatedAt (UTC): {ding.CreatedAtUtc}\r\n" +
        //        $"Created At (local): {ding.CreatedAtLocal}\r\nAnswered: {ding.Answered}\r\nId: {ding.Id}\r\n" +
        //                 $"RecordingIsReady: {ding.RecordingIsReady}\r\nType: {ding.Type}\r\nDevice Name: {ding.Device.Description}\r\n--------------");
        //    log.LogDebug($"Getting url for {ding.Id}");
        //    string filename = string.Empty;
        //    Uri url = null;
        //    var expandedPath = Environment.ExpandEnvironmentVariables(filter.DownloadPath);
        //    try
        //    {
        //        url = await client.GetRecordingUriAsync(ding);
        //        if (url != null)
        //        {
        //            log.LogDebug(url.ToString());
        //            var wc = new System.Net.WebClient();
        //            TimeZoneInfo.Local.GetUtcOffset(ding.CreatedAtUtc);
        //            var est = ding.CreatedAtUtc.AddHours(TimeZoneInfo.Local.GetUtcOffset(ding.CreatedAtUtc).Hours);
        //            filename = Path.Combine(expandedPath,
        //                $"{est.Year}-{est.Month.ToString().PadLeft(2, '0')}-{est.Day.ToString().PadLeft(2, '0')}-T{est.Hour.ToString().PadLeft(2, '0')}_{est.Minute.ToString().PadLeft(2, '0')}_{est.Second.ToString().PadLeft(2, '0')}--{ding.Device.Description}-{ding.Type}.mp4");

        //            Download(url, filename, wc);
        //            log.LogInformation($"{filename} -- complete.");
        //        }
        //        else
        //        {
        //            log.LogDebug("URL is null");
        //            return false;
        //        }
               

        //    }
        //    catch (Exception exe)
        //    {
        //        log.LogError(DownloadErrorMessage(ding, filename, url, exe));
        //        return false;
        //    }

        //    return true;
        //}

        //internal bool Download(Uri url, string filename, WebClient wc, int retry =0)
        //{
        //    try
        //    {
        //        log.LogInformation($"Downloading File: {filename}");
        //        wc.DownloadFile(url, filename);
        //        return true;
        //    }
        //    catch (Exception exe)
        //    {
        //        if (retry < 5)
        //        {
        //            log.LogDebug($"Retry download for {filename}: Count {retry}. Reason: {exe.Message}");
        //            Thread.Sleep(new Random().Next(5000, 10000));
        //            Download(url, filename, wc, retry + 1);
        //            return true;
        //        }
        //        log.LogError($"Problem downloading file: {filename}\r\n{exe.ToString()}");
        //    }
        //    return false;
        //}

        //private string DownloadErrorMessage(Ding ding, string fileName, Uri url, Exception exe)
        //{
        //    StringBuilder sb = new StringBuilder("Error saving recording:\r\n");
        //    sb.AppendLine($"Id:{ding.Id}");
        //    sb.AppendLine($"Filename: {fileName}");
        //    if (url != null)
        //    {
        //        sb.AppendLine($"Url: {url.ToString()}");
        //    }
        //    else
        //    {
        //        sb.AppendLine($"Url: null");
        //    }
        //    sb.AppendLine($"Exception: {exe.ToString()}");

        //    return sb.ToString();
        //}
    }
}
