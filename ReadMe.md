# Ring Videos Downloader

[![Build Status](https://github.com/mmckechney/RingVideos/actions/workflows/dotnet.yml/badge.svg)](https://github.com/mmckechney/RingVideos/actions/workflows/dotnet.yml)

This simple console app (written in .NET Core) will allow you do perform a bulk download of your [Ring.com](https://www.ring.com) videos.
You can filter the videos you download by date range, whether they are starred, the event kind (motion / doorbell ring / live-view / alarm), and by what Ring's computer vision detected in the recording (person, vehicle, animal, etc.).

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
  devices     Get list of devices and their ID values
  showlog     Open the log file
  quit        Quit/close the application

Options (for `starred` and `all`):
  -u, --username <username>              Ring account username
  -p, --password <password>              Ring account password
  --path <path>                          Path to save videos to
  -s, --start <start>                    Start time (earliest videos to download) [default: 1/1/0001 12:00:00 AM]
  -e, --end <end>                        End time (latest videos to download) [default: 12/31/9999 11:59:59 PM]
  -m, --maxcount <maxcount>              Maximum number of videos to download [default: 1000]
  --id, --device-id <device-id>          Only download from the specified device (use `devices` to list)
  -P, --person                           Only download videos where a person was detected (shortcut for `--detection-type human`)
  -k, --kind <kind>                      Only download events of this kind: `motion`, `ding`, `on_demand`, `alarm`
  -D, --detection-type <type>            Only download events where Ring CV classified the detection as this type.
                                         Common values: `human`, `vehicle`, `animal`, `package`, `other_motion`
  -x, --exit                             Close the app after running the command (useful for scripting)
  -?, -h, --help                         Show help and usage information
```

### Filter reference

The following filters can be combined (they are AND-ed together):

| Flag | Effect |
| --- | --- |
| `-s` / `--start`, `-e` / `--end` | Restrict to events in a date range. |
| `-m` / `--maxcount` | Cap the total number of videos downloaded. |
| `--id` / `--device-id` | Restrict to a specific camera/doorbell. |
| `starred` (command) | Only events you starred/favorited in the Ring app. |
| `-P` / `--person` | Only events where Ring CV detected a person. |
| `-k` / `--kind` | Only events of a specific kind (`motion`, `ding`, `on_demand`, `alarm`). |
| `-D` / `--detection-type` | Only events whose Ring CV classification matches (e.g. `human`, `vehicle`, `animal`, `package`). |

Notes on detection filters:

- Ring populates `cv_properties` only for events evaluated by its computer vision. Older events, `on_demand` (live view) events, and events from devices without CV may not include this data and will be excluded when `--person` or `--detection-type` is used.
- `--person` is equivalent to `--detection-type human` (but also matches legacy events that only set `person_detected=true` without a detection type).
- Events without a ready recording (e.g. live-view-only events or recordings still processing) are always skipped — previously these would fail at download time.

### Examples

Download every video from the last 24 hours where a person was detected:

```
RingVideos all --person -s "yesterday" --path D:\RingVideos
```

Download only doorbell-button presses (not motion events) from a specific device:

```
RingVideos all -k ding --id 123456789 --path D:\RingVideos
```

Download starred clips where Ring classified the detection as a vehicle:

```
RingVideos starred --detection-type vehicle --path D:\RingVideos
```

## Version history

- Version 3.2: Added event-kind and detection-type filters (`--kind`, `--detection-type`, `--person`). Events without a ready recording are now skipped automatically.
- Version 3.0: Added additional download threading and retry options and (hopefully) better download status reporting in the console.
- Version 2.0: Updated calling syntax to use sub-commands vs flags. Can now also create and download a current snapshot
- Version 1.3: Updated to leverage the KoenZomers.Ring.Api Nuget package to interact with the Ring API
- Version 1.2: The app will also save your settings to a local config file. This will allow you to just re-run the app with no parameters and have it download the videos since your last run.



## Credits
This console app and API library  was based off of:
- [php-ring-api](https://github.com/jeroenmoors/php-ring-api) by [Jeroen Moors](https://github.com/jeroenmoors) and
- [Ring](https://github.com/jonathanpotts/Ring) by [Jonathan Potts](https://github.com/jonathanpotts)
- [Ring Api](https://github.com/KoenZomers/RingApi) by [Koen Zomers](https://github.com/KoenZomers)