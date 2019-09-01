# Ring Videos Downloader
This simple console app (written in .NET Core) will allow you do perform a bulk download of your [Ring.com](https://www.ring.com) videos.
You can download videos based in a date range and/or if they are starred
## Usage

`RingVideos --path, -p "<path to download>" [options]`
```
options:
    --start, -s         DateTime value for oldest videos to download. (ex: "2019-07-01 16:00")
    --end, -e           DateTime value for the newest videos to download(ex: "2019-70-02 4:00 PM")
    --max, --count, -c  The maximum count of videos to download
    --timezone, -tz     If you are referencing the videos in a timezone other than your local system timezone
                        Accepted values -- US Timezones: EST, CST, MST, PST or GMT/UTC
    --starred           Only download starred videos
    --username          Your Ring username. Note: You can also save this an an environment variable: RingUsername
    --password          Your Ring password. Note: You can also save this an an environment variable: RingPassword
```


## Credits
This console app and API library  was based off of:
 [php-ring-api](https://github.com/jeroenmoors/php-ring-api) by [Jeroen Moors](https://github.com/jeroenmoors) and
 [Ring](https://github.com/jonathanpotts/Ring) by [Jonathan Potts](https://github.com/jonathanpotts)