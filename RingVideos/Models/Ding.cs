using System;

namespace Ring.Models
{
    /// <summary>
    /// A model representing a ding.
    /// </summary>
    public class Ding
    {
        /// <summary>
        /// The ID of the ding.
        /// </summary>
        public ulong Id { get; set; }
        /// <summary>
        /// The time that the ding occurred.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>
        /// Represents if the ding was answered.
        /// </summary>
        public bool Answered { get; set; }
        /// <summary>
        /// Represents if a recording of the ding is available.
        /// </summary>
        public bool RecordingIsReady { get; set; }
        /// <summary>
        /// The Ring device that the ding originated from.
        /// </summary>
        public Device Device { get; set; }
        /// <summary>
        /// The type of ding that occurred.
        /// </summary>
        public DingType Type { get; set; }

        public bool Favorite { get; set; }
    }

    /// <summary>
    /// An enumeration representing types of dings.
    /// </summary>
    public enum DingType
    {
        /// <summary>
        /// Ring Doorbell was rung.
        /// </summary>
        Ring,
        /// <summary>
        /// Motion was detected by a Ring device.
        /// </summary>
        Motion,
        /// <summary>
        /// Unknown ding type.
        /// </summary>
        Unknown,
        OnDemand
    }
}