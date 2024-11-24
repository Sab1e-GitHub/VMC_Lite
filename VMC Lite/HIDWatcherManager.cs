using HidLibrary;
using System.Diagnostics;
using System.Management;
using System.Windows;

namespace VMC_Lite
{
    public class DeviceConnectionEventArgs : EventArgs
    {

        public bool IsConnected { get; }
        public UInt16 Vid { get; }
        public UInt16 Pid { get; }

        public DeviceConnectionEventArgs(bool isConnected, UInt16 vid, UInt16 pid)
        {
            IsConnected = isConnected;
            Vid = vid;
            Pid = pid;
        }
    }
    class HIDWatcherManager
    {
        private ManagementEventWatcher insertWatcher;
        private ManagementEventWatcher removeWatcher;
        private readonly List<(int vid, int pid)> _vmcDevices = new List<(int, int)>();
        private readonly List<(int vid, int pid, bool isConnected)> _vmcBleDevices = new List<(int, int, bool)>();
        public event EventHandler<DeviceConnectionEventArgs> DeviceConnectionChanged;
        private Thread _bleWatcherThread;
        private bool _bleWatcherRunning;
        public HIDWatcherManager()
        {
        }
        public void AddVMCDevice(int vid, int pid)
        {
            _vmcDevices.Add((vid, pid));
            Debug.WriteLine($"Added VMC device: VID={vid:X4}, PID={pid:X4}");
        }
        public void AddBleDevice(int vid, int pid)
        {
            _vmcBleDevices.Add((vid, pid, false));
            Debug.WriteLine($"Added VMC BLE device: VID={vid:X4}, PID={pid:X4}");
        }
        protected virtual void OnDeviceConnectionChanged(DeviceConnectionEventArgs e)
        {
            DeviceConnectionChanged?.Invoke(this, e);  // 触发事件并传递数据
        }
        // OnDeviceConnectionChanged(new DeviceConnectionEventArgs(true));
        /// <summary>
        /// 开始监听设备插入事件和设备拔出事件
        /// </summary>
        public void StartMonitoringDevice()
        {
            // 监听USB设备插入
            insertWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2"));
            insertWatcher.EventArrived += DeviceInserted;
            insertWatcher.Start();

            // 监听USB设备拔出
            removeWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3"));
            removeWatcher.EventArrived += DeviceRemoved;
            removeWatcher.Start();

            StartWatchBleDevice();
        }
        private void StartWatchBleDevice()
        {
            if (_bleWatcherRunning) { return; }
            // 创建一个新的线程来读取输入
            _bleWatcherThread = new Thread(BleWatcherThread)
            {
                IsBackground = true // 设置为后台线程，当主线程退出时它将自动退出
            };
            _bleWatcherRunning = true;
            _bleWatcherThread.Start();

        }
        private void BleWatcherThread()
        {
            while (_bleWatcherRunning)
            {
                try
                {
                    foreach (var device in _vmcBleDevices.ToList()) // 使用 ToList() 防止在遍历时修改列表
                    {
                        bool deviceFound = false;

                        // 使用 HidLibrary 来获取当前连接的 HID 设备列表
                        var hidDevices = HidDevices.Enumerate().ToList();

                        foreach (var hidDevice in hidDevices)
                        {
                            // 获取设备的 VID 和 PID
                            int vid = hidDevice.Attributes.VendorId;
                            int pid = hidDevice.Attributes.ProductId;

                            // 比对 VID 和 PID
                            if (vid == device.vid && pid == device.pid)
                            {
                                deviceFound = true;
                                break;
                            }
                        }

                        // 如果设备状态发生变化，更新状态并打印日志
                        if (deviceFound && !device.isConnected)
                        {
                            // 设备从未连接变为已连接
                            _vmcBleDevices[_vmcBleDevices.IndexOf(device)] = (device.vid, device.pid, true);
                            Debug.WriteLine($"Ble Device connected: VID={device.vid:X4}, PID={device.pid:X4}");
                            OnDeviceConnectionChanged(new DeviceConnectionEventArgs(true, (ushort)device.vid, (ushort)device.pid));
                        }
                        else if (!deviceFound && device.isConnected)
                        {
                            // 设备从已连接变为未连接
                            _vmcBleDevices[_vmcBleDevices.IndexOf(device)] = (device.vid, device.pid, false);
                            Debug.WriteLine($"Ble Device disconnected: VID={device.vid:X4}, PID={device.pid:X4}");
                            OnDeviceConnectionChanged(new DeviceConnectionEventArgs(false, (ushort)device.vid, (ushort)device.pid));
                        }
                    }
                    Thread.Sleep(1000);
                    //Debug.WriteLine("BLE Watcher运行中");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in BLE Watcher Thread: {ex.Message}");
                }
            }

        }

