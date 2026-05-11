using MvCameraControl;
using System;
using System.Collections.Generic;

namespace HikDeviceSmokeTest
{
    class Program
    {
        static readonly DeviceTLayerType EnumTLayerType =
            DeviceTLayerType.MvGigEDevice |
            DeviceTLayerType.MvUsbDevice |
            DeviceTLayerType.MvGenTLGigEDevice |
            DeviceTLayerType.MvGenTLCXPDevice |
            DeviceTLayerType.MvGenTLCameraLinkDevice |
            DeviceTLayerType.MvGenTLXoFDevice;

        static int Main(string[] args)
        {
            IDevice device = null;

            try
            {
                SDKSystem.Initialize();
                Console.WriteLine("[1/6] SDK init ok");

                List<IDeviceInfo> deviceInfos;
                int result = DeviceEnumerator.EnumDevices(EnumTLayerType, out deviceInfos);
                if (result != MvError.MV_OK)
                {
                    Console.WriteLine($"EnumDevices failed: 0x{result:X8}");
                    return -1;
                }

                Console.WriteLine($"[2/6] device count: {deviceInfos.Count}");
                if (deviceInfos.Count == 0)
                {
                    Console.WriteLine("No device found");
                    return -2;
                }

                for (int i = 0; i < deviceInfos.Count; i++)
                {
                    var info = deviceInfos[i];
                    Console.WriteLine($"  [{i}] {info.TLayerType} | {info.ManufacturerName} | {info.ModelName} | SN={info.SerialNumber}");
                }

                var deviceInfo = deviceInfos[0];
                Console.WriteLine($"[3/6] use device: {deviceInfo.ModelName} / {deviceInfo.SerialNumber}");

                device = DeviceFactory.CreateDevice(deviceInfo);

                result = device.Open();
                if (result != MvError.MV_OK)
                {
                    Console.WriteLine($"Open failed: 0x{result:X8}");
                    return -3;
                }
                Console.WriteLine("[4/6] open ok");

                if (device is IGigEDevice gigEDevice)
                {
                    int packetSize;
                    result = gigEDevice.GetOptimalPacketSize(out packetSize);
                    if (result == MvError.MV_OK)
                    {
                        device.Parameters.SetIntValue("GevSCPSPacketSize", packetSize);
                        Console.WriteLine($"GigE packet size set: {packetSize}");
                    }
                    else
                    {
                        Console.WriteLine($"GetOptimalPacketSize failed: 0x{result:X8}");
                    }
                }

                device.Parameters.SetEnumValueByString("AcquisitionMode", "Continuous");
                device.Parameters.SetEnumValueByString("TriggerMode", "Off");

                result = device.StreamGrabber.StartGrabbing();
                if (result != MvError.MV_OK)
                {
                    Console.WriteLine($"StartGrabbing failed: 0x{result:X8}");
                    return -4;
                }
                Console.WriteLine("[5/6] start grabbing ok");

                IFrameOut frameOut;
                result = device.StreamGrabber.GetImageBuffer(3000, out frameOut);
                if (result == MvError.MV_OK)
                {
                    Console.WriteLine($"[6/6] get one frame ok: {frameOut.Image.Width}x{frameOut.Image.Height}, frameNum={frameOut.FrameNum}, pixelType={frameOut.Image.PixelType}");
                    frameOut.Dispose();
                }
                else
                {
                    Console.WriteLine($"GetImageBuffer failed: 0x{result:X8}");
                }

                result = device.StreamGrabber.StopGrabbing();
                Console.WriteLine(result == MvError.MV_OK ? "stop grabbing ok" : $"StopGrabbing failed: 0x{result:X8}");

                result = device.Close();
                Console.WriteLine(result == MvError.MV_OK ? "close ok" : $"Close failed: 0x{result:X8}");

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return -99;
            }
            finally
            {
                if (device != null)
                {
                    device.Dispose();
                }

                SDKSystem.Finalize();
            }
        }
    }
}