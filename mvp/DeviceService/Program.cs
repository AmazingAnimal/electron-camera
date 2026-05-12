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
            int exitCode = 0;
            string portName = args.Length > 0 ? args[0] : "COM3";
            int baudRate = args.Length > 1 ? int.Parse(args[1]) : 9600;
            byte slaveId = args.Length > 2 ? byte.Parse(args[2]) : (byte)1;
            int pollIntervalMs = args.Length > 3 ? int.Parse(args[3]) : 500;

            var mockScanner = new MockScannerService();

            try
            {
                using (var plcService = new PlcService(portName, baudRate, slaveId))
                {
                    Console.WriteLine($"[启动] port={portName}, baudRate={baudRate}, slaveId={slaveId}, pollIntervalMs={pollIntervalMs}");
                    plcService.Connect();
                    Console.WriteLine("{\"type\":\"service-status\",\"status\":\"started\"}");

                    bool lastTrigger = false;
                    int loopIndex = 0;

                    while (true)
                    {
                        loopIndex++;
                        Console.WriteLine($"[轮询 {loopIndex}] reading trigger...");
                        bool trigger = plcService.ReadScanTrigger(0);

                        Console.WriteLine($"[轮询 {loopIndex}] reading device status...");
                        ushort deviceStatus = plcService.ReadDeviceStatus(1);

                        Console.WriteLine($"[轮询 {loopIndex}] reading allow pass coil...");
                        bool allowPass = plcService.ReadAllowPass(0);

                        string plcStatusJson = $"{{\"type\":\"plc-status\",\"scanTrigger\":{trigger.ToString().ToLower()},\"deviceStatus\":{deviceStatus},\"allowPass\":{allowPass.ToString().ToLower()},\"time\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}";
                        Console.WriteLine(plcStatusJson);

                        if (trigger && !lastTrigger)
                        {
                            Console.WriteLine($"[轮询 {loopIndex}] trigger rising edge detected, creating mock scan event...");
                            ScannerEvent scanEvent = mockScanner.CreateMockScanEvent();
                            string scanJson = $"{{\"type\":\"scan\",\"deviceId\":\"{scanEvent.DeviceId}\",\"code\":\"{scanEvent.Code}\",\"symbology\":\"{scanEvent.Symbology}\",\"scanTime\":\"{scanEvent.ScanTime}\",\"source\":\"{scanEvent.Source}\"}}";
                            Console.WriteLine(scanJson);

                            Console.WriteLine($"[轮询 {loopIndex}] writing scan success coil...");
                            plcService.WriteScanSuccess(true, 0);

                            Console.WriteLine($"[轮询 {loopIndex}] writing scan status register...");
                            plcService.WriteScanStatusCode(1, 10);
                        }

                        lastTrigger = trigger;
                        Thread.Sleep(pollIntervalMs);
                    }
                }
            }
            catch (Exception ex)
            {
                string safeMessage = ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"");
                string errorJson = $"{{\"type\":\"error\",\"message\":\"{safeMessage}\",\"time\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}";
                Console.WriteLine(errorJson);
                exitCode = -1;
            }

            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
            return exitCode;
        }
    }
}