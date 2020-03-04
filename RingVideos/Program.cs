
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RingVideos.Models;
using System;
using System.Collections;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.Configuration.Json;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks.Sources;
using System.Text;
using System.Net.Security;
using KoenZomers.Ring.Api;
using System.CommandLine;
using System.CommandLine.Invocation;

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
            var startOption = new Option(new string[] { "--start", "-s" }, "Start time (earliest videos to download)")
            {
                Argument = new Argument<DateTime>("start", () => DateTime.MinValue),
                Required = false
            };

            var endOption = new Option(new string[] { "--end", "-e" }, "End time (latest videos to download)")
            {
                Argument = new Argument<DateTime>("end", () => DateTime.MaxValue),
                Required = false
            };

            var pathOption = new Option(new string[] { "--path" }, "Path to save videos to")
            {
                Argument = new Argument<string>("path", () => string.Empty),
                Required = false
            };

            var passwordOption = new Option(new string[] { "--password", "-p" }, "Ring account password")
            {
                Argument = new Argument<string>("password", () => string.Empty),
                Required = false
            };

            var userNameOption = new Option(new string[] { "--username", "-u" }, "Ring account username")
            {
                Argument = new Argument<string>("username", () => string.Empty),
                Required = false
            };

            var starredOption = new Option(new string[] { "--starred" }, "Flag to only download Starred videos")
            {
                Argument = new Argument<bool>("starred", () => false),
                Required = false
            };

            var maxcountOption = new Option(new string[] { "--maxcount", "-m" }, "Maximum number of videos to download")
            {
                Argument = new Argument<int>("maxcount", () => 1000),
                Required = false
            };
            RootCommand rootCommand = new RootCommand(description: "Simple command line tool to download videos from your Ring account")
            {
                Handler = CommandHandler.Create<string, string, string, DateTime, DateTime, bool, int>(GetVideos)
            };
            rootCommand.Add(userNameOption);
            rootCommand.Add(passwordOption);
            rootCommand.Add(pathOption);
            rootCommand.Add(startOption);
            rootCommand.Add(endOption);
            rootCommand.Add(starredOption);
            rootCommand.Add(maxcountOption);
           

            var services = new ServiceCollection();
            ConfigureServices(services,args);
            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                app = serviceProvider.GetService<RingVideoApplication>();
                auth = Configuration.GetSection("Authentication").Get<Authentication>();
                auth.Decrypt();
                filter = Configuration.GetSection("Filter").Get<Filter>();

                Task<int> val = rootCommand.InvokeAsync(args);
                val.Wait();

                SaveSettings(Program.filter, Program.auth, val.Result);
            }
        }

        private static void SaveSettings(Filter f, Authentication a, int runResult)
        {

            a.Encrypt();
            //Set "next dates" on filter
            if (runResult == 0)
            {
                f.StartDateTime = f.EndDateTime.Value.AddDays(-1);
                f.EndDateTime = null;
            }

            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"RingVideos");
            if(!Directory.Exists(folder))
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

        private static void SetAuthenticationValues(ref Authentication a)
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
                    throw new ArgumentException("A Ring username is requires");
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
                    throw new ArgumentException("A Ring password is requires");
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
        }

        private static void ConfigureServices(ServiceCollection services, string[] args)
        {
            if (args.Any(a => a.ToLower().EndsWith("-d") || a.ToLower().EndsWith("-debug")))
            {
                services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Debug));
            }
            else if(args.Any(a => a.ToLower().EndsWith("-t") || a.ToLower().EndsWith("-trace")))
            {
                services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Trace));
            }
            else
            {
                services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Information));
            }

            Configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"RingVideos\RingVideosConfig.json"), optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();


            services.AddTransient<RingVideoApplication>();
           // .AddTransient<Arguments>();
        }

        private static async Task<int> GetVideos(string username, string password, string path, DateTime start, DateTime end, bool starred, int maxcount)
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
                filter.EndDateTime = start;
            }
            if(starred)
            {
                filter.OnlyStarred = starred;
            }
            if (maxcount != 1000)
            {
                filter.VideoCount = maxcount;
            }
            if (filter.StartDateTime.HasValue)
            {
                filter.StartDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(filter.StartDateTime.Value, TimeZoneInfo.Local);
            }
            if (filter.EndDateTime.HasValue)
            {
                filter.EndDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(filter.EndDateTime.Value, TimeZoneInfo.Local);
            }

            if (Program.filter.SetDebug)
            {
                var logConfig = serviceProvider.GetService<ILoggingBuilder>();
                logConfig.SetMinimumLevel(LogLevel.Debug);
            }

            SetAuthenticationValues(ref Program.auth);

            return await app.Run(Program.filter, Program.auth);

        }
       

    }
    
}


