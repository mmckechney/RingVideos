
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RingVideos.Models;
using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;

namespace RingVideos
{
    class Program
    {
        public static IConfiguration Configuration { get; set; }
        public static Authentication auth;
        public static Filter filter;
        public static ServiceProvider serviceProvider = null;
        public static RingVideoApplication app;

        static void Main(string[] args)
        {
            var startOption = new Option<DateTime>(new string[] { "--start", "-s" }, () => DateTime.MinValue, "Start time (earliest videos to download)");
            var endOption = new Option<DateTime>(new string[] { "--end", "-e" }, () => DateTime.MaxValue, "End time (latest videos to download)");
            var pathOption = new Option<string>(new string[] { "--path" }, () => string.Empty, "Path to save videos to");
            var passwordOption = new Option<string>(new string[] { "--password", "-p" }, () => string.Empty, "Ring account password");
            var userNameOption = new Option<string>(new string[] { "--username", "-u" }, () => string.Empty, "Ring account username");
            var starredOption = new Option<bool>(new string[] { "--starred" }, () => false, "Flag to only download Starred videos");
            var maxcountOption = new Option<int>(new string[] { "--maxcount", "-m" }, () => 1000, "Maximum number of videos to download");
      ;
            RootCommand rootCommand = new RootCommand(description: "Simple command line tool to download videos from your Ring account");

            var starCommand = new Command("starred", "Download only starred videos");
            starCommand.Handler = CommandHandler.Create<string, string, string, DateTime, DateTime, int>(GetStarredVideos);

            var allCommand = new Command("all", "Download all videos (starred and unstarred)");
            allCommand.Handler = CommandHandler.Create<string, string, string, DateTime, DateTime, int>(GetAllVideos);

            var snapshotCommand = new Command("snapshot", "Download only snapshot images");
            snapshotCommand.Handler = CommandHandler.Create<string, string, string, DateTime, DateTime>(GetSnapshotImages);

            rootCommand.Add(starCommand);
            rootCommand.Add(allCommand);
            rootCommand.Add(snapshotCommand);

            starCommand.Add(userNameOption);
            starCommand.Add(passwordOption);
            starCommand.Add(pathOption);
            starCommand.Add(startOption);
            starCommand.Add(endOption);
            starCommand.Add(maxcountOption);

            allCommand.Add(userNameOption);
            allCommand.Add(passwordOption);
            allCommand.Add(pathOption);
            allCommand.Add(startOption);
            allCommand.Add(endOption);
            allCommand.Add(maxcountOption);

            snapshotCommand.Add(userNameOption);
            snapshotCommand.Add(passwordOption);
            snapshotCommand.Add(pathOption);
            snapshotCommand.Add(startOption);
            snapshotCommand.Add(endOption);


            var services = new ServiceCollection();
            ConfigureServices(services,args);
            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                app = serviceProvider.GetService<RingVideoApplication>();
                auth = Configuration.GetSection("Authentication").Get<Authentication>();
                if (auth != null)
                {
                    auth.Decrypt();
                }else
                {
                    auth = new Authentication();
                }
                filter = Configuration.GetSection("Filter").Get<Filter>();
                if(filter == null)
                {
                    filter = new Filter();
                }

                Task<int> val = rootCommand.InvokeAsync(args);
                val.Wait();

                SaveSettings(Program.filter, Program.auth, val.Result);
            }
        }

        private static void SaveSettings(Filter f, Authentication a, int runResult)
        {
            if (f != null && a != null){
                a.Encrypt();
                //Set "next dates" on filter
                if (runResult == 0 && !f.Snapshots)
                {
                    f.StartDateTime = f.EndDateTime.Value.AddDays(-1);
                    f.EndDateTime = null;
                }

                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"RingVideos");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"RingVideos\RingVideosConfig.json");
                var serializeOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var conf = new Config()
                {
                    Authentication = a,
                    Filter = f
                };
                var config = JsonSerializer.Serialize(conf, serializeOptions);

