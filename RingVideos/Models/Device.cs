namespace Ring.Models
{
    /// <summary>
    /// A model representing a Ring device.
    /// </summary>
    public class Device
    {
        /// <summary>
        /// The ID of the device.
        /// </summary>
        public ulong Id { get; set; }
        /// <summary>
        /// The description of the device.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The firmware version installed on the device.
        /// </summary>
        public string FirmwareVersion { get; set; }
        /// <summary>
        /// The address of the device.
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// The latitude of the device.
        /// </summary>
        public double Latitude { get; set; }
        /// <summary>
        /// The longitude of the device.
        /// </summary>
        public double Longitude { get; set; }
        /// <summary>
        /// The time zone used by the device.
        /// </summary>
        public string TimeZone { get; set; }
        /// <summary>
        /// The battery life of the device. If the value is greater than 100, the device is getting A/C power and this represents the mV input. If the value is -1, the device does not save battery life information.
        /// </summary>
        public int BatteryLife { get; set; }
        /// <summary>
        /// The type of the device.
        /// </summary>
        public DeviceType Type { get; set; }
    }

    /// <summary>
    /// An enumeration representing types of Ring devices.
    /// </summary>
    public enum DeviceType
    {
        /// <summary>
        /// Ring Doorbell
        /// </summary>
        Doorbell,
        /// <summary>
        /// Ring Doorbell (Not Owned)
        /// </summary>
        AuthorizedDoorbell,
        /// <summary>
        /// Ring Chime
        /// </summary>
        Chime,
        /// <summary>
        /// Ring Cam
        /// </summary>
        Cam,
        /// <summary>
        /// Unknown Ring Device
        /// </summary>
        Unknown
    }
}