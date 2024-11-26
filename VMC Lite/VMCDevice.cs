using System.Diagnostics;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace VMC_Lite
{
    public class VMCDevice
    {
        public class VMCMainDevice
        {
            //TODO 需要改为继承，继承HID Manager的方法
            private static VMCMainDevice _instance;
            public HIDManager hidMangager;
            private VMCMainDevice()
            {
                hidMangager = new HIDManager(Device.Vid, Device.Pid);
            }
            /// <summary>
            /// 确保只有一个实例
            /// </summary>
            public static VMCMainDevice Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = new VMCMainDevice();
                    }
                    return _instance;
                }
            }
            public class Device
            {
                public const UInt16 Vid = 0x1234;
                public const UInt16 Pid = 0x0064;
                public const string FirmwareVersion = "1.0";
                public const UInt16 maxButtons = 32;
            }
            public class ReportId
            {
                public const byte PID_DEVICE_CONTROL_REPORT_ID = 0x33;
                public const byte VMC_REPORT_ID = 0x60;
                public const byte INPUT_REPORT_ID = 0x01;
                public const byte VMC_RESPONDING_REPORT_ID = 0x61;
            }
            public bool Initialized = false;
            public enum VMCCommandType
            {
                cmd_unknown_command,
                cmd_steering_wheel_set_center,                          // 设置当前角度为中心
                cmd_steering_wheel_set_rotation_range,      // 设置方向盘最大旋转角度，正向+反向
                cmd_steering_wheel_encoder_set_pulse,           // 设置编码器脉冲
                cmd_accelerator_pedal_set_maximum,          // 设置当前值为加速踏板的最大值
                cmd_accelerator_pedal_set_minimum,              // 设置当前值为加速踏板的最小值
                cmd_brake_pedal_set_maximum,                        // 设置当前值为刹车踏板的最大值
                cmd_brake_pedal_set_minimum,                        // 设置当前值为刹车踏板的最小值
                cmd_clutch_pedal_set_maximum,                       // 设置当前值为离合器踏板的最大值
                cmd_clutch_pedal_set_minimum,                       // 设置当前值为离合器踏板的最小值
                cmd_effect_gain_controller_set_spring_gain,         // 设置力反馈效果 - Spring效果的增益
                cmd_effect_gain_controller_set_damper_gain,     // 设置力反馈效果 - Damper效果的增益
                cmd_effect_gain_controller_set_friction_gain,       // 设置力反馈效果 - Friction弹簧效果的增益
                cmd_effect_gain_controller_set_inertia_gain,            // 设置力反馈效果 - Inertia效果的增益
                cmd_effect_limiter_set_inertia_limiter,                     // 设置力反馈效果 - Inertia效果的限位
                cmd_effect_set_spring_kp,       // 设置力反馈效果 - Spring PID的Kp
                cmd_effect_set_spring_ki,       // 设置力反馈效果 - Spring PID的Ki
                cmd_effect_set_spring_kd,       // 设置力反馈效果 - Spring PID的Kd
                cmd_steering_wheel_software_limiter_set_kp, // 设置方向盘软限位 PID 的Kp
                cmd_steering_wheel_software_limiter_set_ki, // 设置方向盘软限位 PID 的Ki
                cmd_steering_wheel_software_limiter_set_kd, // 设置方向盘软限位 PID 的Kd
                cmd_pwm_global_parameters_set_pwm_gain_multiple,        // 设置力反馈输出PWM的增益倍数
                cmd_steering_wheel_software_limiter_set_vibration_feedback_enable,      // 设置方向盘软限位的震动反馈使能
                cmd_steering_wheel_software_limiter_set_vibration_feedback_delay			// 设置方向盘软限位的震动反馈
            }
            public enum VMCRespondingType
            {
                resp_unknown,
                resp_accelerator_pedal_maximum,
                resp_accelerator_pedal_minimum,
                resp_brake_pedal_maximum,
                resp_brake_pedal_minimum,
                resp_clutch_pedal_maximum,
                resp_clutch_pedal_minimum
            }
            /// <summary>
            /// 软限位震动反馈类型
            /// </summary>
            public enum SLVibrationFeedbackType
            {
                SL_Vibration_Feedback_OFF,
                SL_Vibration_Feedback_CONSTANT,
            }

            /// <summary>
            /// 发送VMC命令
            /// </summary>
            /// <param name="cmd">命令</param>
            /// <param name="data">字节 命令对应的参数</param>
            public bool SendVMCCommand(VMCCommandType cmd, byte data)
            {
                return hidMangager.SendOutputReport(ReportId.VMC_REPORT_ID, [(byte)cmd, data]);
            }
            /// <summary>
            /// 发送VMC命令
            /// </summary>
            /// <param name="cmd">命令</param>
            /// <param name="data">字节数组 命令对应的参数</param>
            public bool SendVMCCommand(VMCCommandType cmd, byte[] data)
            {
                byte[] cmdData = new byte[1 + data.Length];
                cmdData[0] = (byte)cmd;
                Array.Copy(data, 0, cmdData, 1, data.Length);
                return hidMangager.SendOutputReport(ReportId.VMC_REPORT_ID, cmdData);
            }
            /// <summary>
            /// 发送VMC命令
            /// </summary>
            /// <param name="cmd">命令</param>
            /// <param name="value">16位无符号整形 命令对应的参数</param>
            public bool SendVMCCommand(VMCCommandType cmd, UInt16 value)
            {
                try
                {
                    byte[] data = [(byte)(value & 0xFF), (byte)(value >> 8)];
                    byte[] cmdData = new byte[1 + data.Length];
                    cmdData[0] = (byte)cmd;
                    Array.Copy(data, 0, cmdData, 1, data.Length);
                    hidMangager.SendOutputReport(ReportId.VMC_REPORT_ID, cmdData);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            /// <summary>
            /// 发送VMC命令
            /// </summary>
            /// <param name="cmd">命令</param>
            /// <param name="value">4字节的浮点型 命令对应的参数</param>
            public bool SendVMCCommand(VMCCommandType cmd, float value)
            {
                try
                {
                    // 将 float 转换为字节数组（4字节）
                    byte[] data = BitConverter.GetBytes((float)value);

                    // 创建命令数据数组（1个字节用于cmd + 4个字节用于float）
                    byte[] cmdData = new byte[1 + data.Length];

                    // 设置命令类型（cmd）
                    cmdData[0] = (byte)cmd;

                    // 复制数据（将float的字节数据复制到cmdData中的1号索引之后）
                    Array.Copy(data, 0, cmdData, 1, data.Length);

                    // 调用 SendOutputReport 方法发送数据
                    hidMangager.SendOutputReport(ReportId.VMC_REPORT_ID, cmdData);

                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error in SendVMCCommand: " + ex.Message);
                    return false;
                }
            }

            /// <summary>
            /// 发送Feature报告然后获取响应
            /// </summary>
            /// <param name="resp">响应类型</param>
            /// <param name="data">字节数组 响应所需的参数</param>
            /// <returns></returns>
            public byte[] GetResponding(VMCRespondingType resp, byte[] data)
            {
                byte[] respData = new byte[1 + data.Length];
                respData[0] = (byte)resp;
                Array.Copy(data, 0, respData, 1, data.Length);
                return hidMangager.SendAndReceiveFeatureReport(ReportId.VMC_RESPONDING_REPORT_ID, respData);
            }
            /// <summary>
            /// 发送初始化命令集合
            /// </summary>
            public void SendInitCommand()
            {
                if (hidMangager.IsDeviceOpened())
                {
                    SendVMCCommand(VMCCommandType.cmd_steering_wheel_set_rotation_range, Properties.Settings.Default.Steering_Wheel_Rotation_Range);
                    SendVMCCommand(VMCCommandType.cmd_accelerator_pedal_set_minimum, Properties.Settings.Default.Accelerator_Pedal_Minimum);
                    SendVMCCommand(VMCCommandType.cmd_accelerator_pedal_set_maximum, Properties.Settings.Default.Accelerator_Pedal_Maximum);
                    SendVMCCommand(VMCCommandType.cmd_brake_pedal_set_minimum, Properties.Settings.Default.Brake_Pedal_Minimum);
                    SendVMCCommand(VMCCommandType.cmd_brake_pedal_set_maximum, Properties.Settings.Default.Brake_Pedal_Maximum);
                    SendVMCCommand(VMCCommandType.cmd_clutch_pedal_set_minimum, Properties.Settings.Default.Clutch_Pedal_Minimum);
                    SendVMCCommand(VMCCommandType.cmd_clutch_pedal_set_maximum, Properties.Settings.Default.Clutch_Pedal_Maximum);
                    SendVMCCommand(VMCCommandType.cmd_steering_wheel_encoder_set_pulse, Properties.Settings.Default.Encoder_Pulse);
                    SendVMCCommand(VMCCommandType.cmd_effect_gain_controller_set_spring_gain, Properties.Settings.Default.Effect_Gain_Controller_Spring_Gain);
                    SendVMCCommand(VMCCommandType.cmd_effect_gain_controller_set_damper_gain, Properties.Settings.Default.Effect_Gain_Controller_Damper_Gain);
                    SendVMCCommand(VMCCommandType.cmd_effect_gain_controller_set_friction_gain, Properties.Settings.Default.Effect_Gain_Controller_Friction_Gain);
                    SendVMCCommand(VMCCommandType.cmd_effect_gain_controller_set_inertia_gain, Properties.Settings.Default.Effect_Gain_Controller_Inertia_Gain);
                    SendVMCCommand(VMCCommandType.cmd_effect_limiter_set_inertia_limiter, Properties.Settings.Default.Effect_Limiter_Inertia_Limiter);
                    SendVMCCommand(VMCCommandType.cmd_effect_set_spring_kp, Properties.Settings.Default.Effect_Spring_PID_Kp);
                    SendVMCCommand(VMCCommandType.cmd_effect_set_spring_ki, Properties.Settings.Default.Effect_Spring_PID_Ki);
                    SendVMCCommand(VMCCommandType.cmd_effect_set_spring_kd, Properties.Settings.Default.Effect_Spring_PID_Kd);
                    SendVMCCommand(VMCCommandType.cmd_steering_wheel_software_limiter_set_kp, Properties.Settings.Default.Steering_Wheel_Software_Limiter_Kp);
                    SendVMCCommand(VMCCommandType.cmd_steering_wheel_software_limiter_set_ki, Properties.Settings.Default.Steering_Wheel_Software_Limiter_Ki);
                    SendVMCCommand(VMCCommandType.cmd_steering_wheel_software_limiter_set_kd, Properties.Settings.Default.Steering_Wheel_Software_Limiter_Kd);
                    SendVMCCommand(VMCCommandType.cmd_pwm_global_parameters_set_pwm_gain_multiple, Properties.Settings.Default.PWM_Gain_Multiple_Value);
                    SendVMCCommand(
                        VMCCommandType.cmd_steering_wheel_software_limiter_set_vibration_feedback_enable,
                        Convert.ToByte(Properties.Settings.Default.Steering_Wheel_Vibration_Feedback_Enabled));
                    SendVMCCommand(
                        VMCCommandType.cmd_steering_wheel_software_limiter_set_vibration_feedback_delay,
                        Convert.ToUInt16(Properties.Settings.Default.Steering_Wheel_Vibration_Feedback_Delay));
                }
            }
        }
        public class VMCBLEGamepadDevice
        {
            private static VMCBLEGamepadDevice _instance;
            public HIDManager hidMangager;
            private VMCBLEGamepadDevice()
            {
                hidMangager = new HIDManager(Device.Vid, Device.Pid);


                ColorModeColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.Led_Color_Mode_Color_String);

                _mode = (VMCLedModeType)Properties.Settings.Default.Led_Mode;

                _isGameMode = Properties.Settings.Default.Led_Game_Mode;
            }
            /// <summary>
            /// 确保只有一个实例
            /// </summary>
            public static VMCBLEGamepadDevice Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = new VMCBLEGamepadDevice();
                    }
                    return _instance;
                }
            }
            public class Device
            {

                public const UInt16 Vid = 0x1234;
                public const UInt16 Pid = 0x0065;
                public const string FirmwareVersion = "1.0";
                public const byte LedNumber = 8;
            }
            public class ReportId
            {
                public const byte VMC_REPORT_ID = 0x03;
            }
            public bool Initialized = false;
            private bool _ledTaskRunning = false;
            private Thread _ledTaskThread;
            private VMCLedModeType _mode;
            private bool _isGameMode = false;
            private bool _endLedEffect = false;

            public enum VMCCommandType
            {
                cmd_unknown,
                cmd_set_led_show,
                cmd_set_led_color_show,
                cmd_set_led_color,
                cmd_set_led_brightness_show,
                cmd_set_led_brightness,
                cmd_set_led_clear_show,
                cmd_set_led_clear,
                cmd_set_led_fill_show,
                cmd_set_led_fill,
                cmd_set_joystick_deadzone,
                cmd_set_joystick_max_value,
            }
            public enum VMCLedModeType
            {
                color = 0,
                breath,
                rainbow,
                bounce,
                rainbow_gradient
            }
            public enum VMCLedEffectDirection
            {
                Left,
                Right
            }
            private Color ColorModeColor;
            public void SetColorModeColor(Color color)
            {
                ColorModeColor = color;
                Properties.Settings.Default.Led_Color_Mode_Color_String = color.ToString();
                Properties.Settings.Default.Save();
            }
            public Color? GetColorModeColor()
            {
                return ColorModeColor;
            }
            public void SetGameMode(bool gameMode)
            {
                _isGameMode = gameMode;
                _endLedEffect = true;
                Properties.Settings.Default.Led_Game_Mode = gameMode;
                Properties.Settings.Default.Save();
            }
            public bool GetGameMode()
            {
                return _isGameMode;
            }
            public void SetLedMode(VMCLedModeType mode)
            {
                _mode = mode;
                _endLedEffect = true;
                Properties.Settings.Default.Led_Mode = (int)mode;
                Properties.Settings.Default.Save();
            }
            /// <summary>
            /// 发送VMC命令
            /// </summary>
            /// <param name="cmd">命令</param>
            /// <param name="data">字节数组 命令对应的参数</param>
            private void SendVMCCommand(VMCCommandType cmd, byte[] data)
            {
                byte[] cmdData = new byte[1 + data.Length];
                cmdData[0] = (byte)cmd;
                Array.Copy(data, 0, cmdData, 1, data.Length);
                hidMangager.SendOutputReport(ReportId.VMC_REPORT_ID, cmdData);
            }
            /// <summary>
            /// 发送VMC命令
            /// </summary>
            /// <param name="cmd">命令</param>
            /// <param name="value">16位无符号整形 命令对应的参数</param>
            private bool SendVMCCommand(VMCCommandType cmd, UInt16 value)
            {
                try
                {
                    byte[] data = [(byte)(value & 0xFF), (byte)(value >> 8)];
                    byte[] cmdData = new byte[1 + data.Length];
                    cmdData[0] = (byte)cmd;
                    Array.Copy(data, 0, cmdData, 1, data.Length);
                    hidMangager.SendOutputReport(ReportId.VMC_REPORT_ID, cmdData);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            /// <summary>
            /// 发送VMC命令
            /// </summary>
            /// <param name="cmd">命令</param>
            /// <param name="value">4字节的浮点型 命令对应的参数</param>
            private bool SendVMCCommand(VMCCommandType cmd, float value)
            {
                try
                {
                    // 将 float 转换为字节数组（4字节）
                    byte[] data = BitConverter.GetBytes((float)value);

                    // 创建命令数据数组（1个字节用于cmd + 4个字节用于float）
                    byte[] cmdData = new byte[1 + data.Length];

                    // 设置命令类型（cmd）
                    cmdData[0] = (byte)cmd;

                    // 复制数据（将float的字节数据复制到cmdData中的1号索引之后）
                    Array.Copy(data, 0, cmdData, 1, data.Length);

                    // 调用 SendOutputReport 方法发送数据
                    hidMangager.SendOutputReport(ReportId.VMC_REPORT_ID, cmdData);

                    return true;
                }
                catch (Exception ex)
                {
                    // 记录错误信息，便于调试
                    Debug.WriteLine("Error in SendVMCCommand: " + ex.Message);
                    return false;
                }
            }
            public void SendInitCommand()
            {
                if (hidMangager.IsDeviceOpened())
                {
                    SetLedBrightness(Properties.Settings.Default.Led_Brightness);
                    SetJoystickAxesDeadzone(1, Properties.Settings.Default.Joystick_1_Deadzone);
                    SetJoystickAxesDeadzone(2, Properties.Settings.Default.Joystick_2_Deadzone);
                    SetJoystickAxesMaxValue(1, Properties.Settings.Default.Joystick_1_MaxValue);
                    SetJoystickAxesMaxValue(2, Properties.Settings.Default.Joystick_2_MaxValue);
                }
            }
            public void SetLedBrightness(byte brightness, bool show = true)
            {
                if (show)
                {
                    SendVMCCommand(VMCCommandType.cmd_set_led_brightness_show, [brightness]);
                }
                else
                {
                    SendVMCCommand(VMCCommandType.cmd_set_led_brightness, [brightness]);
                }
            }
            public void SetLedColor(byte ledIndex, Color color, bool show = true)
            {
                if (show)
                {
                    SendVMCCommand(VMCCommandType.cmd_set_led_color_show, [ledIndex, color.R, color.G, color.B]);
                }
                else
                {
                    SendVMCCommand(VMCCommandType.cmd_set_led_color, [ledIndex, color.R, color.G, color.B]);
                }
            }
            public void SetLedShow()
            {
                SendVMCCommand(VMCCommandType.cmd_set_led_show, [0]);
            }
            public void SetLedClear(bool show = true)
            {
                if (show)
                {
                    SendVMCCommand(VMCCommandType.cmd_set_led_clear_show, [0]);
                }
                else
                {
                    SendVMCCommand(VMCCommandType.cmd_set_led_clear, [0]);
                }
            }
            public void SetLedFill(byte ledIndex, byte ledNumber, Color color, bool show = true)
            {
                if (show)
                {
                    SendVMCCommand(VMCCommandType.cmd_set_led_fill_show, [ledIndex, ledNumber, color.R, color.G, color.B]);
                }
                else
                {
                    SendVMCCommand(VMCCommandType.cmd_set_led_fill, [ledIndex, ledNumber, color.R, color.G, color.B]);
                }
            }
            public void SetJoystickAxesDeadzone(byte joystickIndex, UInt16 deadzone)
            {

                SendVMCCommand(VMCCommandType.cmd_set_joystick_deadzone, [joystickIndex, (byte)(deadzone & 0xFF), (byte)(deadzone >> 8)]);

            }
            public void SetJoystickAxesMaxValue(byte joystickIndex, UInt16 maxValue)
            {

                SendVMCCommand(VMCCommandType.cmd_set_joystick_max_value, [joystickIndex, (byte)(maxValue & 0xFF), (byte)(maxValue >> 8)]);


            }
            //public void SetLedColorAll(Color[] colors)
            //{
            //    try
            //    {
            //        SendVMCCommand(VMCCommandType.cmd_set_led_color_all_show, [
            //            colors[0].R,
            //        colors[0].G,
            //        colors[0].B,
            //        colors[1].R,
            //        colors[1].G,
            //        colors[1].B,
            //        colors[2].R,
            //        colors[2].G,
            //        colors[2].B,
            //        colors[3].R,
            //        colors[3].G,
            //        colors[3].B,
            //        colors[4].R,
            //        colors[4].G,
            //        colors[4].B,
            //        colors[5].R,
            //        colors[5].G,
            //        colors[5].B,
            //        colors[6].R,
            //        colors[6].G,
            //        colors[6].B,
            //        colors[7].R,
            //        colors[7].G,
            //        colors[7].B,
            //        ]);
            //    }catch (Exception e)
            //    {
            //        Debug.WriteLine(e);
            //    }
            //}
            public void StartLedTask()
            {
                if (_ledTaskRunning)
                {
                    Debug.WriteLine("LedTask已经创建，不能重复创建");
                    return;
                }
                // 创建一个新的线程来读取输入
                _ledTaskThread = new Thread(LedTask)
                {
                    IsBackground = true // 设置为后台线程，当主线程退出时它将自动退出
                };
                _ledTaskRunning = true;
                _ledTaskThread.Start();

            }
            private void LedTask()
            {
                while (_ledTaskRunning)
                {
                    if (_isGameMode)    // 此处添加如果游戏已经打开
                    {
                        Debug.WriteLine("LED游戏模式已打开");
                        if (ETS2Telemetry.IsSdkActived())
                        {
                            Debug.WriteLine("ETS2 SDK已打开");
                            bool currentLight = false;
                            bool currentElectricEnable = false;
                            while (_isGameMode) //如果关闭了游戏模式，就停止
                            {
                                //Debug.WriteLine("检查游戏状态");
                                if (ETS2Telemetry.IsPaused())
                                {
                                    // 显示暂停时的灯光
                                    ShowBounceEffect();
                                }
                                else
                                {
                                    //SetLedClear();
                                    if ((currentElectricEnable != ETS2Telemetry.IsElectricEnabled()) && ETS2Telemetry.IsElectricEnabled())
                                    {
                                        currentElectricEnable = ETS2Telemetry.IsElectricEnabled();
                                        for (uint i = 0; i < 10; i++)
                                        {
                                            ShowProgressBarEffect(i, 10);
                                        }
                                        for (uint i = 10; i > 0; i--)
                                        {
                                            ShowProgressBarEffect(i, 10);
                                        }
                                    }
                                    else if ((currentElectricEnable != ETS2Telemetry.IsElectricEnabled()) && !ETS2Telemetry.IsElectricEnabled())
                                    {
                                        currentElectricEnable = ETS2Telemetry.IsElectricEnabled();
                                    }
                                    if (currentLight != ETS2Telemetry.IsLightsParking())
                                    {
                                        currentLight = ETS2Telemetry.IsLightsParking();
                                        if (ETS2Telemetry.IsLightsParking())
                                        {
                                            SetLedBrightness((byte)(Properties.Settings.Default.Led_Brightness > (255 - 20) ? 255 : Properties.Settings.Default.Led_Brightness + 20));
                                        }
                                        else
                                        {
                                            SetLedBrightness(Properties.Settings.Default.Led_Brightness);
                                        }
                                    }
                                    if (ETS2Telemetry.IsBlinkerLeftActived() && ETS2Telemetry.IsElectricEnabled())
                                    {
                                        ShowFlowingEffect(VMCLedEffectDirection.Left, Color.FromRgb(255, 60, 0));
                                    }
                                    else if (ETS2Telemetry.IsBlinkerRightActived() && ETS2Telemetry.IsElectricEnabled())
                                    {
                                        ShowFlowingEffect(VMCLedEffectDirection.Right, Color.FromRgb(255, 60, 0));
                                    }
                                    else
                                    {
                                        ShowProgressBarEffect(Convert.ToUInt32(ETS2Telemetry.GetEngineRpm()), Convert.ToUInt32(ETS2Telemetry.GetEngineRpmMax()));
                                        //Thread.Sleep(100);
                                    }


                                }

                                Thread.Sleep(50);
                            }

                        }
                        else
                        {
                            Debug.WriteLine("没有检测到任何游戏进程，进入用户设定的模式");
                            SwitchMode();
                        }
                    }
                    else
                    {
                        SwitchMode();
                    }
                    Thread.Sleep(50);
                }
            }
            private void SwitchMode()
            {
                switch (_mode)
                {
                    case VMCLedModeType.color:
                        SetLedFill(0, Device.LedNumber, ColorModeColor);
                        break;
                    case VMCLedModeType.breath:
                        StartBreathMode(ColorModeColor);
                        break;
                    case VMCLedModeType.rainbow:
                        StartRainbowMode();
                        break;
                    case VMCLedModeType.bounce:
                        StartBounceMode();
                        break;
                    case VMCLedModeType.rainbow_gradient:
                        StartRainbowGradientMode();
                        break;
                }
            }
            // 以下为开始的模式，会无限循环，仅在切换模式时被打断
            private void StartRainbowMode()
            {
                // 获取 LED 数量
                int ledCount = Device.LedNumber;
                int rainbowLength = 7;  // 彩虹颜色数量（你可以根据需要调整）

                // 彩虹颜色数组
                Color[] rainbowColors = new Color[]
                {
        Color.FromRgb(255, 0, 0),      // 红色
        Color.FromRgb(255, 127, 0),    // 橙色
        Color.FromRgb(255, 255, 0),    // 黄色
        Color.FromRgb(0, 255, 0),      // 绿色
        Color.FromRgb(0, 0, 255),      // 蓝色
        Color.FromRgb(75, 0, 130),     // 靛蓝色
        Color.FromRgb(148, 0, 211)     // 紫色
                };

                // 控制滚动速度
                int stepDelay = 50; // 每次滚动的延时

                // 用于存储每个 LED 当前的颜色
                Color[] ledColors = new Color[ledCount];

                // 初始化 LED 灯带的颜色
                for (int i = 0; i < ledCount; i++)
                {
                    ledColors[i] = rainbowColors[i % rainbowLength];
                    SetLedColor((byte)i, ledColors[i], false);  // 设置初始颜色
                }

                // 手动应用初始颜色
                SetLedShow();

                // 启动彩虹滚动效果
                while (!_endLedEffect)
                {
                    // 先将最后一个 LED 的颜色设置为彩虹的第一个颜色（进行滚动）
                    Color firstColor = ledColors[0];
                    for (int i = 0; i < ledCount - 1; i++)
                    {
                        ledColors[i] = ledColors[i + 1];  // 向左移动颜色
                    }
                    ledColors[ledCount - 1] = rainbowColors[(Array.IndexOf(rainbowColors, ledColors[ledCount - 2]) + 1) % rainbowLength];  // 计算最后一个 LED 的颜色

                    // 更新每个 LED 的颜色
                    for (int i = 0; i < ledCount; i++)
                    {
                        SetLedColor((byte)i, ledColors[i], false);
                    }

                    // 应用当前颜色到 LED
                    SetLedShow();

                    // 控制滚动速度
                    Thread.Sleep(stepDelay);  // 延时控制滚动速度
                }
                _endLedEffect = false;
            }
            private void StartBounceMode()
            {
                int ledCount = Device.LedNumber;
                if (ledCount < 3) return;

                int progressBarLength = 3;  // 进度条占用 3 个 LED 灯
                int stepDelay = 150;         // 延时，控制滚动速度

                int currentPosition = 0;
                bool movingRight = true;

                while (!_endLedEffect)
                {
                    // 清空所有 LED 灯的颜色
                    SetLedClear(false);

                    // 使用 SetLedFill 设置连续 3 个 LED
                    SetLedFill((byte)currentPosition, (byte)progressBarLength, ColorModeColor, false);

                    // 应用颜色
                    SetLedShow();

                    // 进度条方向控制
                    if (movingRight)
                    {
                        if (currentPosition < ledCount - progressBarLength)
                            currentPosition++;
                        else
                        {
                            movingRight = false;
                            currentPosition--;
                        }
                    }
                    else
                    {
                        if (currentPosition > 0)
                            currentPosition--;
                        else
                        {
                            movingRight = true;
                            currentPosition++;
                        }
                    }

                    // 控制滚动速度
                    Thread.Sleep(stepDelay);
                }
                _endLedEffect = false;
            }
            private void StartBreathMode(Color baseColor)
            {
                byte brightness = 0;  // 初始亮度
                byte fadeAmount = 5;   // 每次亮度变化的增量
                int delayTime = 20;   // 每步延时，控制呼吸速度

                // 呼吸效果：逐渐增加亮度
                while (brightness < 255)
                {
                    if (_endLedEffect)
                    {
                        break;
                    }
                    // 调整颜色的亮度
                    Color currentColor = AdjustBrightness(baseColor, brightness);

                    // 更新LED的颜色（假设 SetLedFill 用于控制灯光）
                    SetLedFill(0, Device.LedNumber, currentColor);

                    // 增加亮度
                    brightness += fadeAmount;

                    // 控制渐变速度
                    Thread.Sleep(delayTime);  // 延时控制呼吸效果的速度
                }

                // 呼吸效果：逐渐减小亮度
                while (brightness > 0)
                {
                    if (_endLedEffect)
                    {
                        break;
                    }
                    // 调整颜色的亮度
                    Color currentColor = AdjustBrightness(baseColor, brightness);

                    // 更新LED的颜色
                    SetLedFill(0, Device.LedNumber, currentColor);

                    // 减少亮度
                    brightness -= fadeAmount;

                    // 控制渐变速度
                    Thread.Sleep(delayTime);  // 延时控制呼吸效果的速度
                }
                _endLedEffect = false;
            }

            private void StartRainbowGradientMode()
            {
                int ledCount = Device.LedNumber;
                if (ledCount < 1) return;

                // 定义渐变色：从红色到紫色的彩虹渐变
                Color[] gradientColors = new Color[]
                {
                    Colors.Red,        // 红色
                    Color.FromRgb(255, 127, 0),   // 橙色
                    Colors.Yellow,     // 黄色
                    Colors.Green,      // 绿色
                    Colors.Blue,       // 蓝色
                    Color.FromRgb(75, 0, 130),    // 靛色
                    Colors.Purple      // 紫色
                };

                // 渐变过渡步数
                int transitionSteps = 100;  // 每个渐变颜色过渡的步数
                List<Color> gradientSteps = new List<Color>();

                // 生成渐变色
                for (int i = 0; i < gradientColors.Length - 1; i++)
                {
                    Color startColor = gradientColors[i];
                    Color endColor = gradientColors[i + 1];

                    // 在当前颜色和下一个颜色之间进行插值
                    for (int j = 0; j < transitionSteps; j++)
                    {
                        double t = (double)j / (transitionSteps - 1); // 插值比例
                        byte red = (byte)(startColor.R + (endColor.R - startColor.R) * t);
                        byte green = (byte)(startColor.G + (endColor.G - startColor.G) * t);
                        byte blue = (byte)(startColor.B + (endColor.B - startColor.B) * t);

                        gradientSteps.Add(Color.FromRgb(red, green, blue));
                    }
                }

                // 最后加入最后的颜色
                gradientSteps.Add(gradientColors[gradientColors.Length - 1]);

                // 创建反向渐变（从紫色回到红色）
                List<Color> reverseGradientSteps = new List<Color>(gradientSteps);
                reverseGradientSteps.Reverse();

                // 合并正向和反向渐变（一个完整的循环）
                gradientSteps.AddRange(reverseGradientSteps);

                // 无限循环：通过一个 while 循环实现渐变效果
                int totalSteps = gradientSteps.Count;
                int currentStep = 0;

                while (!_endLedEffect)
                {
                    // 获取当前的渐变颜色
                    Color currentColor = gradientSteps[currentStep];

                    // 设置所有 LED 为当前的渐变颜色
                    for (int i = 0; i < ledCount; i++)
                    {
                        SetLedFill((byte)i, 1, currentColor, false);
                    }

                    // 应用颜色
                    SetLedShow();

                    // 计算下一个渐变步骤
                    currentStep = (currentStep + 1) % totalSteps;

                    // 延时，控制渐变的速度
                    Task.Delay(1).Wait();  // 控制每步渐变的速度，可以调整这个延时值
                }
                _endLedEffect = false;
            }

            // 以下为显示的效果，仅会播放一次
            /// <summary>
            /// 显示呼吸效果
            /// </summary>
            /// <param name="baseColor"></param>
            private void ShowBreathEffect(Color baseColor)
            {
                byte brightness = 0;  // 初始亮度
                byte fadeAmount = 5;   // 每次亮度变化的增量
                int delayTime = 20;   // 每步延时，控制呼吸速度

                // 呼吸效果：逐渐增加亮度
                while (brightness < 255)
                {
                    // 调整颜色的亮度
                    Color currentColor = AdjustBrightness(baseColor, brightness);

                    // 更新LED的颜色（假设 SetLedFill 用于控制灯光）
                    SetLedFill(0, Device.LedNumber, currentColor);

                    // 增加亮度
                    brightness += fadeAmount;

                    // 控制渐变速度
                    Thread.Sleep(delayTime);  // 延时控制呼吸效果的速度
                }

                // 呼吸效果：逐渐减小亮度
                while (brightness > 0)
                {
                    // 调整颜色的亮度
                    Color currentColor = AdjustBrightness(baseColor, brightness);

                    // 更新LED的颜色
                    SetLedFill(0, Device.LedNumber, currentColor);

                    // 减少亮度
                    brightness -= fadeAmount;

                    // 控制渐变速度
                    Thread.Sleep(delayTime);  // 延时控制呼吸效果的速度
                }
            }
            private void ShowProgressBarEffect(UInt32 currentValue, UInt32 maxValue)
            {
                int ledCount = Device.LedNumber;
                if (ledCount < 1) return;

                // 颜色数组（对应从左到右的颜色）
                byte[,] colors = new byte[8, 3]
                {
            { 0, 255, 0 },    // 绿色
            { 120, 255, 0 },  // 亮黄绿色
            { 200, 255, 0 },  // 黄色
            { 255, 255, 0 },  // 黄色
            { 255, 160, 0 },  // 橙色
            { 255, 80, 0 },   // 橙红色
            { 255, 40, 0 },   // 红色
            { 255, 0, 0 }     // 红色
                };

                // 计算比例，得到要点亮的 LED 数量
                double ratio = (double)currentValue / maxValue;
                int litLedCount = (int)Math.Round(ratio * ledCount);  // 四舍五入计算点亮的 LED 数量

                // 清空所有 LED 灯的颜色
                //SetLedClear(false);
                // 根据点亮的 LED 数量，设置 LED 颜色
                for (int i = 0; i < litLedCount && i < ledCount; i++)
                {
                    // 设置 LED 的颜色
                    byte red = colors[i % 8, 0];
                    byte green = colors[i % 8, 1];
                    byte blue = colors[i % 8, 2];
                    Thread.Sleep(1);
                    SetLedColor((byte)i, Color.FromRgb(red, green, blue), false);
                }

                // 确保未点亮的 LED 使用默认颜色（关闭）
                for (int i = litLedCount; i < ledCount; i++)
                {
                    SetLedColor((byte)i, Colors.Black, false);  // 关闭未点亮的 LED
                }
                // 应用设置
                SetLedShow();

            }
            private void ShowFlowingEffect(VMCLedEffectDirection direction, Color color)
            {
                int ledCount = Device.LedNumber;
                if (ledCount < 1) return;

                // 延时控制，控制灯光流动速度
                int stepDelay = 100;  // 调整延时以控制流动速度


                // 清空所有 LED 灯的颜色
                SetLedClear(false);

                // 流动效果
                if (direction == VMCLedEffectDirection.Left)
                {
                    // 从右到左流动
                    for (int i = 0; i < ledCount; i++)
                    {
                        // 设置当前 LED 灯的颜色
                        SetLedColor((byte)(ledCount - 1 - i), color, false); // 从右边开始点亮
                        SetLedShow();
                        Thread.Sleep(stepDelay);
                    }
                }
                else if (direction == VMCLedEffectDirection.Right)
                {
                    // 从左到右流动
                    for (int i = 0; i < ledCount; i++)
                    {
                        // 设置当前 LED 灯的颜色
                        SetLedColor((byte)i, color, false); // 从左边开始点亮
                        SetLedShow();
                        Thread.Sleep(stepDelay);
                    }
                }
                Thread.Sleep(500);
                // 灯光全亮后，熄灭所有 LED 灯
                SetLedClear(true);

                // 等待一下后开始下一次循环
                Thread.Sleep(500); // 可以根据需要调整熄灭后的等待时间

            }
            private void ShowBounceEffect()
            {
                int ledCount = Device.LedNumber;
                if (ledCount < 3) return;

                int progressBarLength = 3;  // 进度条占用 3 个 LED 灯
                int stepDelay = 150;         // 延时，控制滚动速度

                int currentPosition = 0;
                bool movingRight = true;

                // 控制是否已经完成来回滚动
                bool hasBounced = false;

                // 执行一次完整的往返
                while (!hasBounced)
                {
                    // 清空所有 LED 灯的颜色
                    SetLedClear(false);

                    // 使用 SetLedFill 设置连续 3 个 LED
                    SetLedFill((byte)currentPosition, (byte)progressBarLength, ColorModeColor, false);

                    // 应用颜色
                    SetLedShow();

                    // 进度条方向控制
                    if (movingRight)
                    {
                        // 从左到右滚动
                        if (currentPosition < ledCount - progressBarLength)
                            currentPosition++;
                        else
                        {
                            // 到达右边，切换为向左滚动
                            movingRight = false;
                            currentPosition--;
                        }
                    }
                    else
                    {
                        // 从右到左滚动
                        if (currentPosition > 0)
                            currentPosition--;
                        else
                        {
                            // 到达左边，切换为向右滚动
                            movingRight = true;
                            currentPosition++;
                        }
                    }

                    // 控制滚动速度
                    Thread.Sleep(stepDelay);

                    // 判断是否完成一次完整的来回滚动
                    if (currentPosition == 0 && !movingRight)
                    {
                        hasBounced = true;  // 完成一个完整的往返
                    }
                }
            }






            // 根据亮度进度调整RGB颜色的亮度
            private Color AdjustBrightness(Color color, byte brightness)
            {
                byte r = (byte)(color.R * brightness / 255);  // 调整R通道
                byte g = (byte)(color.G * brightness / 255);  // 调整G通道
                byte b = (byte)(color.B * brightness / 255);  // 调整B通道

                return Color.FromRgb(r, g, b);
            }
            public void StopLedTask()
            {
                _endLedEffect = true;
                _ledTaskRunning = false;
                _ledTaskThread?.Join();
            }
        }
    }
}