        public void StopWatcher()
        {
            insertWatcher.Stop();
            removeWatcher.Stop();
            _bleWatcherRunning = false;
            _bleWatcherThread?.Join();
        }
        /// <summary>
        /// 设备插入时触发的回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceInserted(object sender, EventArrivedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 使用 HidLibrary 获取当前连接的所有 HID 设备
                var hidDevices = HidDevices.Enumerate().ToList();

                foreach (var device in hidDevices)
                {
                    // 获取设备的 VID 和 PID
                    int vid = device.Attributes.VendorId;
                    int pid = device.Attributes.ProductId;

                    // 查找符合条件的 vid pid 设备
                    var targetDevice = _vmcDevices.FirstOrDefault(d => d.vid == vid && d.pid == pid);

                    if (targetDevice != default)
                    {
                        // 如果找到了匹配的设备
                        Debug.WriteLine($"Found target device: VID:{vid:X4} PID:{pid:X4}");
                        // 执行你需要的操作，比如设备已插入的处理
                        OnDeviceConnectionChanged(new DeviceConnectionEventArgs(true, (ushort)vid, (ushort)pid));
                    }
                }
            });
        }

        /// <summary>
        /// 当 HID 设备拔出时触发的回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceRemoved(object sender, EventArrivedEventArgs e)
        {
            // 创建一个列表来记录当前存在的设备
            var currentDevices = new List<(int vid, int pid)>();

            // 使用 HidLibrary 获取当前连接的所有 HID 设备
            var hidDevices = HidDevices.Enumerate().ToList();

            foreach (var device in hidDevices)
            {
                // 获取设备的 VID 和 PID
                int vid = device.Attributes.VendorId;
                int pid = device.Attributes.ProductId;

                // 将当前设备的 VID 和 PID 添加到列表中
                currentDevices.Add((vid, pid));
            }

            // 检查目标设备列表中是否有设备不存在于当前设备列表
            var removedDevices = _vmcDevices
                .Where(target => !currentDevices.Any(current =>
                    current.vid == target.vid &&
                    current.pid == target.pid))
                .ToList();

            // 打印所有拔出的设备
            foreach (var device in removedDevices)
            {
                OnDeviceConnectionChanged(new DeviceConnectionEventArgs(false, (ushort)device.vid, (ushort)device.pid));
                Debug.WriteLine($"Device removed: VID:{device.vid:X4} PID:{device.pid:X4}");
            }
        }

        ///// <summary>
        ///// 通过给出的设备ID解析出VID值
        ///// </summary>
        ///// <param name="deviceID">设备ID字符串</param>
        ///// <returns>返回解析后的字符串</returns>
        //private string GetVID(string deviceID)
        //{
        //    // 查找 VID 在 "VID_" 之后的索引
        //    int startIdx = deviceID.IndexOf("VID_") + 4; // 索引从 "VID_" 后面开始
        //    int endIdx = deviceID.IndexOf("&", startIdx);  // 找到 "&" 作为结束位置

        //    if (startIdx >= 4 && endIdx > startIdx)
        //    {
        //        return deviceID.Substring(startIdx, endIdx - startIdx);
        //    }
        //    return null;  // 返回 null 或空字符串，表示没有找到 VID
        //}
        ///// <summary>
        ///// 通过给出的设备ID解析出PID值
        ///// </summary>
        ///// <param name="deviceID">设备ID字符串</param>
        ///// <returns>返回解析后的字符串</returns>
        //private string GetPID(string deviceID)
        //{
        //    // 查找 PID 在 "PID_" 之后的索引
        //    int startIdx = deviceID.IndexOf("PID_") + 4;  // 索引从 "PID_" 后面开始
        //    int endIdx = deviceID.IndexOf("\\", startIdx); // 找到 "\\" 作为结束位置

        //    if (startIdx >= 4 && endIdx > startIdx)
        //    {
        //        return deviceID.Substring(startIdx, endIdx - startIdx);
        //    }
        //    return null;  // 返回 null 或空字符串，表示没有找到 PID
        //}
    }
}