                File.WriteAllText(settingsFile, config);
            }
        }

        private static bool SetAuthenticationValues(ref Authentication a)
        {
            if(string.IsNullOrWhiteSpace(a.UserName))
            {
                var un = System.Environment.GetEnvironmentVariable("RingUsername");
                if(!string.IsNullOrWhiteSpace(un))
                {
                    a.UserName = un;
                }
                else
                {
                    Console.WriteLine("A Ring username is required");
                    return false;
                }
            }
            if (string.IsNullOrWhiteSpace(a.ClearTextPassword))
            {
                var pw = System.Environment.GetEnvironmentVariable("RingPassword");
                if (!string.IsNullOrWhiteSpace(pw))
                {
                    a.ClearTextPassword = pw;
                }
                else
                {
                    Console.WriteLine("A Ring password is required");
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(a.RefreshToken))
            {
                var rt = System.Environment.GetEnvironmentVariable("RefreshToken");
                if (!string.IsNullOrWhiteSpace(rt))
                {
                    a.RefreshToken = rt;
                }
            }
            return true;
        }

        private static void ConfigureServices(ServiceCollection services, string[] args)
        {
            var level = LogLevel.Information;
            
            if (args.Any(a => a.ToLower().EndsWith("-d") || a.ToLower().EndsWith("-debug")))
            {
                level = LogLevel.Debug;
            }
            else if(args.Any(a => a.ToLower().EndsWith("-t") || a.ToLower().EndsWith("-trace")))
            {
                level = LogLevel.Trace;
            }
            
            services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(level));
            

            Configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"RingVideos\RingVideosConfig.json"), optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();


            services.AddTransient<RingVideoApplication>();
           // .AddTransient<Arguments>();
        }


        private static async Task<int> GetSnapshotImages(string username, string password, string path, DateTime start, DateTime end)
        {
            return await GetVideos(username, password, path, start, end, false,true, 1000);
        }

        private static async Task<int> GetAllVideos(string username, string password, string path, DateTime start, DateTime end, int maxcount)
        {
            return await GetVideos(username, password, path, start, end, false,false, maxcount);
        }
        private static async Task<int> GetStarredVideos(string username, string password, string path, DateTime start, DateTime end, int maxcount)
        {
            return await GetVideos(username, password, path, start, end, true, false, maxcount);
        }
        private static async Task<int> GetVideos(string username, string password, string path, DateTime start, DateTime end, bool starred, bool snapshot, int maxcount)
        {
            if(!string.IsNullOrEmpty(username))
            {
                auth.UserName = username;
            }
            if (!string.IsNullOrEmpty(password))
            {
                auth.ClearTextPassword = password;
            }
            if (!string.IsNullOrEmpty(path))
            {
                filter.DownloadPath = path;
            }
            if (start != DateTime.MinValue)
            {
                filter.StartDateTime = start;
            }
            if (end != DateTime.MaxValue)
            {
                filter.EndDateTime = end;
            }
            else
            {
                filter.EndDateTime = DateTime.Now;
            }
            if(starred)
            {
                filter.OnlyStarred = starred;
            }
            if (snapshot)
            {
                filter.Snapshots = snapshot;
            }
            if (maxcount != 1000)
            {
                filter.VideoCount = maxcount;
            }
            if (filter.StartDateTime.HasValue)
            {
                filter.StartDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(filter.StartDateTime.Value, TimeZoneInfo.Local);
            }
            else
            {
                filter.StartDateTime = DateTime.MinValue;
                filter.StartDateTimeUtc = DateTime.MinValue;
            }
            if (filter.EndDateTime.HasValue)
            {
                filter.EndDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(filter.EndDateTime.Value, TimeZoneInfo.Local);
            }
            else
            {
                filter.EndDateTime = DateTime.MaxValue;
                filter.EndDateTimeUtc = DateTime.MaxValue;
            }

            if (Program.filter.SetDebug)
            {
                var logConfig = serviceProvider.GetService<ILoggingBuilder>();
                logConfig.SetMinimumLevel(LogLevel.Debug);
            }

            if (SetAuthenticationValues(ref Program.auth))
            {
                return await app.Run(Program.filter, Program.auth);
            }
            else
            {
                return -200;
            }
            

        }
    

    }
    
}


