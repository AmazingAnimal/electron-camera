using System;
using System.IO.Ports;
using System.Linq;

namespace ModbusRtuDemo
{
    public sealed class ModbusRtuClient : IDisposable
    {
        private readonly SerialPort _serialPort;

        public ModbusRtuClient(string portName, int baudRate, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
            {
                ReadTimeout = 2000,
                WriteTimeout = 2000
            };
        }

        public bool IsOpen => _serialPort.IsOpen;

        public void Open()
        {
            if (!IsOpen)
            {
                _serialPort.Open();
            }
        }

        public void Close()
        {
            if (IsOpen)
            {
                _serialPort.Close();
            }
        }

        public bool[] ReadCoils(byte slaveId, ushort startAddress, ushort coilCount)
        {
            byte[] request = BuildReadCoilsRequest(slaveId, startAddress, coilCount);
            PrepareRequest();
            _serialPort.Write(request, 0, request.Length);

            int expectedLength = 5 + (coilCount + 7) / 8;
            byte[] response = ReadExact(expectedLength);

            ValidateCrc(response);
            ValidateResponseHeader(response, slaveId, 0x01);

            int byteCount = response[2];
            bool[] values = new bool[coilCount];
            for (int i = 0; i < coilCount; i++)
            {
                int byteIndex = 3 + i / 8;
                if (byteIndex >= 3 + byteCount)
                {
                    throw new Exception("线圈响应长度异常");
                }
                values[i] = ((response[byteIndex] >> (i % 8)) & 0x01) == 1;
            }

            return values;
        }

        public ushort[] ReadHoldingRegisters(byte slaveId, ushort startAddress, ushort registerCount)
        {
            byte[] request = BuildReadHoldingRegistersRequest(slaveId, startAddress, registerCount);
            PrepareRequest();
            _serialPort.Write(request, 0, request.Length);

            int expectedLength = 5 + registerCount * 2;
            byte[] response = ReadExact(expectedLength);

            ValidateCrc(response);
            ValidateResponseHeader(response, slaveId, 0x03);

            int byteCount = response[2];
            if (byteCount != registerCount * 2)
            {
                throw new Exception($"返回字节数异常: {byteCount}");
            }

            ushort[] values = new ushort[registerCount];
            for (int i = 0; i < registerCount; i++)
            {
                values[i] = (ushort)((response[3 + i * 2] << 8) | response[4 + i * 2]);
            }

            return values;
        }

        public void WriteSingleCoil(byte slaveId, ushort coilAddress, bool value)
        {
            byte[] request = BuildWriteSingleCoilRequest(slaveId, coilAddress, value);
            PrepareRequest();
            _serialPort.Write(request, 0, request.Length);

            byte[] response = ReadExact(8);
            ValidateCrc(response);
            ValidateResponseHeader(response, slaveId, 0x05);
        }

        public void WriteSingleRegister(byte slaveId, ushort registerAddress, ushort value)
        {
            byte[] request = BuildWriteSingleRegisterRequest(slaveId, registerAddress, value);
            PrepareRequest();
            _serialPort.Write(request, 0, request.Length);

            byte[] response = ReadExact(8);
            ValidateCrc(response);
            ValidateResponseHeader(response, slaveId, 0x06);
        }

        public byte[] BuildReadCoilsRequest(byte slaveId, ushort startAddress, ushort coilCount)
        {
            return BuildRequest(slaveId, 0x01, startAddress, coilCount);
        }

        public byte[] BuildReadHoldingRegistersRequest(byte slaveId, ushort startAddress, ushort registerCount)
        {
            return BuildRequest(slaveId, 0x03, startAddress, registerCount);
        }

        public byte[] BuildWriteSingleCoilRequest(byte slaveId, ushort coilAddress, bool value)
        {
            return BuildRequest(slaveId, 0x05, coilAddress, value ? (ushort)0xFF00 : (ushort)0x0000);
        }

        public byte[] BuildWriteSingleRegisterRequest(byte slaveId, ushort registerAddress, ushort value)
        {
            return BuildRequest(slaveId, 0x06, registerAddress, value);
        }

        private static byte[] BuildRequest(byte slaveId, byte functionCode, ushort address, ushort value)
        {
            byte[] frame = new byte[8];
            frame[0] = slaveId;
            frame[1] = functionCode;
            frame[2] = (byte)(address >> 8);
            frame[3] = (byte)(address & 0xFF);
            frame[4] = (byte)(value >> 8);
            frame[5] = (byte)(value & 0xFF);

            ushort crc = ComputeCrc(frame, 6);
            frame[6] = (byte)(crc & 0xFF);
            frame[7] = (byte)(crc >> 8);
            return frame;
        }

        public static ushort ComputeCrc(byte[] data, int length)
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

        public static string ToHex(byte[] data)
        {
            return string.Join(" ", data.Select(b => b.ToString("X2")));
        }

        private void PrepareRequest()
        {
            if (!IsOpen)
            {
                Open();
            }

            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
        }

        private byte[] ReadExact(int expectedLength)
        {
            byte[] buffer = new byte[expectedLength];
            int offset = 0;
            while (offset < expectedLength)
            {
                int read = _serialPort.Read(buffer, offset, expectedLength - offset);
                if (read <= 0)
                {
                    throw new TimeoutException("读取串口超时");
                }
                offset += read;
            }
            return buffer;
        }

        private static void ValidateCrc(byte[] response)
        {
            ushort expected = ComputeCrc(response, response.Length - 2);
            ushort actual = (ushort)(response[response.Length - 2] | (response[response.Length - 1] << 8));
            if (expected != actual)
            {
                throw new Exception($"CRC 校验失败: expected=0x{expected:X4}, actual=0x{actual:X4}");
            }
        }

        private static void ValidateResponseHeader(byte[] response, byte slaveId, byte functionCode)
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

        public void Dispose()
        {
            Close();
            _serialPort.Dispose();
        }
    }
}
