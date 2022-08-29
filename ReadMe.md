# Ring Videos Downloader

[![Build Status](https://github.com/mmckechney/RingVideos/actions/workflows/dotnet.yml/badge.svg)](https://github.com/mmckechney/RingVideos/actions/workflows/dotnet.yml)

This simple console app (written in .NET Core) will allow you do perform a bulk download of your [Ring.com](https://www.ring.com) videos.
You can download videos based in a date range and/or if they are starred
## Usage

```
RingVideos:
  Simple command line tool to download videos from your Ring account

Usage:
  RingVideos [options] [command]

Options:
  --version         Show version information
  -?, -h, --help    Show help and usage information

Commands:
  starred     Download only starred videos
  all         Download all videos (starred and unstarred)
  snapshot    Download only snapshot images

Options:
  -u, --username <username>    Ring account username [default: ]
  -p, --password <password>    Ring account password [default: ]
  --path <path>                Path to save videos to [default: ]
  -s, --start <start>          Start time (earliest videos to download) [default: 1/1/0001 12:00:00 AM]
  -e, --end <end>              End time (latest videos to download) [default: 12/31/9999 11:59:59 PM]
  -m, --maxcount <maxcount>    Maximum number of videos to download [default: 1000]
  -?, -h, --help               Show help and usage information
```

- Version 1.2: The app will also save your settings to a local config file. This will allow you to just re-run the app with no parameters and have it download the videos since your last run.
- Version 1.3: Updated to leverage the KoenZomers.Ring.Api Nuget package to interact with the Ring API
- Version 2.0: Updated calling syntax to use sub-commands vs flags. Can now also create and download a current snapshot

## Credits
This console app and API library  was based off of:
- [php-ring-api](https://github.com/jeroenmoors/php-ring-api) by [Jeroen Moors](https://github.com/jeroenmoors) and
- [Ring](https://github.com/jonathanpotts/Ring) by [Jonathan Potts](https://github.com/jonathanpotts)
- [Ring Api](https://github.com/KoenZomers/RingApi) by [Koen Zomers](https://github.com/KoenZomers)