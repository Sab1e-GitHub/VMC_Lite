using HidLibrary;
using System.Diagnostics;

namespace VMC_Lite
{

    /// <summary>
    /// 管理HID设备的连接、插入、拔出
    /// </summary>
    public class HIDManager
    {
        private HidDevice _hidDevice;
        private UInt16 _deviceVID;
        private UInt16 _devicePID;


        public HIDManager(UInt16 VID, UInt16 PID)
        {
            _deviceVID = VID;
            _devicePID = PID;
        }
        /// <summary>
        /// 打开设备
        /// </summary>
        /// <returns>返回是否打开成功</returns>
        public bool OpenDevice()
        {
            var devices = HidDevices.Enumerate();
            //Debug.WriteLine(devices);
            // 过滤符合条件的设备
            foreach (var device in devices)
            {
                // 获取设备的VID和PID
                int deviceVid = device.Attributes.VendorId;
                int devicePid = device.Attributes.ProductId;

                // 比较设备的VID和PID是否与目标匹配
                if (deviceVid == _deviceVID && devicePid == _devicePID)
                {
                    Debug.WriteLine($"Found device: VID = {deviceVid}, PID = {devicePid}");

                    _hidDevice = device;
                    // 打开设备
                    _hidDevice.OpenDevice();

                    Debug.WriteLine($"Device Connected!");
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 发送Output报告
        /// </summary>
        /// <param name="reportId">报告ID</param>
        /// <param name="data">要发送的数据</param>
        /// <returns>返回是否写入成功</returns>
        /// <exception cref="InvalidOperationException">设备未连接时抛出异常</exception>
        public bool SendOutputReport(byte reportId, byte[] data)
        {
            if (_hidDevice == null)
            {
                throw new InvalidOperationException("HID 设备未连接");
            }

            // HID 报告的长度（包括报告 ID），通常由设备定义
            int reportLength = _hidDevice.Capabilities.OutputReportByteLength;
            if (reportLength > 0)
            {
                // 创建要发送的报告，包含报告 ID
                byte[] outputReport = new byte[reportLength];
                outputReport[0] = reportId; // 设置报告 ID

                // 将数据填充到报告中，数据从第二个字节开始（第一个字节是报告 ID）
                for (int i = 0; i < data.Length && i < reportLength - 1; i++)
                {
                    outputReport[i + 1] = data[i];
                }

                // 使用 HID 库的 Write 方法发送报告
                bool result = _hidDevice.Write(outputReport);

                // 检查写入是否成功
                return result;
            }
            return false;
        }
        /// <summary>
        /// 发送一次Feature报告
        /// </summary>
        /// <param name="reportId">报告ID</param>
        /// <param name="data">要发送的数据</param>
        public void SendFeatureReport(byte reportId, byte[] data)
        {
            try
            {
                // 确保设备已连接
                if (IsDeviceOpened())
                {
                    // 将报告 ID 和数据组合成一个完整的 Feature 报告
                    byte[] featureReport = new byte[data.Length + 1];
                    featureReport[0] = reportId;
                    Array.Copy(data, 0, featureReport, 1, data.Length);

                    // 使用 Write 方法发送 Feature 报告
                    bool success = _hidDevice.WriteFeatureData(featureReport);
                    if (success)
                    {
                        Debug.WriteLine("Feature report sent successfully.");
                    }
                    else
                    {
                        Debug.WriteLine("Failed to send Feature report.");
                    }
                }
                else
                {
                    Debug.WriteLine("Device is not connected.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending Feature report: {ex.Message}");
            }
        }
        /// <summary>
        /// 仅读取一次Feature报告
        /// </summary>
        /// <param name="reportLength">读取的长度</param>
        /// <param name="reportId">报告ID</param>
        /// <returns>返回报告内容</returns>
        public byte[] ReadFeatureReport(int reportLength, byte reportId)
        {
            try
            {
                // 确保设备已连接
                if (IsDeviceOpened())
                {
                    // 创建一个空的 byte 数组来存储读取的数据
                    byte[] reportData = new byte[reportLength];

                    // 读取 Feature 数据
                    bool success = _hidDevice.ReadFeatureData(out reportData, reportId);
                    if (success)
                    {
                        Debug.WriteLine("Feature data received successfully.");
                        return reportData;
                    }
                    else
                    {
                        Debug.WriteLine("Failed to receive Feature data.");
                        return null;
                    }
                }
                else
                {
                    Debug.WriteLine("Device is not connected.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading Feature data: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// 发送一个Feature报告然后接收一次设备返回的Feature报告
        /// </summary>
        /// <param name="reportId">报告ID</param>
        /// <param name="sendData">要发送的数据</param>
        /// <returns>返回设备发来的Feature报告内容</returns>
        public byte[] SendAndReceiveFeatureReport(byte reportId, byte[] sendData)
        {
            // 发送 Feature 报告
            SendFeatureReport(reportId, sendData);

            // 读取设备的响应 Feature 报告
            int reportLength = 64;

            return ReadFeatureReport(reportLength, reportId);
            //if (receivedData != null)
            //{
            //    // 处理接收到的数据
            //    Debug.WriteLine("Received Data: " + BitConverter.ToString(receivedData));
            //}
        }
        public bool IsDeviceOpened()
        {
            if (_hidDevice != null && _hidDevice.IsConnected && _hidDevice.IsOpen)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public HidDeviceData? Read()
        {
            if (IsDeviceOpened())
            {
                return _hidDevice.Read();
            }
            return null;
        }
    }
}
