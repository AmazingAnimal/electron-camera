using System;
using System.Globalization;
using System.IO.Ports;
using System.Linq;

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

            using (var serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One))
            {
                serialPort.ReadTimeout = 2000;
                serialPort.WriteTimeout = 2000;

                try
                {
                    serialPort.Open();
                    Console.WriteLine($"串口已打开: {portName}, 波特率: {baudRate}");

                    byte[] request = BuildReadHoldingRegistersRequest(slaveId, startAddress, registerCount);
                    Console.WriteLine("发送报文: " + ToHex(request));
                    serialPort.Write(request, 0, request.Length);

                    // 读取响应: 从站地址(1) + 功能码(1) + 字节数(1) + 数据(N*2) + CRC(2)
                    int expectedLength = 5 + registerCount * 2;
                    byte[] response = ReadExact(serialPort, expectedLength);
                    Console.WriteLine("接收报文: " + ToHex(response));

                    ValidateCrc(response);
                    ValidateResponseHeader(response, slaveId, 0x03);

                    int byteCount = response[2];
                    if (byteCount != registerCount * 2)
                    {
                        throw new Exception($"返回字节数异常: {byteCount}");
                    }

                    for (int i = 0; i < registerCount; i++)
                    {
                        ushort value = (ushort)((response[3 + i * 2] << 8) | response[4 + i * 2]);
                        Console.WriteLine($"寄存器[{startAddress + i}] = {value}");
                    }

                    return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Modbus RTU 通信失败:");
                    Console.WriteLine(ex.Message);
                    return -1;
                }
                finally
                {
                    if (serialPort.IsOpen)
                    {
                        serialPort.Close();
                        Console.WriteLine("串口已关闭");
                    }
                }
            }
        }

        static byte[] BuildReadHoldingRegistersRequest(byte slaveId, ushort startAddress, ushort registerCount)
        {
            byte[] frame = new byte[8];
            frame[0] = slaveId;
            frame[1] = 0x03;
            frame[2] = (byte)(startAddress >> 8);
            frame[3] = (byte)(startAddress & 0xFF);
            frame[4] = (byte)(registerCount >> 8);
            frame[5] = (byte)(registerCount & 0xFF);

            ushort crc = ComputeCrc(frame, 6);
            frame[6] = (byte)(crc & 0xFF);
            frame[7] = (byte)(crc >> 8);
            return frame;
        }

        static ushort ComputeCrc(byte[] data, int length)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    bool lsb = (crc & 0x0001) != 0;
                    crc >>= 1;
                    if (lsb)
                    {
                        crc ^= 0xA001;
                    }
                }
            }
            return crc;
        }

        static void ValidateCrc(byte[] response)
        {
            ushort expected = ComputeCrc(response, response.Length - 2);
            ushort actual = (ushort)(response[response.Length - 2] | (response[response.Length - 1] << 8));
            if (expected != actual)
            {
                throw new Exception($"CRC 校验失败: expected=0x{expected:X4}, actual=0x{actual:X4}");
            }
        }

        static void ValidateResponseHeader(byte[] response, byte slaveId, byte functionCode)
        {
            if (response[0] != slaveId)
            {
                throw new Exception($"从站地址不匹配: {response[0]}");
            }

            if (response[1] == (functionCode | 0x80))
            {
                throw new Exception($"从站返回异常码: 0x{response[2]:X2}");
            }

            if (response[1] != functionCode)
            {
                throw new Exception($"功能码不匹配: 0x{response[1]:X2}");
            }
        }

        static byte[] ReadExact(SerialPort port, int expectedLength)
        {
            byte[] buffer = new byte[expectedLength];
            int offset = 0;
            while (offset < expectedLength)
            {
                int read = port.Read(buffer, offset, expectedLength - offset);
                if (read <= 0)
                {
                    throw new TimeoutException("读取串口超时");
                }
                offset += read;
            }
            return buffer;
        }

        static string ToHex(byte[] data)
        {
            return string.Join(" ", data.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
        }
    }
}