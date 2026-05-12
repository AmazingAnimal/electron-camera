using System;
using DeviceService.Models;

namespace DeviceService.Services
{
    public sealed class MockScannerService
    {
        private int _counter = 1;

        public ScannerEvent CreateMockScanEvent()
        {
            string code = $"MOCK-{DateTime.Now:yyyyMMdd}-{_counter:D4}";
            _counter++;

            return new ScannerEvent
            {
                Type = "scan",
                DeviceId = "mock-camera-01",
                Code = code,
                Symbology = "QR",
                ScanTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Source = "mock"
            };
        }
    }
}