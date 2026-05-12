namespace DeviceService.Models
{
    public sealed class ScannerEvent
    {
        public string Type { get; set; }
        public string DeviceId { get; set; }
        public string Code { get; set; }
        public string Symbology { get; set; }
        public string ScanTime { get; set; }
        public string Source { get; set; }
    }
}