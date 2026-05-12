namespace DeviceService.Models
{
    public sealed class PlcStatus
    {
        public string Type { get; set; }
        public bool ScanTrigger { get; set; }
        public ushort DeviceStatus { get; set; }
        public bool AllowPass { get; set; }
        public string Time { get; set; }
    }
}