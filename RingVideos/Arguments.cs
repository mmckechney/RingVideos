using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RingVideos.Models;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace RingVideos
{
    /// <summary>
    /// Arguments class
    /// /*
    ///* Arguments class: application arguments interpreter
    ///*
    ///* Authors:		R. LOPES
    ///* Contributors:	R. LOPES
    ///* Created:		25 October 2002
    ///* Modified:		28 October 2002
    ///*
    ///* Version:		1.0
    ///*/
    /// </summary>
    public class Arguments
    {
        private ILogger log;
        public Arguments(ILogger<Arguments> logger)
        {
            log = logger;
        }
        public StringDictionary ParseArguments(string[] Args)
        {
            StringDictionary Parameters = new StringDictionary();
            Regex Spliter = new Regex(@"^-{1,2}|^/|=", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            Regex Remover = new Regex(@"^['""]?(.*?)['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            string Parameter = null;
            string[] Parts;

            // Valid parameters forms:
            // {-,/,--}param{ ,=,:}((",')value(",'))
            // Examples: -param1 value1 --param2 /param3:"Test-:-work" /param4=happy -param5 '--=nice=--'
            foreach (string Txt in Args)
            {
                // Look for new parameters (-,/ or --) and a possible enclosed value (=,:)
                Parts = Spliter.Split(Txt, 3);
                switch (Parts.Length)
                {
                    // Found a value (for the last parameter found (space separator))
                    case 1:
                        if (Parameter != null)
                        {
                            if (!Parameters.ContainsKey(Parameter))
                            {
                                Parts[0] = Remover.Replace(Parts[0], "$1");
                                Parameters.Add(Parameter, Parts[0]);
                            }
                            Parameter = null;
                        }
                        // else Error: no parameter waiting for a value (skipped)
                        break;
                    // Found just a parameter
                    case 2:
                        // The last parameter is still waiting. With no value, set it to true.
                        if (Parameter != null)
                            if (!Parameters.ContainsKey(Parameter))
                                Parameters.Add(Parameter, "true");

                        Parameter = Parts[1];
                        break;
                    // Parameter with enclosed value
                    case 3:
                        // The last parameter is still waiting. With no value, set it to true.
                        if (Parameter != null)
                        {
                            if (!Parameters.ContainsKey(Parameter))
                                Parameters.Add(Parameter, "true");
                        }
                        Parameter = Parts[1].ToLowerInvariant();
                        // Remove possible enclosing characters (",')
                        if (!Parameters.ContainsKey(Parameter))
                        {
                            Parts[2] = Remover.Replace(Parts[2], "$1");
                            Parameters.Add(Parameter, Parts[2]);
                        }
                        Parameter = null;
                        break;
                }
            }
            // In case a parameter is still waiting
            if (Parameter != null)
            {
                if (!Parameters.ContainsKey(Parameter)) Parameters.Add(Parameter, "true");
            }

            return Parameters;
        }


        internal (Filter, Authentication) ParseCommandline(string[] args, Authentication a, Filter f)
        {
            var dict = ParseArguments(args);
 
            foreach (var key in dict.Keys)
            {
                switch (key.ToString().ToLower())
                {
                    case "s":
                    case "start":
                        DateTime startTime;
                        if (DateTime.TryParse(dict[key.ToString()], out startTime))
                        {
                            f.StartDateTime = startTime;
                        }
                        else
                        {   if (!f.StartDateTime.HasValue)
                            {
                                throw new ArgumentException("Unable to set --start value. Please make sure it is in quotes and the format \"YYYY-MM-dd HH:mm\"");
                            }
                        }
                        break;
                    case "e":
                    case "end":
                        DateTime endTime;
                        if (DateTime.TryParse(dict[key.ToString()], out endTime))
                        {
                            f.EndDateTime = endTime;
                        }
                        else
                        {
                            f.EndDateTime = DateTime.Today.AddDays(1).AddSeconds(-1);
                        }
                        break;
                    case "p":
                    case "path":
                        f.DownloadPath = dict[key.ToString()];
                        break;

                    case "c":
                    case "count":
                    case "max":
                        int limit;
                        if (Int32.TryParse(dict[key.ToString()], out limit))
                        {
                            f.VideoCount = limit;
                        }else
                        {
                            throw new ArgumentException("Unable to set --max value. Please make sure it is an integer value");
                        }

                        break;
                    case "timezone":
                    case "tz":
                       f.TimeZone = dict[key.ToString()];
                        break;
                    case "starred":
                        f.OnlyStarred = true;
                        break;
                    case "username":
                        a.UserName = dict[key.ToString()];
                        break;
                    case "password":
                        a.ClearTextPassword = dict[key.ToString()];
                        break;
                    case "debug:":
                    case "d":
                    case "trace":
                    case "t":
                        //can ignore, these are used in initial services configuration
                        break;
                    default:
                        log.LogInformation($"Unrecognized argument: {key.ToString()}={dict[key.ToString()]}");
                        break;

                }
            }

            if (string.IsNullOrWhiteSpace(f.TimeZone))
            {
                f.TimeZone = string.Empty;
            }

            TimeZoneInfo tzInf;
            switch (f.TimeZone.ToLower())
            {
                case "est":
                    tzInf = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                     break;
                case "pst":
                    tzInf = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
                    break;
                case "mst":
                    tzInf = TimeZoneInfo.FindSystemTimeZoneById(" US Mountain Standard Time");
                    break;
                case "cst":
                    tzInf = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                    break;
                case "utc":
                case "gmt":
                    tzInf = TimeZoneInfo.Utc;
                    break;
                default:
                    tzInf = TimeZoneInfo.Local;
                    break;
            }

            //Convert the dates to UTC
            if (f.StartDateTime.HasValue)
            {
                f.StartDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(f.StartDateTime.Value, tzInf);
            }
            if (f.EndDateTime.HasValue)
            {
                f.EndDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(f.EndDateTime.Value, tzInf);
            }

            if(f.VideoCount == 0)
            {
                f.VideoCount = 10000;
            }


            return (f, a);

        }
    }
}