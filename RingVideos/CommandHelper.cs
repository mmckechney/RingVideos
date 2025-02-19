using Spectre.Console;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Linq;


namespace RingVideos
{
    public class CommandHelper
    {
      public CommandHelper()
      {

      }

      public Parser SetupCommands()
      {
         var startOption = new Option<DateTime>(new string[] { "--start", "-s" }, () => DateTime.MinValue, "Start time (earliest videos to download)");
         var endOption = new Option<DateTime>(new string[] { "--end", "-e" }, () => DateTime.MaxValue, "End time (latest videos to download)");
         var pathOption = new Option<string>(new string[] { "--path" },  "Path to save videos to");
         var passwordOption = new Option<string>(new string[] { "--password", "-p" }, "Ring account password");
         var userNameOption = new Option<string>(new string[] { "--username", "-u" }, "Ring account username");
         var starredOption = new Option<bool>(new string[] { "--starred" }, () => false, "Flag to only download Starred videos");
         var maxcountOption = new Option<int>(new string[] { "--maxcount", "-m" }, () => 1000, "Maximum number of videos to download");
         var deviceIdOption = new Option<long>(new string[] { "--device-id", "--id" },  "Device ID to download videos from. (Use command `devices` to get the available list)");
         //var debugLogOption = new Option<bool>(new string[] { "-d", "--debug" }, "Debug log option flag");
         //var traceLogOption = new Option<bool>(new string[] { "-t", "--trace" }, "Trace log option flag");
         var exitAppOption = new Option<bool>(new string[] { "-x", "--exit" }, "Option to close app after running command (vs. keeping open). This could be useful in using in automated scripting. ");
         ;
         RootCommand rootCommand = new RootCommand(description: "Simple command line tool to download videos from your Ring account");

         var starCommand = new Command("starred", "Download only starred videos");
         starCommand.Handler = CommandHandler.Create<string, string, string, DateTime, DateTime, int, long?>(Worker.GetStarredVideos);

         var allCommand = new Command("all", "Download all videos (starred and unstarred)");
         allCommand.Handler = CommandHandler.Create<string, string, string, DateTime, DateTime, int, long?>(Worker.GetAllVideos);

         var snapshotCommand = new Command("snapshot", "Download only snapshot images");
         snapshotCommand.Handler = CommandHandler.Create<string, string, string, DateTime, DateTime, long?>(Worker.GetSnapshotImages);

         var showLogCommand = new Command("showlog", "Open the log file");
         showLogCommand.Handler = CommandHandler.Create(Worker.ShowLog);

         var deviceListCommand = new Command("devices", "Get list of devices and their ID values");
         deviceListCommand.Add(userNameOption);
         deviceListCommand.Add(passwordOption);
         deviceListCommand.Handler = CommandHandler.Create<string, string>(Worker.DeviceList);

         var quitCommand = new Command("quit", "Quit/close the application");
         quitCommand.AddAlias("exit");
         quitCommand.AddAlias("q");
         quitCommand.Handler = CommandHandler.Create(Worker.QuitApplication);

         rootCommand.Add(starCommand);
         rootCommand.Add(allCommand);
         rootCommand.Add(snapshotCommand);
         rootCommand.Add(showLogCommand);
         rootCommand.Add(deviceListCommand);
         rootCommand.Add(exitAppOption);
         rootCommand.Add(quitCommand);
         //rootCommand.AddOption(debugLogOption);
         //rootCommand.AddOption(traceLogOption);

         starCommand.Add(userNameOption);
         starCommand.Add(passwordOption);
         starCommand.Add(pathOption);
         starCommand.Add(startOption);
         starCommand.Add(endOption);
         starCommand.Add(maxcountOption);
         starCommand.Add(deviceIdOption);

         allCommand.Add(userNameOption);
         allCommand.Add(passwordOption);
         allCommand.Add(pathOption);
         allCommand.Add(startOption);
         allCommand.Add(endOption);
         allCommand.Add(maxcountOption);
         allCommand.Add(deviceIdOption);

         snapshotCommand.Add(userNameOption);
         snapshotCommand.Add(passwordOption);
         snapshotCommand.Add(pathOption);
         snapshotCommand.Add(startOption);
         snapshotCommand.Add(endOption);
         snapshotCommand.Add(deviceIdOption);

         var parser = new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseTypoCorrections().UseHelp(ctx =>
                {
                   ctx.HelpBuilder.CustomizeLayout(_ => HelpBuilder.Default
                                      .GetLayout()
                                      .Prepend(
                                          _ => AnsiConsole.Write(new FigletText("Ring Videos"))));
                                      }).Build();

         return parser;
      }

    }
}
