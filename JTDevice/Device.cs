using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;


namespace JTDevice
{
    public class Device : IDisposable
    {
        private DeviceConnector connector;
        public string? DeviceIP { get; set; }
        private int deviceID;
        private bool isHeartbeatActive = false;
        private bool showConnectFlag = false;
        private int tryconnect_count = 0;
        public ConnectStatus Connect_status;
        private int plctask_count = 0;
        private bool isReconnectTimerEnabled = false;   //5000ms
        private bool isDeviceTimerEnabled = false;   //1000ms

        // 定义一个事件，用于通知上层程序
        public event EventHandler<string> DataProcessed;

        public Device(int id, string ip, int port)
        {
            deviceID = id;
            DeviceIP = ip;
            connector = new DeviceConnector(ip, port);
            // 订阅事件
            connector.DataReceived += OnDataReceived;
        }

        private void OnDataReceived(object sender, string data)
        {
            // 处理接收到的数据
            // 可以在这里触发另一个事件，以通知上层程序
            ProcessReceivedData(data);
        }

        private void ProcessReceivedData(string data)
        {
            // 触发事件
            DataProcessed?.Invoke(this, data);
        }


        public void task()
        {
            Task.Run(() => Connect());
        }

        public void Connect()
        {
            if (!connector.IsConnectedToDevice)
            {
                ConnectToDevice();
                Connect_status = ConnectStatus.Connected;
                showConnectFlag = true;
            }
        }

        public void CloseByButton()
        {
            isDeviceTimerEnabled = false;
            isReconnectTimerEnabled = false;
            Disconnect();
        }

        public void ConnectToDevice()
        {
            try
            {
                connector.Connect();

                isDeviceTimerEnabled = true;
                isReconnectTimerEnabled = true;

                tryconnect_count++;
                if (tryconnect_count == int.MaxValue) { tryconnect_count = 0; }
                Console.WriteLine($"{DateTime.Now}:Device{this.deviceID} Try to connect：{DeviceIP}，Count：{tryconnect_count}");
                Logger.logger.Info(string.Format($"Device{this.deviceID}  Try to connect：{DeviceIP}，Count：{tryconnect_count}"));
                if (tryconnect_count > 99999)
                {
                    tryconnect_count = 0;
                }
            }
            catch (SocketException socketEx)
            {
                // 处理网络相关的异常
                Logger.logger.Error(socketEx, "Network error occurred while connecting to the device.");
                // 可能的错误恢复逻辑，比如重试
            }
            catch (IOException ioEx)
            {
                // 处理I/O异常
                Logger.logger.Error(ioEx, "I/O error occurred while connecting to the device.");
            }
            catch (Exception ex)
            {
                // 处理其他类型的异常
                Logger.logger.Error(ex, "An unexpected error occurred while connecting to the device.");
                throw; // 如果无法处理，则重新抛出异常
            }
        }

        public void Disconnect()
        {
            connector.Disconnect(); ;
            Connect_status = ConnectStatus.Disconnected;
        }

        public void Reconnect()
        {
            connector.Disconnect();
            connector.Connect();
        }

        public void Device_TimeOut()
        {
            Device_TimeOut_event();
        }

        public void ReconnectTimeOut()
        {
            if (!connector.IsConnectedToDevice)
            {
                Reconnect();
            }
        }

        public void Button_connect_Show()
        {
            DateTime startTime = DateTime.Now;
            long time = (long)(startTime - connector.ReceiveDeviceDataTime).TotalSeconds;

            //Console.WriteLine($"{DateTime.Now}:time:{time}");
            if ((time > 3) && (showConnectFlag == true))
            {
                isHeartbeatActive = false;
                Connect_status = ConnectStatus.Connecting;
            }
            else
            {
                isHeartbeatActive = true;
                if (connector.IsConnectedToDevice)
                {
                    Connect_status = ConnectStatus.Connected;
                }
            }
        }

        public void Device_TimeOut_event()  //写入DO
        {
            Button_connect_Show();
        }

        private void DeviceTaskStart(int id)
        {
            Thread t;
            t = new Thread(DeviceTask);
            t.IsBackground = true;
            t.Name = $"PLCTask:" + id.ToString();
            t.Start();
        }

        public void Send(string message)
        {
            connector.SendData(message);
        }

        private void DeviceTask()
        {
            while (true)
            {
                Thread.Sleep(1000);
                plctask_count++;
                if (plctask_count > (int.MaxValue - 1000))
                {
                    plctask_count = 0;
                }

                if (plctask_count % 1 == 0)  //每一秒钟需要做的事情
                {
                    if (isDeviceTimerEnabled)
                    {
                        Device_TimeOut();
                    }
                }

                if (plctask_count % 5 == 0)  //每5秒钟需要做的事情,重连设备
                {
                    //Console.WriteLine(Thread.CurrentThread.Name);
                    if (isReconnectTimerEnabled)
                    {
                        ReconnectTimeOut();
                    }
                }
            }
        }

        public void Dispose()
        {
            connector?.Dispose();
        }
    }
}




