using System;

namespace ModbusRtuDemo
{
    public sealed class PlcService : IDisposable
    {
        private readonly ModbusRtuClient _client;
        private readonly byte _slaveId;

        public PlcService(string portName, int baudRate, byte slaveId)
        {
            _client = new ModbusRtuClient(portName, baudRate);
            _slaveId = slaveId;
        }

        public bool IsConnected => _client.IsOpen;

        public void Connect()
        {
            _client.Open();
        }

        public void Disconnect()
        {
            _client.Close();
        }

        public ushort[] ReadRegisters(ushort startAddress, ushort registerCount)
        {
            return _client.ReadHoldingRegisters(_slaveId, startAddress, registerCount);
        }

        public bool[] ReadCoils(ushort startAddress, ushort coilCount)
        {
            return _client.ReadCoils(_slaveId, startAddress, coilCount);
        }

        // 示例：读取“扫码触发”寄存器，约定值为 1 表示触发
        public bool ReadScanTrigger(ushort address = 0)
        {
            ushort[] values = ReadRegisters(address, 1);
            return values.Length > 0 && values[0] == 1;
        }

        // 示例：读取“设备状态”寄存器
        public ushort ReadDeviceStatus(ushort address = 1)
        {
            ushort[] values = ReadRegisters(address, 1);
            return values.Length > 0 ? values[0] : (ushort)0;
        }

        // 示例：读取“允许放行”线圈
        public bool ReadAllowPass(ushort address = 0)
        {
            bool[] values = ReadCoils(address, 1);
            return values.Length > 0 && values[0];
        }

        // 示例：写入“扫码成功”状态线圈
        public void WriteScanSuccess(bool success, ushort address = 0)
        {
            _client.WriteSingleCoil(_slaveId, address, success);
        }

        // 示例：写入“扫码结果状态码”寄存器
        public void WriteScanStatusCode(ushort statusCode, ushort address = 10)
        {
            _client.WriteSingleRegister(_slaveId, address, statusCode);
        }

        public byte[] BuildReadRegistersRequest(ushort startAddress, ushort registerCount)
        {
            return _client.BuildReadHoldingRegistersRequest(_slaveId, startAddress, registerCount);
        }

        public byte[] BuildReadCoilsRequest(ushort startAddress, ushort coilCount)
        {
            return _client.BuildReadCoilsRequest(_slaveId, startAddress, coilCount);
        }

        public byte[] BuildWriteSingleCoilRequest(ushort address, bool value)
        {
            return _client.BuildWriteSingleCoilRequest(_slaveId, address, value);
        }

        public byte[] BuildWriteSingleRegisterRequest(ushort address, ushort value)
        {
            return _client.BuildWriteSingleRegisterRequest(_slaveId, address, value);
        }

        public static string ToHex(byte[] data)
        {
            return ModbusRtuClient.ToHex(data);
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}