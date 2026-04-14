using Spectre.Console;
using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;


namespace RingVideos
{
    public class CommandHelper
    {
      public CommandHelper()
      {

      }

      public RootCommand SetupCommands()
      {
         var startOption = new Option<DateTime>("--start", "-s") { Description = "Start time (earliest videos to download)", DefaultValueFactory = _ => DateTime.MinValue };
         var endOption = new Option<DateTime>("--end", "-e") { Description = "End time (latest videos to download)", DefaultValueFactory = _ => DateTime.MaxValue };
         var pathOption = new Option<string>("--path") { Description = "Path to save videos to" };
         var passwordOption = new Option<string>("--password", "-p") { Description = "Ring account password" };
         var userNameOption = new Option<string>("--username", "-u") { Description = "Ring account username" };
         var maxcountOption = new Option<int>("--maxcount", "-m") { Description = "Maximum number of videos to download", DefaultValueFactory = _ => 1000 };
         var deviceIdOption = new Option<long>("--device-id", "--id") { Description = "Device ID to download videos from. (Use command `devices` to get the available list)" };
         var exitAppOption = new Option<bool>("-x", "--exit") { Description = "Option to close app after running command (vs. keeping open). This could be useful in using in automated scripting." };

         RootCommand rootCommand = new RootCommand(description: "Simple command line tool to download videos from your Ring account");

         var starCommand = new Command("starred", "Download only starred videos");
         starCommand.SetAction(async (parseResult, ct) =>
         {
            return await Worker.GetStarredVideos(
               parseResult.GetValue(userNameOption),
               parseResult.GetValue(passwordOption),
               parseResult.GetValue(pathOption),
               parseResult.GetValue(startOption),
               parseResult.GetValue(endOption),
               parseResult.GetValue(maxcountOption),
               GetNullableLong(parseResult, deviceIdOption));
         });

         var allCommand = new Command("all", "Download all videos (starred and unstarred)");
         allCommand.SetAction(async (parseResult, ct) =>
         {
            return await Worker.GetAllVideos(
               parseResult.GetValue(userNameOption),
               parseResult.GetValue(passwordOption),
               parseResult.GetValue(pathOption),
               parseResult.GetValue(startOption),
               parseResult.GetValue(endOption),
               parseResult.GetValue(maxcountOption),
               GetNullableLong(parseResult, deviceIdOption));
         });

         var snapshotCommand = new Command("snapshot", "Download only snapshot images");
         snapshotCommand.SetAction(async (parseResult, ct) =>
         {
            return await Worker.GetSnapshotImages(
               parseResult.GetValue(userNameOption),
               parseResult.GetValue(passwordOption),
               parseResult.GetValue(pathOption),
               parseResult.GetValue(startOption),
               parseResult.GetValue(endOption),
               GetNullableLong(parseResult, deviceIdOption));
         });

         var showLogCommand = new Command("showlog", "Open the log file");
         showLogCommand.SetAction((parseResult) =>
         {
            Worker.ShowLog();
         });

         var deviceListCommand = new Command("devices", "Get list of devices and their ID values");
         deviceListCommand.Add(userNameOption);
         deviceListCommand.Add(passwordOption);
         deviceListCommand.SetAction(async (parseResult, ct) =>
         {
            await Worker.DeviceList(
               parseResult.GetValue(userNameOption),
               parseResult.GetValue(passwordOption));
         });

         var quitCommand = new Command("quit", "Quit/close the application");
         quitCommand.Aliases.Add("exit");
         quitCommand.Aliases.Add("q");
         quitCommand.SetAction((parseResult) =>
         {
            Worker.QuitApplication();
         });

         rootCommand.Add(starCommand);
         rootCommand.Add(allCommand);
         rootCommand.Add(snapshotCommand);
         rootCommand.Add(showLogCommand);
         rootCommand.Add(deviceListCommand);
         rootCommand.Add(quitCommand);

         starCommand.Add(userNameOption);
         starCommand.Add(passwordOption);
         starCommand.Add(pathOption);
         starCommand.Add(startOption);
         starCommand.Add(endOption);
         starCommand.Add(maxcountOption);
         starCommand.Add(deviceIdOption);
         starCommand.Add(exitAppOption);

         allCommand.Add(userNameOption);
         allCommand.Add(passwordOption);
         allCommand.Add(pathOption);
         allCommand.Add(startOption);
         allCommand.Add(endOption);
         allCommand.Add(maxcountOption);
         allCommand.Add(deviceIdOption);
         allCommand.Add(exitAppOption);

         snapshotCommand.Add(userNameOption);
         snapshotCommand.Add(passwordOption);
         snapshotCommand.Add(pathOption);
         snapshotCommand.Add(startOption);
         snapshotCommand.Add(endOption);
         snapshotCommand.Add(deviceIdOption);
         snapshotCommand.Add(exitAppOption);

         deviceListCommand.Add(exitAppOption);

         return rootCommand;
      }

      /// <summary>
      /// Returns null when the device-id option was not specified (value is default 0), otherwise returns the parsed value.
      /// </summary>
      private static long? GetNullableLong(ParseResult parseResult, Option<long> option)
      {
         var result = parseResult.GetResult(option);
         if (result is null)
            return null;

         return parseResult.GetValue(option);
      }
    }
}
