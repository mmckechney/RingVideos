# Ring Videos Downloader

[![Build Status](https://dev.azure.com/mckechney/RingVideos/_apis/build/status/mmckechney.RingVideos?branchName=master)](https://dev.azure.com/mckechney/RingVideos/_build/latest?definitionId=12&branchName=master)

This simple console app (written in .NET Core) will allow you do perform a bulk download of your [Ring.com](https://www.ring.com) videos.
You can download videos based in a date range and/or if they are starred
## Usage

```
RingVideos:
  Simple command line tool to download videos from your Ring account

Usage:
  RingVideos [options]

Options:
  -u, --username <username>    Ring account username
  -p, --password <password>    Ring account password
  --path <path>                Path to save videos to
  -s, --start <start>          Start time (earliest videos to download)
  -e, --end <end>              End time (latest videos to download)
  --starred                    Flag to only download Starred videos
  -m, --maxcount <maxcount>    Maximum number of videos to download
  --version                    Show version information
  -?, -h, --help               Show help and usage information
```

- Version 1.2: The app will also save your settings to a local config file. This will allow you to just re-run the app with no parameters and have it download the videos since your last run.
- Version 1.3: Updated to leverage the KoenZomers.Ring.Api Nuget package to interact with the Ring API

## Credits
This console app and API library  was based off of:
- [php-ring-api](https://github.com/jeroenmoors/php-ring-api) by [Jeroen Moors](https://github.com/jeroenmoors) and
- [Ring](https://github.com/jonathanpotts/Ring) by [Jonathan Potts](https://github.com/jonathanpotts)
- [Ring Api](https://github.com/KoenZomers/RingApi) by [Koen Zomers](https://github.com/KoenZomers)