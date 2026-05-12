using System;
using System.Threading;

namespace ModbusRtuDemo
{
    class Program
    {
        static int Main(string[] args)
        {
            string portName = args.Length > 0 ? args[0] : "COM3";
            int baudRate = args.Length > 1 ? int.Parse(args[1]) : 9600;
            byte slaveId = args.Length > 2 ? byte.Parse(args[2]) : (byte)1;
            ushort startAddress = args.Length > 3 ? ushort.Parse(args[3]) : (ushort)0;
            ushort registerCount = args.Length > 4 ? ushort.Parse(args[4]) : (ushort)2;

            try
            {
                using (var plcService = new PlcService(portName, baudRate, slaveId))
                {
                    plcService.Connect();
                    Console.WriteLine($"串口已打开: {portName}, 波特率: {baudRate}, 从站: {slaveId}");

                    byte[] registerRequest = plcService.BuildReadRegistersRequest(startAddress, registerCount);
                    Console.WriteLine("发送保持寄存器请求: " + PlcService.ToHex(registerRequest));

                    ushort[] values = plcService.ReadRegisters(startAddress, registerCount);
                    for (int i = 0; i < values.Length; i++)
                    {
                        Console.WriteLine($"寄存器[{startAddress + i}] = {values[i]}");
                    }

                    bool trigger = plcService.ReadScanTrigger(0);
                    Console.WriteLine($"扫码触发位(寄存器0) = {(trigger ? "触发" : "未触发")}");

                    ushort deviceStatus = plcService.ReadDeviceStatus(1);
                    Console.WriteLine($"设备状态(寄存器1) = {deviceStatus}");

                    byte[] coilRequest = plcService.BuildReadCoilsRequest(0, 1);
                    Console.WriteLine("发送线圈请求: " + PlcService.ToHex(coilRequest));
                    bool allowPass = plcService.ReadAllowPass(0);
                    Console.WriteLine($"允许放行线圈(地址0) = {(allowPass ? "ON" : "OFF")}");

                    Console.WriteLine();
                    Console.WriteLine("---- 写单线圈示例 ----");
                    byte[] writeCoilRequest = plcService.BuildWriteSingleCoilRequest(0, true);
                    Console.WriteLine("发送写单线圈请求: " + PlcService.ToHex(writeCoilRequest));
                    plcService.WriteScanSuccess(true, 0);
                    Console.WriteLine("写单线圈完成: 地址0 = ON");

                    Console.WriteLine();
                    Console.WriteLine("---- 写单寄存器示例 ----");
                    byte[] writeRegisterRequest = plcService.BuildWriteSingleRegisterRequest(10, 1);
                    Console.WriteLine("发送写单寄存器请求: " + PlcService.ToHex(writeRegisterRequest));
                    plcService.WriteScanStatusCode(1, 10);
                    Console.WriteLine("写单寄存器完成: 地址10 = 1");

                    Console.WriteLine();
                    Console.WriteLine("---- 轮询示例：持续读取扫码触发位 ----");
                    Console.WriteLine("默认轮询 10 次，每次间隔 1000ms");
                    for (int i = 0; i < 10; i++)
                    {
                        bool pollingTrigger = plcService.ReadScanTrigger(0);
                        ushort pollingDeviceStatus = plcService.ReadDeviceStatus(1);
                        Console.WriteLine($"[轮询 {i + 1}/10] 扫码触发位 = {(pollingTrigger ? "触发" : "未触发")}, 设备状态 = {pollingDeviceStatus}");
                        Thread.Sleep(1000);
                    }

                    Console.WriteLine();
                    Console.WriteLine("示例结束，串口即将关闭");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Modbus RTU 通信失败:");
                Console.WriteLine(ex.Message);
                return -1;
            }
        }
    }
}