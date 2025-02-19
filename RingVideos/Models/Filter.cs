using System;
using System.Text.Json.Serialization;

namespace RingVideos.Models
{

   public class Filter
   {
      public int VideoCount { get; set; } = 10000;
      public DateTime? StartDateTime { get; set; }
      public DateTime? EndDateTime { get; set; } = DateTime.Today.AddDays(1).AddSeconds(-1);

      private DateTime? _startDateTimeUtc = null;
      [JsonIgnore]
      public DateTime? StartDateTimeUtc
      {
         get
         {
            if (StartDateTime.HasValue)
            {
               return TimeZoneInfo.ConvertTimeToUtc(StartDateTime.Value, TimeZoneInfo.Local);
            }
            else
            {
               return DateTime.MaxValue;
            }

         }
      }

      [JsonIgnore]
      public DateTime? EndDateTimeUtc {
         get
         {
            if (EndDateTime.HasValue)
            {
                return TimeZoneInfo.ConvertTimeToUtc(EndDateTime.Value, TimeZoneInfo.Local);
            }
            else
            {
               return DateTime.MaxValue;
            }
         }
      }
      public string DownloadPath { get; set; }
      public string TimeZone { get; set; }
      [JsonIgnore]
      public bool OnlyStarred { get; set; } = false;
      public bool SetDebug { get; set; } = false;
      [JsonIgnore]
      public bool Snapshots { get; set; } = false;
      public DateTime? SnapshotsStartDateTime { get; set; }
      public DateTime? SnapshotsEndDateTime { get; set; } = DateTime.Today.AddDays(1).AddSeconds(-1);

      public long? DeviceId { get; set; }

   }

   internal class Config
   {
      public Authentication Authentication { get; set; }
      public Filter Filter { get; set; }
   }
}

