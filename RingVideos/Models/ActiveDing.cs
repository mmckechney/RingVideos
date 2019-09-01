namespace Ring.Models
{
    /// <summary>
    /// A model representing an active ding.
    /// </summary>
    public class ActiveDing
    {
        /// <summary>
        /// The ID of the ding.
        /// </summary>
        public ulong Id { get; set; }
        /// <summary>
        /// The Ring device that the ding originated from.
        /// </summary>
        public Device Device { get; set; }
        /// <summary>
        /// The type of ding that occurred.
        /// </summary>
        public DingType Type { get; set; }
    }
}