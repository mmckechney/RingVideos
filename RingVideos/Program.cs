
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

namespace RingVideos
{
    class Program
    {
        public static IConfiguration Configuration { get; set; }
   

        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            ConfigureServices(services,args);
            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                RingVideoApplication app = serviceProvider.GetService<RingVideoApplication>();
                Arguments argument = serviceProvider.GetService<Arguments>();
                Authentication auth = Configuration.GetSection("Authentication").Get<Authentication>();
                auth.Decrypt();
                Filter filter = Configuration.GetSection("Filter").Get<Filter>();

                (Filter f, Authentication a) = argument.ParseCommandline(args, auth, filter);

                if (f.SetDebug)
                {
                    var logConfig = serviceProvider.GetService<ILoggingBuilder>();
                    logConfig.SetMinimumLevel(LogLevel.Debug);
                }

                SetAuthenticationValues(a);

               // Start up logic here
                app.Run(f,a).Wait();

                SaveSettings(f, a);
            }
        }

        private static void SaveSettings(Filter f, Authentication a)
        {

            a.Encrypt();
            //Set "next dates" on filter
            f.StartDateTime = f.EndDateTime.Value.AddDays(-1);
            f.EndDateTime = null;

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

        private static void SetAuthenticationValues(Authentication a)
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

     
            services.AddTransient<RingVideoApplication>()
            .AddTransient<RingClient>()
            .AddTransient<Arguments>();

         
         
        }

       

    }
    
}


