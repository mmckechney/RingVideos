
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RingVideos.Models;
using System;
using System.Collections;
using System.Net;
using System.Threading.Tasks;
using System.Linq;

namespace RingVideos
{
    class Program
    {

        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            ConfigureServices(services,args);
            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                RingVideoApplication app = serviceProvider.GetService<RingVideoApplication>();
                Arguments argument = serviceProvider.GetService<Arguments>();
                Filter f = argument.ParseCommandline(args);

                if (f.SetDebug)
                {
                    var logConfig = serviceProvider.GetService<ILoggingBuilder>();
                    logConfig.SetMinimumLevel(LogLevel.Debug);
                }

                SetAuthenticationValues(f);

               // Start up logic here
                app.Run(f).Wait();
            }
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();

        }

        private static void SetAuthenticationValues(Filter f)
        {
            if(string.IsNullOrWhiteSpace(f.UserName))
            {
                var un = System.Environment.GetEnvironmentVariable("RingUsername");
                if(!string.IsNullOrWhiteSpace(un))
                {
                    f.UserName = un;
                }
                else
                {
                    throw new ArgumentException("A Ring username is requires");
                }
            }
            if (string.IsNullOrWhiteSpace(f.Password))
            {
                var pw = System.Environment.GetEnvironmentVariable("RingPassword");
                if (!string.IsNullOrWhiteSpace(pw))
                {
                    f.Password = pw;
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
            else
            {
                services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Information));
            }

            services.AddTransient<RingVideoApplication>()
            .AddTransient<RingClient>()
            .AddTransient<Arguments>();

         
         
        }

       

    }
    
}


