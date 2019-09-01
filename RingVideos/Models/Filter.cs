using System;
using System.Collections.Generic;
using System.Text;

namespace RingVideos.Models
{
    class Filter
    {
        public int VideoCount { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public DateTime? StartDateTimeUtc { get; set; }
        public DateTime? EndDateTimeUtc { get; set; }
        public string DownloadPath { get; set; }
        public string TimeZone { get; set; }
        public bool OnlyStarred { get; set; }
        public bool SetDebug { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
