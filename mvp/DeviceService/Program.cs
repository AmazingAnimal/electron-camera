using System;
using System.Threading;
using DeviceService.Models;
using DeviceService.Services;
using ModbusRtuDemo;

namespace DeviceService
{
    class Program
    {
        private static volatile bool _running = true;
        private static volatile bool _manualTriggerRequested = false;
        private static volatile bool _forceScanSuccess = true;

        static int Main(string[] args)
        {
            int exitCode = 0;
            string portName = args.Length > 0 ? args[0] : "COM3";
            int baudRate = args.Length > 1 ? int.Parse(args[1]) : 9600;
            byte slaveId = args.Length > 2 ? byte.Parse(args[2]) : (byte)1;
            int pollIntervalMs = args.Length > 3 ? int.Parse(args[3]) : 500;

            var mockScanner = new MockScannerService();
            StartCommandLoop();

            try
            {
                using (var plcService = new PlcService(portName, baudRate, slaveId))
                {
                    Console.WriteLine($"[启动] port={portName}, baudRate={baudRate}, slaveId={slaveId}, pollIntervalMs={pollIntervalMs}");
                    plcService.Connect();
                    Console.WriteLine("{\"type\":\"service-status\",\"status\":\"started\"}");

                    bool lastTrigger = false;
                    int loopIndex = 0;
                    int consecutiveFailures = 0;
                    bool degradedReported = false;

                    while (_running)
                    {
                        loopIndex++;

                        try
                        {
                            Console.WriteLine($"[轮询 {loopIndex}] reading trigger...");
                            bool trigger = plcService.ReadScanTrigger(0);

                            Console.WriteLine($"[轮询 {loopIndex}] reading device status...");
                            ushort deviceStatus = plcService.ReadDeviceStatus(1);

                            Console.WriteLine($"[轮询 {loopIndex}] reading allow pass coil...");
                            bool allowPass = plcService.ReadAllowPass(0);

                            if (consecutiveFailures > 0)
                            {
                                Console.WriteLine($"[轮询 {loopIndex}] communication recovered after {consecutiveFailures} failures");
                            }

                            consecutiveFailures = 0;
                            if (degradedReported)
                            {
                                Console.WriteLine("{\"type\":\"service-status\",\"status\":\"connected\",\"message\":\"modbus recovered\"}");
                                degradedReported = false;
                            }

                            string plcStatusJson = $"{{\"type\":\"plc-status\",\"scanTrigger\":{trigger.ToString().ToLower()},\"deviceStatus\":{deviceStatus},\"allowPass\":{allowPass.ToString().ToLower()},\"time\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}";
                            Console.WriteLine(plcStatusJson);

                            bool shouldTrigger = (trigger && !lastTrigger) || _manualTriggerRequested;
                            if (shouldTrigger)
                            {
                                _manualTriggerRequested = false;
                                Console.WriteLine($"[轮询 {loopIndex}] trigger detected, creating mock scan event...");
                                ScannerEvent scanEvent = mockScanner.CreateMockScanEvent();
                                string scanJson = $"{{\"type\":\"scan\",\"deviceId\":\"{scanEvent.DeviceId}\",\"code\":\"{scanEvent.Code}\",\"symbology\":\"{scanEvent.Symbology}\",\"scanTime\":\"{scanEvent.ScanTime}\",\"source\":\"{scanEvent.Source}\"}}";
                                Console.WriteLine(scanJson);

                                Console.WriteLine($"[轮询 {loopIndex}] writing scan success coil...");
                                plcService.WriteScanSuccess(_forceScanSuccess, 0);

                                Console.WriteLine($"[轮询 {loopIndex}] writing scan status register...");
                                plcService.WriteScanStatusCode(_forceScanSuccess ? (ushort)1 : (ushort)2, 10);
                            }

                            lastTrigger = trigger;
                        }
                        catch (Exception ex)
                        {
                            consecutiveFailures++;
                            string safeMessage = ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"");
                            string errorJson = $"{{\"type\":\"error\",\"message\":\"{safeMessage}\",\"failureCount\":{consecutiveFailures},\"time\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}";
                            Console.WriteLine(errorJson);

                            if (!degradedReported)
                            {
                                Console.WriteLine("{\"type\":\"service-status\",\"status\":\"degraded\",\"message\":\"modbus communication failed\"}");
                                degradedReported = true;
                            }
                        }

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

            Console.WriteLine("{\"type\":\"service-status\",\"status\":\"stopped\"}");
            return exitCode;
        }

        private static void StartCommandLoop()
        {
            var commandThread = new Thread(() =>
            {
                while (_running)
                {
                    string line = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    if (line.Contains("mock-trigger"))
                    {
                        _manualTriggerRequested = true;
                        Console.WriteLine("{\"type\":\"service-log\",\"message\":\"mock-trigger received\"}");
                    }
                    else if (line.Contains("mock-success"))
                    {
                        _forceScanSuccess = true;
                        Console.WriteLine("{\"type\":\"service-log\",\"message\":\"mock success mode enabled\"}");
                    }
                    else if (line.Contains("mock-fail"))
                    {
                        _forceScanSuccess = false;
                        Console.WriteLine("{\"type\":\"service-log\",\"message\":\"mock fail mode enabled\"}");
                    }
                    else if (line.Contains("stop"))
                    {
                        _running = false;
                        Console.WriteLine("{\"type\":\"service-log\",\"message\":\"stop command received\"}");
                    }
                    else
                    {
                        Console.WriteLine("{\"type\":\"service-log\",\"message\":\"unknown command\"}");
                    }
                }
            });

            commandThread.IsBackground = true;
            commandThread.Start();
        }
    }
}