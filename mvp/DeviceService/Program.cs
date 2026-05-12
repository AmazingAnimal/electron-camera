using System;
using System.Threading;
using DeviceService.Models;
using DeviceService.Services;
using ModbusRtuDemo;

namespace DeviceService
{
    class Program
    {
        static int Main(string[] args)
        {
            string portName = args.Length > 0 ? args[0] : "COM3";
            int baudRate = args.Length > 1 ? int.Parse(args[1]) : 9600;
            byte slaveId = args.Length > 2 ? byte.Parse(args[2]) : (byte)1;
            int pollIntervalMs = args.Length > 3 ? int.Parse(args[3]) : 500;

            var mockScanner = new MockScannerService();

            try
            {
                using (var plcService = new PlcService(portName, baudRate, slaveId))
                {
                    plcService.Connect();
                    Console.WriteLine("{\"type\":\"service-status\",\"status\":\"started\"}");

                    bool lastTrigger = false;

                    while (true)
                    {
                        bool trigger = plcService.ReadScanTrigger(0);
                        ushort deviceStatus = plcService.ReadDeviceStatus(1);
                        bool allowPass = plcService.ReadAllowPass(0);

                        string plcStatusJson = $"{{\"type\":\"plc-status\",\"scanTrigger\":{trigger.ToString().ToLower()},\"deviceStatus\":{deviceStatus},\"allowPass\":{allowPass.ToString().ToLower()},\"time\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}";
                        Console.WriteLine(plcStatusJson);

                        if (trigger && !lastTrigger)
                        {
                            ScannerEvent scanEvent = mockScanner.CreateMockScanEvent();
                            string scanJson = $"{{\"type\":\"scan\",\"deviceId\":\"{scanEvent.DeviceId}\",\"code\":\"{scanEvent.Code}\",\"symbology\":\"{scanEvent.Symbology}\",\"scanTime\":\"{scanEvent.ScanTime}\",\"source\":\"{scanEvent.Source}\"}}";
                            Console.WriteLine(scanJson);

                            plcService.WriteScanSuccess(true, 0);
                            plcService.WriteScanStatusCode(1, 10);
                        }

                        lastTrigger = trigger;
                        Thread.Sleep(pollIntervalMs);
                    }
                }
            }
            catch (Exception ex)
            {
                string errorJson = $"{{\"type\":\"error\",\"message\":\"{ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",\"time\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}";
                Console.WriteLine(errorJson);
                return -1;
            }
        }
    }
}