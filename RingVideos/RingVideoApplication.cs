using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ring.Models;
using RingVideos.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.Threading;
using System.Text;
using Newtonsoft.Json.Serialization;

namespace RingVideos
{
    public class RingVideoApplication
    {

        private readonly ILogger log;
        private readonly RingClient client;

        public RingVideoApplication(ILogger<RingVideoApplication> logger, RingClient client)
        {
            log = logger;
            this.client = client;
            
        }


        internal async Task<int> Run(Filter filter, Authentication auth)
        {
            this.client.Initialize(auth.UserName, auth.ClearTextPassword).Wait();
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
            if(filter.StartDateTime.HasValue)
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
            if(filter.VideoCount != 10000)
            {
                message.AppendLine($"Max downloads:\t{filter.VideoCount}");
            }
            message.AppendLine($"Only Starred:\t{filter.OnlyStarred}");

            log.LogInformation(message.ToString());

            log.LogInformation("Querying for videos to download...");
            var dings = await client.GetDingsAsync(filter.StartDateTimeUtc,filter.EndDateTimeUtc,filter.VideoCount, filter.OnlyStarred);
            log.LogInformation($"Found {dings.Count} videos to download");

            var tasks =  dings.Select(x => SaveRecordingAsync(x, filter));
            var results = await Task.WhenAll(tasks);
            log.LogInformation("Done!");
            return 0;
        }

        internal async Task<bool> SaveRecordingAsync(Ding ding, Filter filter)
        {
            log.LogInformation($"--------------\r\nDevice: {ding.Device.Type}\r\nCreatedAt (UTC): {ding.CreatedAtUtc}\r\n" +
                $"Created At (local): {ding.CreatedAtLocal}\r\nAnswered: {ding.Answered}\r\nId: {ding.Id}\r\n" +
                         $"RecordingIsReady: {ding.RecordingIsReady}\r\nType: {ding.Type}\r\nDevice Name: {ding.Device.Description}\r\n--------------");
            log.LogDebug($"Getting url for {ding.Id}");
            string filename = string.Empty;
            Uri url = null;
            var expandedPath = Environment.ExpandEnvironmentVariables(filter.DownloadPath);
            try
            {
                url = await client.GetRecordingUriAsync(ding);
                if (url != null)
                {
                    log.LogDebug(url.ToString());
                    var wc = new System.Net.WebClient();
                    TimeZoneInfo.Local.GetUtcOffset(ding.CreatedAtUtc);
                    var est = ding.CreatedAtUtc.AddHours(TimeZoneInfo.Local.GetUtcOffset(ding.CreatedAtUtc).Hours);
                    filename = Path.Combine(expandedPath,
                        $"{est.Year}-{est.Month.ToString().PadLeft(2, '0')}-{est.Day.ToString().PadLeft(2, '0')}-T{est.Hour.ToString().PadLeft(2, '0')}_{est.Minute.ToString().PadLeft(2, '0')}_{est.Second.ToString().PadLeft(2, '0')}--{ding.Device.Description}-{ding.Type}.mp4");

                    Download(url, filename, wc);
                    log.LogInformation($"{filename} -- complete.");
                }
                else
                {
                    log.LogDebug("URL is null");
                    return false;
                }
               

            }
            catch (Exception exe)
            {
                log.LogError(DownloadErrorMessage(ding, filename, url, exe));
                return false;
            }

            return true;
        }

        internal bool Download(Uri url, string filename, WebClient wc, int retry =0)
        {
            try
            {
                log.LogInformation($"Downloading File: {filename}");
                wc.DownloadFile(url, filename);
                return true;
            }
            catch (Exception exe)
            {
                if (retry < 5)
                {
                    log.LogDebug($"Retry download for {filename}: Count {retry}. Reason: {exe.Message}");
                    Thread.Sleep(new Random().Next(5000, 10000));
                    Download(url, filename, wc, retry + 1);
                    return true;
                }
                log.LogError($"Problem downloading file: {filename}\r\n{exe.ToString()}");
            }
            return false;
        }

        private string DownloadErrorMessage(Ding ding, string fileName, Uri url, Exception exe)
        {
            StringBuilder sb = new StringBuilder("Error saving recording:\r\n");
            sb.AppendLine($"Id:{ding.Id}");
            sb.AppendLine($"Filename: {fileName}");
            if (url != null)
            {
                sb.AppendLine($"Url: {url.ToString()}");
            }
            else
            {
                sb.AppendLine($"Url: null");
            }
            sb.AppendLine($"Exception: {exe.ToString()}");

            return sb.ToString();
        }
    }
}
