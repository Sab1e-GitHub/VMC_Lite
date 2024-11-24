using HidLibrary;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static VMC_Lite.VMCDevice;
namespace VMC_Lite
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string SoftwareVersion = "1.2";
        private int _currentAngle = 0;
        private VMCBLEGamepadDevice _vmcBleGamepadDevice = VMCBLEGamepadDevice.Instance;
        private VMCMainDevice _vmcMainDevice = VMCMainDevice.Instance;
        private bool _continueReading = false;
        private Thread _inputThread;
        private HIDWatcherManager _hidWatcherManager;
        public MainViewModel ViewModel { get; private set; }
        public MainWindow()
        {

            InitializeComponent();
            // 数据绑定
            ViewModel = new MainViewModel();
            this.DataContext = ViewModel;

            this.Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // 在初始化时 试图打开一次设备
            if (_vmcBleGamepadDevice.hidMangager.OpenDevice())
            {
                // 如果打开了设备
                // 更新UI
                OnVMCBleDeviceStateChanged(true);
            }

            if (_vmcMainDevice.hidMangager.OpenDevice())
            {
                // 如果打开了设备
                // 初始化用户配置的UI，读取用户配置的内容
                // 窗口打开时，检测一次是否有VMC设备
                OnVMCMainDeviceStateChanged(true);
            }
            InitPropertiesSettingsUI();
            // 开始监控HID设备事件
            _hidWatcherManager = new HIDWatcherManager();
            _hidWatcherManager.AddBleDevice(VMCBLEGamepadDevice.Device.Vid, VMCBLEGamepadDevice.Device.Pid);
            _hidWatcherManager.AddVMCDevice(VMCMainDevice.Device.Vid, VMCMainDevice.Device.Pid);
            _hidWatcherManager.StartMonitoringDevice();
            _hidWatcherManager.DeviceConnectionChanged += OnDeviceConnectionChanged;


            // 订阅窗口的 Deactivated 和 Activated 事件
            this.Deactivated += MainWindow_Deactivated;
            this.Activated += MainWindow_Activated;

            OperationStatus.Text = "VMC Lite Ver:" + SoftwareVersion;
        }
        /// <summary>
        /// 当设备状态发生变化时进行处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDeviceConnectionChanged(object sender, DeviceConnectionEventArgs e)
        {
            Debug.WriteLine("设备状态发生变化。");
            if (e.IsConnected)
            {
                if (e.Vid == VMCMainDevice.Device.Vid && e.Pid == VMCMainDevice.Device.Pid)
                {
                    Debug.WriteLine("VID:" + e.Vid + "PID:" + e.Pid + "设备已连接!");
                    _vmcMainDevice.hidMangager.OpenDevice();
                    OnVMCMainDeviceStateChanged(true);
                }
                else if (e.Vid == VMCBLEGamepadDevice.Device.Vid && e.Pid == VMCBLEGamepadDevice.Device.Pid)
                {
                    Debug.WriteLine("VID:" + e.Vid + "PID:" + e.Pid + "设备已连接!");
                    _vmcBleGamepadDevice.hidMangager.OpenDevice();
                    OnVMCBleDeviceStateChanged(true);
                }
            }
            else
            {
                if (e.Vid == VMCMainDevice.Device.Vid && e.Pid == VMCMainDevice.Device.Pid)
                {
                    Debug.WriteLine("VID:" + e.Vid + "PID:" + e.Pid + "设备已断开连接!");
                    OnVMCMainDeviceStateChanged(false);
                }
                else if (e.Vid == VMCBLEGamepadDevice.Device.Vid && e.Pid == VMCBLEGamepadDevice.Device.Pid)
                {
                    Debug.WriteLine("VID:" + e.Vid + "PID:" + e.Pid + "设备已断开连接!");
                    OnVMCBleDeviceStateChanged(false);
                }
            }
        }

        /// <summary>
        /// 当窗口失去焦点时触发回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            Debug.WriteLine("窗口失去焦点");
            Overlay.Visibility = Visibility.Visible;
            StopReadingInput();
        }

        /// <summary>
        /// 当窗口获得焦点时触发回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Activated(object sender, EventArgs e)
        {
            Debug.WriteLine("窗口获得焦点");
            Overlay.Visibility = Visibility.Collapsed;
            if (_vmcMainDevice.hidMangager.IsDeviceOpened() && IsLoaded)
            {
                StartReadingInput();
            }
        }
        /// <summary>
        /// 窗口关闭时停止释放资源
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {

            base.OnClosed(e);
            _hidWatcherManager.StopWatcher();
            _vmcBleGamepadDevice.SetLedClear();
        }




        /// <summary>
        /// 开始实时读取 HID 设备的输入数据
        /// </summary>
        public void StartReadingInput()
        {
            if (_continueReading)
            {
                Debug.WriteLine("ReadingInput任务已经创建，不能重复创建");
                return;
            }

            // 创建一个新的线程来读取输入
            _inputThread = new Thread(ReadInputData)
            {
                IsBackground = true // 设置为后台线程，当主线程退出时它将自动退出
            };
            _continueReading = true;
            _inputThread.Start();
            Debug.WriteLine("ReadingInput任务创建成功");
        }

        /// <summary>
        /// 停止读取输入数据
        /// </summary>
        public void StopReadingInput()
        {
            _continueReading = false;
            _inputThread?.Join(); // 等待线程结束
        }

        /// <summary>
        /// 实时读取 HID 设备的输入数据
        /// </summary>
        private void ReadInputData()
        {
            while (_continueReading)
            {
                if (_vmcMainDevice.hidMangager.IsDeviceOpened())
                {
                    // 使用 Read 方法读取输入报告
                    HidDeviceData data = _vmcMainDevice.hidMangager.Read();

                    if (data != null)
                    {
                        if (data.Status == HidDeviceData.ReadStatus.Success)
                        {

                            // 处理读取到的输入数据
                            ProcessInputData(data.Data);
                        }
                        else
                        {
                            // 处理读取失败或设备断开连接
                            Debug.WriteLine("读取输入数据失败或设备断开连接");
                        }
                    }
                    else
                    {
                        // 处理读取失败或设备断开连接
                        Debug.WriteLine("读取输入数据失败或设备断开连接");
                    }
                }
                else
                {
                    Debug.WriteLine("设备未连接");
                }

                // 添加适当的延迟，避免高频率读取占用 CPU
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 处理读取到的输入数据
        /// </summary>
        /// <param name="inputData">输入报告中的数据</param>
        private void ProcessInputData(byte[] inputData)
        {

            switch (inputData[0])
            {
                case VMCMainDevice.ReportId.INPUT_REPORT_ID:
                    _currentAngle = (Int16)(inputData[1] | (inputData[2] << 8)) * Properties.Settings.Default.Steering_Wheel_Rotation_Range / 65534;
                    //Debug.WriteLine("角度 " + _currentAngle.ToString());
                    ViewModel.CurrentAngle = _currentAngle.ToString() + "°";
                    if (_currentAngle >= 0)
                    {
                        ViewModel.LeftAngle = 0;
                        ViewModel.RightAngle = _currentAngle;
                    }
                    else
                    {
                        ViewModel.RightAngle = 0;
                        ViewModel.LeftAngle = -_currentAngle;
                    }
                    ViewModel.AccelerateProgressValue = (UInt16)(inputData[3] | (inputData[4] << 8));
                    ViewModel.BrakeProgressValue = (UInt16)(inputData[5] | (inputData[6] << 8));
                    ViewModel.ClutchProgressValue = (UInt16)(inputData[7] | (inputData[8] << 8));
                    break;

            }


        }
        /// <summary>
        /// 当设备状态发生变化时会调用此方法用于初始化设备或进行其他操作
        /// </summary>
        /// <param name="isConnected">是否已经连接设备</param>
        private void OnVMCMainDeviceStateChanged(bool isConnected)
        {

            if (isConnected)
            {
                if (!_vmcMainDevice.Initialized)    //如果设备没有初始化，就初始化
                {
                    _vmcMainDevice.hidMangager.SendOutputReport(0x0B, [0x08]);
                    _vmcMainDevice.hidMangager.SendOutputReport(0x0B, [0x04]);
                    _vmcMainDevice.hidMangager.SendOutputReport(0x0B, [0x01]);
                    //TODO 如果游戏打开，可能会导致发送的报告冲突从而导致单片机卡死
                    if (IsWindowActive())  //只有当窗口有焦点时才开始读取Input
                    {
                        Debug.WriteLine("窗口有焦点");
                        StartReadingInput();
                    }
                    else
                    {
                        Debug.WriteLine("窗口没有焦点");
                    }
                    _vmcMainDevice.SendInitCommand();
                    _vmcMainDevice.Initialized = true;
                }
            }
            else
            {

                StopReadingInput();
                _vmcMainDevice.Initialized = false; // 设备断开连接，等待初始化
            }
            UpdateDeviceStateUI(isConnected);
        }
        private void OnVMCBleDeviceStateChanged(bool isConnected)
        {

            if (isConnected)
            {
                if (!_vmcBleGamepadDevice.Initialized)    //如果设备没有初始化，就初始化
                {

                    // 初始化设备
                    _vmcBleGamepadDevice.SendInitCommand();
                    _vmcBleGamepadDevice.StartLedTask();
                    _vmcBleGamepadDevice.Initialized = true;
                }
            }
            else
            {

                _vmcBleGamepadDevice.StopLedTask();
                _vmcBleGamepadDevice.Initialized = false; // 设备断开连接，等待初始化

            }
            UpdateBleDeviceStateUI(isConnected);
        }
        private bool IsWindowActive()
        {
            // 使用 Func 来获取并返回值
            return (bool)this.Dispatcher.Invoke(() =>
            {
                return this.IsActive;
            });
        }

        /// <summary>
        /// 更新设备连接状态的UI
        /// </summary>
        /// <param name="isConnected"></param>
        private void UpdateDeviceStateUI(bool isConnected)
        {
            if (isConnected)
            {
                this.Dispatcher.Invoke(() =>
                {
                    DeviceStateTextBlock.Text = "核心设备已连接";
                    DeviceStateEllipse.Fill = new SolidColorBrush(Colors.Green);
                });
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    DeviceStateTextBlock.Text = "核心设备已断开连接";
                    DeviceStateEllipse.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0xA4, 0x00, 0x00));
                });
            }
        }
        private void UpdateBleDeviceStateUI(bool isConnected)
        {
            if (isConnected)
            {
                this.Dispatcher.Invoke(() =>
                {
                    BleDeviceStateTextBlock.Text = "蓝牙设备已连接";
                    BleDeviceStateEllipse.Fill = new SolidColorBrush(Colors.Blue);
                });
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    BleDeviceStateTextBlock.Text = "蓝牙设备已断开连接";
                    BleDeviceStateEllipse.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0xA4, 0x00, 0x00));
                });
            }
        }
        /// <summary>
        /// 更新当前角度UI
        /// </summary>
        private void UpdateCurrentAngleUI()
        {

            if (_currentAngle >= 0)
            {
                this.Dispatcher.Invoke(() =>
                {
                    RightAngleBar.Value = _currentAngle;
                    LeftAngleBar.Value = 0;
                });
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    LeftAngleBar.Value = -_currentAngle;
                    RightAngleBar.Value = 0;
                });
            }

        }

        /// <summary>
        /// 显示操作是否成功
        /// </summary>
        /// <param name="isSuccess">操作状态是否成功</param>
        private void ShowOperationStatus(bool isSuccess)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (OperationStatus != null)
                {
                    if (isSuccess)
                    {
                        OperationStatus.Text = $"{DateTime.Now} | 操作成功";
                    }
                    else
                    {
                        OperationStatus.Text = $"{DateTime.Now} | 操作失败";
                    }
                }
            });
        }
        /// <summary>
        /// 显示操作提示
        /// </summary>
        /// <param name="text">提示文本</param>
        private void ShowOperationStatus(string text)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (OperationStatus != null)
                {
                    OperationStatus.Text = $"{DateTime.Now} | " + text;
                }
            });
        }
        /// <summary>
        /// 读取用户配置文件并加载到UI
        /// </summary>
        private void InitPropertiesSettingsUI()
        {
            SetAngleRangeTextBlock.Text = Properties.Settings.Default.Steering_Wheel_Rotation_Range.ToString() + "°";
            SetAngleRangeSlider.Value = Properties.Settings.Default.Steering_Wheel_Rotation_Range;
            LeftAngleBar.Maximum = Properties.Settings.Default.Steering_Wheel_Rotation_Range / 2;
            RightAngleBar.Maximum = Properties.Settings.Default.Steering_Wheel_Rotation_Range / 2;
            SetEncoderPulseTextBox.Text = Properties.Settings.Default.Encoder_Pulse.ToString();
            SetSpringGainTextBox.Text = Properties.Settings.Default.Effect_Gain_Controller_Spring_Gain.ToString();
            SetDamperGainTextBox.Text = Properties.Settings.Default.Effect_Gain_Controller_Damper_Gain.ToString();
            SetFrictionGainTextBox.Text = Properties.Settings.Default.Effect_Gain_Controller_Friction_Gain.ToString();
            SetInertiaGainTextBox.Text = Properties.Settings.Default.Effect_Gain_Controller_Inertia_Gain.ToString();
            SetInertiaLimiterTextBox.Text = Properties.Settings.Default.Effect_Limiter_Inertia_Limiter.ToString();
            SetSpringKpTextBox.Text = Properties.Settings.Default.Effect_Spring_PID_Kp.ToString();
            SetSpringKiTextBox.Text = Properties.Settings.Default.Effect_Spring_PID_Ki.ToString();
            SetSpringKdTextBox.Text = Properties.Settings.Default.Effect_Spring_PID_Kd.ToString();
            SetWheelSLKpTextBox.Text = Properties.Settings.Default.Steering_Wheel_Software_Limiter_Kp.ToString();
            SetWheelSLKiTextBox.Text = Properties.Settings.Default.Steering_Wheel_Software_Limiter_Ki.ToString();
            SetWheelSLKdTextBox.Text = Properties.Settings.Default.Steering_Wheel_Software_Limiter_Kd.ToString();
            SetPWMGainTextBox.Text = Properties.Settings.Default.PWM_Gain_Multiple_Value.ToString();

            SetLEDBrightnessTextBlock.Text = Properties.Settings.Default.Led_Brightness.ToString();
            SetLEDBrightnessSlider.Value = Properties.Settings.Default.Led_Brightness;

            LedModeColorColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.Led_Color_Mode_Color_String);

            switch ((VMCBLEGamepadDevice.VMCLedModeType)Properties.Settings.Default.Led_Mode)
            {
                case VMCBLEGamepadDevice.VMCLedModeType.color:
                    LedModeColorRadioButton.IsChecked = true;
                    LedModeColorGrid.Visibility = Visibility.Visible;
                    LedModeRainbowGrid.Visibility = Visibility.Collapsed;
                    break;
                case VMCBLEGamepadDevice.VMCLedModeType.breath:
                    LedModeBreathRadioButton.IsChecked = true;
                    LedModeColorGrid.Visibility = Visibility.Visible;
                    LedModeRainbowGrid.Visibility = Visibility.Collapsed;
                    break;
                case VMCBLEGamepadDevice.VMCLedModeType.rainbow:
                    LedModeRainbowRadioButton.IsChecked = true;
                    LedModeColorGrid.Visibility = Visibility.Collapsed;
                    LedModeRainbowGrid.Visibility = Visibility.Visible;
                    break;
                case VMCBLEGamepadDevice.VMCLedModeType.bounce:
                    LedModeBounceRadioButton.IsChecked = true;
                    LedModeColorGrid.Visibility = Visibility.Visible;
                    LedModeRainbowGrid.Visibility = Visibility.Collapsed;
                    break;
                case VMCBLEGamepadDevice.VMCLedModeType.rainbow_gradient:
                    LedModeRainbowGradientRadioButton.IsChecked = true;
                    LedModeColorGrid.Visibility = Visibility.Collapsed;
                    LedModeRainbowGrid.Visibility = Visibility.Collapsed;
                    break;
            }
            LedGameModeCheckBox.IsChecked = Properties.Settings.Default.Led_Game_Mode;


            SetJoystick1DeadzoneSlider.Value = Properties.Settings.Default.Joystick_1_Deadzone;
            SetJoystick1DeadzoneTextblock.Text = Properties.Settings.Default.Joystick_1_Deadzone.ToString();
            SetJoystick2DeadzoneSlider.Value = Properties.Settings.Default.Joystick_2_Deadzone;
            SetJoystick2DeadzoneTextblock.Text = Properties.Settings.Default.Joystick_2_Deadzone.ToString();

            SetJoystick1MaxValueSlider.Value = Properties.Settings.Default.Joystick_1_MaxValue;
            SetJoystick1MaxValueTextblock.Text = Properties.Settings.Default.Joystick_1_MaxValue.ToString();
            SetJoystick2MaxValueSlider.Value = Properties.Settings.Default.Joystick_2_MaxValue;
            SetJoystick2MaxValueTextblock.Text = Properties.Settings.Default.Joystick_2_MaxValue.ToString();


        }




        private void OnClickResetCenterPointButton(object sender, RoutedEventArgs e)
        {
            if (_vmcMainDevice.hidMangager.IsDeviceOpened())
            {
                _vmcMainDevice.SendVMCCommand(VMCMainDevice.VMCCommandType.cmd_steering_wheel_set_center, [0]);
                ShowOperationStatus(true);
            }
            else
            {
                ShowOperationStatus(false);
            }
        }

        //private void OnClickOpenETS2ToolsButton(object sender, RoutedEventArgs e)
        //{
        //    ETS2Tools eTS2Tools = new ETS2Tools();
        //    eTS2Tools.Show();
        //    ShowOperationStatus(true);
        //}

        private void OnClickSetAccelerateMaximum(object sender, RoutedEventArgs e)
        {
            if (_vmcMainDevice.hidMangager.IsDeviceOpened())
            {
                var respData = _vmcMainDevice.GetResponding(VMCMainDevice.VMCRespondingType.resp_accelerator_pedal_maximum, [0]);

                if (respData != null)
                {
                    Properties.Settings.Default.Accelerator_Pedal_Maximum = (UInt16)(respData[2] | (respData[3] << 8)); ;
                    Properties.Settings.Default.Save();
                    ShowOperationStatus(true);
                }
                else
                {
                    ShowOperationStatus(false);
                }
            }
            else
            {
                ShowOperationStatus(false);
            }
        }

        private void OnClickSetAccelerateMinimum(object sender, RoutedEventArgs e)
        {
            if (_vmcMainDevice.hidMangager.IsDeviceOpened())
            {
                var respData = _vmcMainDevice.GetResponding(VMCMainDevice.VMCRespondingType.resp_accelerator_pedal_minimum, [0]);

                if (respData != null)
                {
                    Properties.Settings.Default.Accelerator_Pedal_Minimum = (UInt16)(respData[2] | (respData[3] << 8)); ;
                    Properties.Settings.Default.Save();
                    ShowOperationStatus(true);
                }
                else
                {
                    ShowOperationStatus(false);
                }
            }
            else
            {
                ShowOperationStatus(false);
            }
        }

        private void OnClickSetBrakeMaximum(object sender, RoutedEventArgs e)
        {
            if (_vmcMainDevice.hidMangager.IsDeviceOpened())
            {
                var respData = _vmcMainDevice.GetResponding(VMCMainDevice.VMCRespondingType.resp_brake_pedal_maximum, [0]);

                if (respData != null)
                {
                    Properties.Settings.Default.Brake_Pedal_Maximum = (UInt16)(respData[2] | (respData[3] << 8)); ;
                    Properties.Settings.Default.Save();
                    ShowOperationStatus(true);
                }
                else
                {
                    ShowOperationStatus(false);
                }
            }
            else
            {
                ShowOperationStatus(false);
            }
        }

        private void OnClickSetBrakeMinimum(object sender, RoutedEventArgs e)
        {
            if (_vmcMainDevice.hidMangager.IsDeviceOpened())
            {
                var respData = _vmcMainDevice.GetResponding(VMCMainDevice.VMCRespondingType.resp_brake_pedal_minimum, [0]);

                if (respData != null)
                {
                    Properties.Settings.Default.Brake_Pedal_Minimum = (UInt16)(respData[2] | (respData[3] << 8)); ;
                    Properties.Settings.Default.Save();
                    ShowOperationStatus(true);
                }
                else
                {
                    ShowOperationStatus(false);
                }
            }
            else
            {
                ShowOperationStatus(false);
            }
        }

        private void OnClickSetClutchMaximum(object sender, RoutedEventArgs e)
        {
            if (_vmcMainDevice.hidMangager.IsDeviceOpened())
            {
                var respData = _vmcMainDevice.GetResponding(VMCMainDevice.VMCRespondingType.resp_clutch_pedal_maximum, [0]);

                if (respData != null)
                {
                    Properties.Settings.Default.Clutch_Pedal_Maximum = (UInt16)(respData[2] | (respData[3] << 8)); ;
                    Properties.Settings.Default.Save();
                    ShowOperationStatus(true);
                }
                else
                {
                    ShowOperationStatus(false);
                }
            }
            else
            {
                ShowOperationStatus(false);
            }
        }

        private void OnClickSetClutchMinimum(object sender, RoutedEventArgs e)
        {
            if (_vmcMainDevice.hidMangager.IsDeviceOpened())
            {
                var respData = _vmcMainDevice.GetResponding(VMCMainDevice.VMCRespondingType.resp_clutch_pedal_minimum, [0]);

                if (respData != null)
                {
                    Properties.Settings.Default.Clutch_Pedal_Minimum = (UInt16)(respData[2] | (respData[3] << 8)); ;
                    Properties.Settings.Default.Save();
                    ShowOperationStatus(true);
                }
                else
                {
                    ShowOperationStatus(false);
                }
            }
            else
            {
                ShowOperationStatus(false);
            }
        }

        private void OnValueChangedSetAngleRangeSlider(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vmcMainDevice.hidMangager.IsDeviceOpened())
            {
                if (IsLoaded)
                {
                    ushort value = Convert.ToUInt16(SetAngleRangeSlider.Value);
                    Properties.Settings.Default.Steering_Wheel_Rotation_Range = value;
                    SetAngleRangeTextBlock.Text = value.ToString() + "°";

                    _vmcMainDevice.SendVMCCommand(VMCMainDevice.VMCCommandType.cmd_steering_wheel_set_rotation_range, value);


                    LeftAngleBar.Maximum = value / 2;
                    RightAngleBar.Maximum = value / 2;

                    Properties.Settings.Default.Save();
                    ShowOperationStatus(true);
                }
            }
            else
            {
                ShowOperationStatus(false);
            }
        }

        private void SetEncoderPulseTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 允许的数字键（主键盘0-9）和小键盘的数字键
            bool isNumberKey = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);

            // 允许的控制键，如退格键、删除键、方向键等
            bool isControlKey = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right;

            // 允许回车键用于确认
            if (e.Key == Key.Enter)
            {
                try
                {
                    var value = Convert.ToUInt16(SetEncoderPulseTextBox.Text);
                    Properties.Settings.Default.Encoder_Pulse = value;
                    Properties.Settings.Default.Save();
                    _vmcMainDevice.SendVMCCommand(VMCMainDevice.VMCCommandType.cmd_steering_wheel_encoder_set_pulse, value);
                    ShowOperationStatus("设置值： " + SetEncoderPulseTextBox.Text + " - 成功");
                }
                catch
                {
                    ShowOperationStatus("设置值： " + SetEncoderPulseTextBox.Text + " - 失败 - 输入范围 0~65535");
                }


            }

            // 如果不是数字键或控制键，则阻止输入
            e.Handled = !(isNumberKey || isControlKey);
        }

        private void SetSpringGainTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 允许的数字键（主键盘0-9）和小键盘的数字键
            bool isNumberKey = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);

            // 允许的控制键，如退格键、删除键、方向键等
            bool isControlKey = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right;

            // 允许回车键用于确认
            if (e.Key == Key.Enter)
            {
                var text = SetSpringGainTextBox.Text;
                try
                {

                    var value = Convert.ToUInt16(text);
                    Properties.Settings.Default.Effect_Gain_Controller_Spring_Gain = value;
                    Properties.Settings.Default.Save();
                    _vmcMainDevice.SendVMCCommand(VMCMainDevice.VMCCommandType.cmd_effect_gain_controller_set_spring_gain, value);
                    ShowOperationStatus("设置值： " + text + " - 成功");
                }
                catch
                {
                    ShowOperationStatus("设置值： " + text + " - 失败 - 输入范围 0~65535");
                }
            }

            // 如果不是数字键或控制键，则阻止输入
            e.Handled = !(isNumberKey || isControlKey);
        }

        private void SetDamperGainTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 允许的数字键（主键盘0-9）和小键盘的数字键
            bool isNumberKey = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);

            // 允许的控制键，如退格键、删除键、方向键等
            bool isControlKey = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right;

            // 允许回车键用于确认
            if (e.Key == Key.Enter)
            {
                var text = SetDamperGainTextBox.Text;
                try
                {

                    var value = Convert.ToUInt16(text);
                    Properties.Settings.Default.Effect_Gain_Controller_Damper_Gain = value;
                    Properties.Settings.Default.Save();
                    _vmcMainDevice.SendVMCCommand(VMCMainDevice.VMCCommandType.cmd_effect_gain_controller_set_damper_gain, value);
                    ShowOperationStatus("设置值： " + text + " - 成功");
                }
                catch
                {
                    ShowOperationStatus("设置值： " + text + " - 失败 - 输入范围 0~65535");
                }
            }

            // 如果不是数字键或控制键，则阻止输入
            e.Handled = !(isNumberKey || isControlKey);
        }

        private void SetFrictionGainTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 允许的数字键（主键盘0-9）和小键盘的数字键
            bool isNumberKey = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);

            // 允许的控制键，如退格键、删除键、方向键等
            bool isControlKey = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right;

            // 允许回车键用于确认
            if (e.Key == Key.Enter)
            {
                var text = SetFrictionGainTextBox.Text;
                try
                {

                    var value = Convert.ToUInt16(text);
                    Properties.Settings.Default.Effect_Gain_Controller_Friction_Gain = value;
                    Properties.Settings.Default.Save();
                    _vmcMainDevice.SendVMCCommand(VMCMainDevice.VMCCommandType.cmd_effect_gain_controller_set_friction_gain, value);
                    ShowOperationStatus("设置值： " + text + " - 成功");
                }
                catch
                {
                    ShowOperationStatus("设置值： " + text + " - 失败 - 输入范围 0~65535");
                }
            }

            // 如果不是数字键或控制键，则阻止输入
            e.Handled = !(isNumberKey || isControlKey);
        }

        private void SetInertiaGainTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 允许的数字键（主键盘0-9）和小键盘的数字键
            bool isNumberKey = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);

            // 允许的控制键，如退格键、删除键、方向键等
            bool isControlKey = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right;

            // 允许回车键用于确认
            if (e.Key == Key.Enter)
            {
                var text = SetInertiaGainTextBox.Text;
                try
                {

                    var value = Convert.ToUInt16(text);
                    Properties.Settings.Default.Effect_Gain_Controller_Inertia_Gain = value;
                    Properties.Settings.Default.Save();
                    _vmcMainDevice.SendVMCCommand(VMCMainDevice.VMCCommandType.cmd_effect_gain_controller_set_inertia_gain, value);
                    ShowOperationStatus("设置值： " + text + " - 成功");
                }
                catch
                {
                    ShowOperationStatus("设置值： " + text + " - 失败 - 输入范围 0~65535");
                }
            }

            // 如果不是数字键或控制键，则阻止输入
            e.Handled = !(isNumberKey || isControlKey);
        }

        private void SetInertiaLimiterTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var text = SetInertiaLimiterTextBox.Text;
            // 允许的数字键（主键盘0-9）和小键盘的数字键
            bool isNumberKey = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);

            // 允许的控制键，如退格键、删除键、方向键等
            bool isControlKey = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right;

            // 允许小数点键（.）
            bool isDecimalPoint = e.Key == Key.OemPeriod || e.Key == Key.Decimal;
            // 允许回车键用于确认
            if (e.Key == Key.Enter)
            {

                try
                {

                    var value = float.Parse(text);

                    Properties.Settings.Default.Effect_Limiter_Inertia_Limiter = value;
                    Properties.Settings.Default.Save();
                    _vmcMainDevice.SendVMCCommand(VMCMainDevice.VMCCommandType.cmd_effect_limiter_set_inertia_limiter, value);
                    ShowOperationStatus("设置值： " + text + " - 成功");
                }
                catch
                {
                    ShowOperationStatus("设置值： " + text + " - 失败 - 请输入数字（浮点型）");
                }
            }
            // 允许小数点时，检查当前文本框是否已有小数点
            if (isDecimalPoint)
            {
                if (text.Contains("."))
                {
                    e.Handled = true;  // 如果已经包含小数点，阻止再次输入小数点
                }
                else
                {
                    e.Handled = false;  // 允许输入小数点
                }
            }
            // 如果是数字或控制键，允许输入
            else if (isNumberKey || isControlKey)
            {
                e.Handled = false;
            }
            // 否则，阻止输入
            else
            {
                e.Handled = true;
            }
        }

        private void SetSpringKpTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var text = SetSpringKpTextBox.Text;
            // 允许的数字键（主键盘0-9）和小键盘的数字键
            bool isNumberKey = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);

            // 允许的控制键，如退格键、删除键、方向键等
            bool isControlKey = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right;

            // 允许小数点键（.）
            bool isDecimalPoint = e.Key == Key.OemPeriod || e.Key == Key.Decimal;
            // 允许回车键用于确认
            if (e.Key == Key.Enter)
            {

                try
                {

                    var value = float.Parse(text);
                    Properties.Settings.Default.Effect_Spring_PID_Kp = value;
                    Properties.Settings.Default.Save();
                    _vmcMainDevice.SendVMCCommand(VMCMainDevice.VMCCommandType.cmd_effect_set_spring_kp, value);
                    ShowOperationStatus("设置值： " + text + " - 成功");
                }
                catch
                {
                    ShowOperationStatus("设置值： " + text + " - 失败 - 请输入数字（浮点型）");
                }
            }
            // 允许小数点时，检查当前文本框是否已有小数点
            if (isDecimalPoint)
            {
                if (text.Contains("."))
                {
                    e.Handled = true;  // 如果已经包含小数点，阻止再次输入小数点
                }
                else
                {
                    e.Handled = false;  // 允许输入小数点
                }
            }
            // 如果是数字或控制键，允许输入
            else if (isNumberKey || isControlKey)
            {
                e.Handled = false;
            }
            // 否则，阻止输入
            else
            {
                e.Handled = true;
            }
        }

        private void SetSpringKiTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var text = SetSpringKiTextBox.Text;
            // 允许的数字键（主键盘0-9）和小键盘的数字键
            bool isNumberKey = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);

            // 允许的控制键，如退格键、删除键、方向键等
            bool isControlKey = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right;

            // 允许小数点键（.）
            bool isDecimalPoint = e.Key == Key.OemPeriod || e.Key == Key.Decimal;
            // 允许回车键用于确认
            if (e.Key == Key.Enter)
            {

                try
                {

                    var value = float.Parse(text);

                    Properties.Settings.Default.Effect_Spring_PID_Ki = value;
                    Properties.Settings.Default.Save();
                    _vmcMainDevice.SendVMCCommand(VMCMainDevice.VMCCommandType.cmd_effect_set_spring_ki, value);
                    ShowOperationStatus("设置值： " + text + " - 成功");
                }
                catch
                {
                    ShowOperationStatus("设置值： " + text + " - 失败 - 请输入数字（浮点型）");
                }
            }
            // 允许小数点时，检查当前文本框是否已有小数点
            if (isDecimalPoint)
            {
                if (text.Contains("."))
                {
                    e.Handled = true;  // 如果已经包含小数点，阻止再次输入小数点
                }
                else
                {
                    e.Handled = false;  // 允许输入小数点
                }
            }
            // 如果是数字或控制键，允许输入
            else if (isNumberKey || isControlKey)
            {
                e.Handled = false;
            }
            // 否则，阻止输入
            else
            {
                e.Handled = true;
            }
        }

        private void SetSpringKdTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var text = SetSpringKdTextBox.Text;
            // 允许的数字键（主键盘0-9）和小键盘的数字键
            bool isNumberKey = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);

            // 允许的控制键，如退格键、删除键、方向键等
            bool isControlKey = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right;

            // 允许小数点键（.）
            bool isDecimalPoint = e.Key == Key.OemPeriod || e.Key == Key.Decimal;
            // 允许回车键用于确认
            if (e.Key == Key.Enter)
            {

                try
                {

                    var value = float.Parse(text);

                    Properties.Settings.Default.Effect_Spring_PID_Kd = value;
                    Properties.Settings.Default.Save();
                    _vmcMainDevice.SendVMCCommand(VMCMainDevice.VMCCommandType.cmd_effect_set_spring_kd, value);
                    ShowOperationStatus("设置值： " + text + " - 成功");
                }
                catch
                {
                    ShowOperationStatus("设置值： " + text + " - 失败 - 请输入数字（浮点型）");
                }
            }
            // 允许小数点时，检查当前文本框是否已有小数点
            if (isDecimalPoint)
            {
                if (text.Contains("."))
                {
                    e.Handled = true;  // 如果已经包含小数点，阻止再次输入小数点
                }
                else
                {
                    e.Handled = false;  // 允许输入小数点
                }
            }
            // 如果是数字或控制键，允许输入
            else if (isNumberKey || isControlKey)
            {
                e.Handled = false;
            }
            // 否则，阻止输入
            else
            {
                e.Handled = true;
            }
        }

        private void SetWheelSLKpTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var text = SetWheelSLKpTextBox.Text;
            // 允许的数字键（主键盘0-9）和小键盘的数字键
            bool isNumberKey = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);

            // 允许的控制键，如退格键、删除键、方向键等
            bool isControlKey = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right;

            // 允许小数点键（.）
            bool isDecimalPoint = e.Key == Key.OemPeriod || e.Key == Key.Decimal;
            // 允许回车键用于确认
            if (e.Key == Key.Enter)
            {

                try
                {

                    var value = float.Parse(text);

                    Properties.Settings.Default.Steering_Wheel_Software_Limiter_Kp = value;
                    Properties.Settings.Default.Save();
                    _vmcMainDevice.SendVMCCommand(VMCMainDevice.VMCCommandType.cmd_steering_wheel_software_limiter_set_kp, value);
                    ShowOperationStatus("设置值： " + text + " - 成功");
                }
                catch
                {
                    ShowOperationStatus("设置值： " + text + " - 失败 - 请输入数字（浮点型）");
                }
            }
            // 允许小数点时，检查当前文本框是否已有小数点
            if (isDecimalPoint)
            {
                if (text.Contains("."))
                {
                    e.Handled = true;  // 如果已经包含小数点，阻止再次输入小数点
                }
                else
                {
                    e.Handled = false;  // 允许输入小数点
                }
            }
            // 如果是数字或控制键，允许输入
            else if (isNumberKey || isControlKey)
            {
                e.Handled = false;
            }
            // 否则，阻止输入
            else
            {
                e.Handled = true;
            }
        }

        private void SetWheelSLKiTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var text = SetWheelSLKiTextBox.Text;
            // 允许的数字键（主键盘0-9）和小键盘的数字键
            bool isNumberKey = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);

            // 允许的控制键，如退格键、删除键、方向键等
            bool isControlKey = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right;

            // 允许小数点键（.）
            bool isDecimalPoint = e.Key == Key.OemPeriod || e.Key == Key.Decimal;
            // 允许回车键用于确认
            if (e.Key == Key.Enter)
            {

                try
                {

                    var value = float.Parse(text);

                    Properties.Settings.Default.Steering_Wheel_Software_Limiter_Ki = value;
                    Properties.Settings.Default.Save();
                    _vmcMainDevice.SendVMCCommand(VMCMainDevice.VMCCommandType.cmd_steering_wheel_software_limiter_set_ki, value);
                    ShowOperationStatus("设置值： " + text + " - 成功");
                }
                catch
                {
                    ShowOperationStatus("设置值： " + text + " - 失败 - 请输入数字（浮点型）");
                }
            }
            // 允许小数点时，检查当前文本框是否已有小数点
            if (isDecimalPoint)
            {
                if (text.Contains("."))
                {
                    e.Handled = true;  // 如果已经包含小数点，阻止再次输入小数点
                }
                else
                {
                    e.Handled = false;  // 允许输入小数点
                }
            }
            // 如果是数字或控制键，允许输入
            else if (isNumberKey || isControlKey)
            {
                e.Handled = false;
            }
            // 否则，阻止输入
            else
            {
                e.Handled = true;
            }
        }

        private void SetWheelSLKdTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var text = SetWheelSLKdTextBox.Text;
            // 允许的数字键（主键盘0-9）和小键盘的数字键
            bool isNumberKey = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);

            // 允许的控制键，如退格键、删除键、方向键等
            bool isControlKey = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right;

            // 允许小数点键（.）
            bool isDecimalPoint = e.Key == Key.OemPeriod || e.Key == Key.Decimal;
            // 允许回车键用于确认
            if (e.Key == Key.Enter)
            {

                try
                {

                    var value = float.Parse(text);

                    Properties.Settings.Default.Steering_Wheel_Software_Limiter_Kd = value;
                    Properties.Settings.Default.Save();
                    _vmcMainDevice.SendVMCCommand(VMCMainDevice.VMCCommandType.cmd_steering_wheel_software_limiter_set_kd, value);
                    ShowOperationStatus("设置值： " + text + " - 成功");
                }
                catch
                {
                    ShowOperationStatus("设置值： " + text + " - 失败 - 请输入数字（浮点型）");
                }
            }
            // 允许小数点时，检查当前文本框是否已有小数点
            if (isDecimalPoint)
            {
                if (text.Contains("."))
                {
                    e.Handled = true;  // 如果已经包含小数点，阻止再次输入小数点
                }
                else
                {
                    e.Handled = false;  // 允许输入小数点
                }
            }
            // 如果是数字或控制键，允许输入
            else if (isNumberKey || isControlKey)
            {
                e.Handled = false;
            }
            // 否则，阻止输入
            else
            {
                e.Handled = true;
            }
        }

        private void SetPWMGainTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var text = SetPWMGainTextBox.Text;
            // 允许的数字键（主键盘0-9）和小键盘的数字键
            bool isNumberKey = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);

            // 允许的控制键，如退格键、删除键、方向键等
            bool isControlKey = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right;

            // 允许小数点键（.）
            bool isDecimalPoint = e.Key == Key.OemPeriod || e.Key == Key.Decimal;
            // 允许回车键用于确认
            if (e.Key == Key.Enter)
            {

                try
                {

                    var value = float.Parse(text);

                    Properties.Settings.Default.PWM_Gain_Multiple_Value = value;
                    Properties.Settings.Default.Save();
                    _vmcMainDevice.SendVMCCommand(VMCMainDevice.VMCCommandType.cmd_pwm_global_parameters_set_pwm_gain_multiple, value);
                    ShowOperationStatus("设置值： " + text + " - 成功");
                }
                catch
                {
                    ShowOperationStatus("设置值： " + text + " - 失败 - 请输入数字（浮点型）");
                }
            }
            // 允许小数点时，检查当前文本框是否已有小数点
            if (isDecimalPoint)
            {
                if (text.Contains("."))
                {
                    e.Handled = true;  // 如果已经包含小数点，阻止再次输入小数点
                }
                else
                {
                    e.Handled = false;  // 允许输入小数点
                }
            }
            // 如果是数字或控制键，允许输入
            else if (isNumberKey || isControlKey)
            {
                e.Handled = false;
            }
            // 否则，阻止输入
            else
            {
                e.Handled = true;
            }
        }

        private void OnValueChangedSetLEDBrightnessSlider(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vmcBleGamepadDevice.hidMangager.IsDeviceOpened())
            {
                if (IsLoaded)
                {
                    byte value = Convert.ToByte(SetLEDBrightnessSlider.Value);
                    Properties.Settings.Default.Led_Brightness = value;
                    SetLEDBrightnessTextBlock.Text = value.ToString();

                    _vmcBleGamepadDevice.SetLedBrightness(value);

                    Properties.Settings.Default.Save();
                    ShowOperationStatus(true);
                }
            }
            else
            {
                ShowOperationStatus(false);
            }
        }

        private void OnCheckedLedModeRadioButton(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                if (sender == LedModeColorRadioButton)
                {
                    LedModeColorGrid.Visibility = Visibility.Visible;
                    //LedModeBreathGrid.Visibility = Visibility.Collapsed;
                    LedModeRainbowGrid.Visibility = Visibility.Collapsed;
                    //LedModeBounceGrid.Visibility = Visibility.Collapsed;
                    _vmcBleGamepadDevice.SetLedMode(VMCBLEGamepadDevice.VMCLedModeType.color);
                }
                else if (sender == LedModeBreathRadioButton)
                {
                    LedModeColorGrid.Visibility = Visibility.Visible;
                    //LedModeBreathGrid.Visibility = Visibility.Visible;
                    LedModeRainbowGrid.Visibility = Visibility.Collapsed;
                    //LedModeBounceGrid.Visibility = Visibility.Collapsed;
                    _vmcBleGamepadDevice.SetLedMode(VMCBLEGamepadDevice.VMCLedModeType.breath);
                }
                else if (sender == LedModeRainbowRadioButton)
                {
                    LedModeColorGrid.Visibility = Visibility.Collapsed;
                    //LedModeBreathGrid.Visibility = Visibility.Collapsed;
                    LedModeRainbowGrid.Visibility = Visibility.Visible;
                    //LedModeBounceGrid.Visibility = Visibility.Collapsed;
                    _vmcBleGamepadDevice.SetLedMode(VMCBLEGamepadDevice.VMCLedModeType.rainbow);
                }
                else if (sender == LedModeBounceRadioButton)
                {
                    LedModeColorGrid.Visibility = Visibility.Visible;
                    //LedModeBreathGrid.Visibility = Visibility.Collapsed;
                    LedModeRainbowGrid.Visibility = Visibility.Collapsed;
                    //LedModeBounceGrid.Visibility = Visibility.Visible;
                    _vmcBleGamepadDevice.SetLedMode(VMCBLEGamepadDevice.VMCLedModeType.bounce);
                }
                else if (sender == LedModeRainbowGradientRadioButton)
                {
                    LedModeColorGrid.Visibility = Visibility.Collapsed;
                    //LedModeBreathGrid.Visibility = Visibility.Collapsed;
                    LedModeRainbowGrid.Visibility = Visibility.Collapsed;
                    //LedModeBounceGrid.Visibility = Visibility.Visible;
                    _vmcBleGamepadDevice.SetLedMode(VMCBLEGamepadDevice.VMCLedModeType.rainbow_gradient);
                }
            }
        }

        private void LedModeColorColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            Color? selectedColor = LedModeColorColorPicker.SelectedColor;
            if (selectedColor.HasValue)
            {
                _vmcBleGamepadDevice.SetColorModeColor(selectedColor.Value);
            }
        }

        private void LedGameModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _vmcBleGamepadDevice.SetGameMode(false);
        }

        private void LedGameModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _vmcBleGamepadDevice.SetGameMode(true);
        }

        private void SetJoystick1DeadzoneSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vmcBleGamepadDevice.hidMangager.IsDeviceOpened())
            {
                if (IsLoaded)
                {
                    UInt16 value = Convert.ToUInt16(SetJoystick1DeadzoneSlider.Value);
                    Properties.Settings.Default.Joystick_1_Deadzone = value;
                    SetJoystick1DeadzoneTextblock.Text = value.ToString();
                    SetJoystick1MaxValueSlider.Minimum = value;
                    _vmcBleGamepadDevice.SetJoystickAxesDeadzone(1, value);

                    Properties.Settings.Default.Save();
                    ShowOperationStatus(true);
                }
            }
            else
            {
                ShowOperationStatus(false);
            }
        }



        private void SetJoystick2DeadzoneSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vmcBleGamepadDevice.hidMangager.IsDeviceOpened())
            {
                if (IsLoaded)
                {
                    UInt16 value = Convert.ToUInt16(SetJoystick2DeadzoneSlider.Value);
                    Properties.Settings.Default.Joystick_2_Deadzone = value;
                    SetJoystick2DeadzoneTextblock.Text = value.ToString();
                    SetJoystick2MaxValueSlider.Minimum = value;
                    _vmcBleGamepadDevice.SetJoystickAxesDeadzone(2, value);

                    Properties.Settings.Default.Save();
                    ShowOperationStatus(true);
                }
            }
            else
            {
                ShowOperationStatus(false);
            }
        }


        private void SetJoystick1MaxValueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vmcBleGamepadDevice.hidMangager.IsDeviceOpened())
            {
                if (IsLoaded)
                {
                    UInt16 value = Convert.ToUInt16(SetJoystick1MaxValueSlider.Value);
                    Properties.Settings.Default.Joystick_1_MaxValue = value;
                    SetJoystick1MaxValueTextblock.Text = value.ToString();
                    _vmcBleGamepadDevice.SetJoystickAxesMaxValue(1, value);

                    Properties.Settings.Default.Save();
                    ShowOperationStatus(true);
                }
            }
            else
            {
                ShowOperationStatus(false);
            }
        }


        private void SetJoystick2MaxValueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vmcBleGamepadDevice.hidMangager.IsDeviceOpened())
            {
                if (IsLoaded)
                {
                    UInt16 value = Convert.ToUInt16(SetJoystick2MaxValueSlider.Value);
                    Properties.Settings.Default.Joystick_2_MaxValue = value;
                    SetJoystick2MaxValueTextblock.Text = value.ToString();

                    _vmcBleGamepadDevice.SetJoystickAxesMaxValue(2, value);

                    Properties.Settings.Default.Save();
                    ShowOperationStatus(true);
                }
            }
            else
            {
                ShowOperationStatus(false);
            }
        }

    }
}
