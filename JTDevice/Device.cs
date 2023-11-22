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
    public class Logger
    {
        public static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }

    // 设备连接管理器
    public class DeviceConnector
    {
        private TcpClient tcpClient;
        private string deviceIP;
        private int devicePort;
        private NetworkStream networkStream;
        public DateTime ReceiveDeviceDataTime;

        public bool IsConnectedToDevice { get; private set; } = false;

        public DeviceConnector(string ip, int port)
        {
            deviceIP = ip;
            devicePort = port;
        }

        public void Connect()
        {
            tcpClient = new TcpClient();
            IPAddress ipaddress = IPAddress.Parse(deviceIP);
            tcpClient.BeginConnect(ipaddress, devicePort, new AsyncCallback(AsynConnect), tcpClient);
        }

        public void Disconnect()
        {
            if (tcpClient != null)
            {
                tcpClient.Close();
                //tcpClient = null;
                if (tcpClient != null)
                {
                    tcpClient.Close();
                }
            }
        }

        byte[] TempBytes = new byte[4096];
        public void AsynConnect(IAsyncResult iar)
        {
            try
            {
                //连接成功
                tcpClient.EndConnect(iar);
                //连接成功标志


                Logger.logger.Info(string.Format("Try to connect：{0}，Successful", deviceIP));

                IsConnectedToDevice = true;
                networkStream = tcpClient.GetStream();
                //开始异步读取返回数据
                networkStream.BeginRead(TempBytes, 0, TempBytes.Length, new AsyncCallback(AsynReceiveData), TempBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 异步接受数据
        /// </summary>
        /// <param name="iar"></param>
        byte[] NewBytes = new byte[4096];
        public void AsynReceiveData(IAsyncResult iar)
        {
            byte[] CurrentBytes = (byte[])iar.AsyncState;
            try
            {
                if (IsConnectedToDevice)
                {
                    if (tcpClient.Connected)
                    {
                        //结束了本次数据接收
                        int num = networkStream.EndRead(iar);
                        ReceiveDeviceDataTime = DateTime.Now;
                        //Console.WriteLine($"{DateTime.Now}:Rceicetime:{ReceivePlcDataTime}");
                        networkStream.BeginRead(NewBytes, 0, NewBytes.Length, new AsyncCallback(AsynReceiveData), NewBytes);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "AsynReceiveData");
                IsConnectedToDevice = false;
            }
        }


        /// <summary>
        /// 发送数据
        /// <param name="SendBytes">需要发送的数据</param>
        /// </summary>
        public void SendDataToDevice(byte[] SendBytes)
        {
            try
            {
                if (networkStream.CanWrite && SendBytes != null && SendBytes.Length > 0)
                {
                    //发送数据
                    networkStream.Write(SendBytes, 0, SendBytes.Length);
                    networkStream.Flush();
                    //Console.WriteLine($"{DateTime.Now}:SendBytes:{SendBytes[0]}");
                }
            }
            catch (Exception ex)
            {
                if (tcpClient != null)
                {
                    tcpClient.Close();
                    //关闭连接后马上更新连接状态标志
                }
                Console.WriteLine(ex.Message + "SendDataToDevice");
                IsConnectedToDevice = false;
            }
        }

    }

    public class Device : IDisposable
    {
        public string DeviceIP { get; set; }
        public int DevicePort { get; set; } = 5677;

        private int deviceID;
        private DeviceConnector connector;
        
        private bool Heartbeat = false;
        private bool ShowConnect_flag = false;

        private int tryconnect_count = 0;

        public enum_ConnectStatus Connect_status;

        private int plctask_count = 0;
        private bool boolReconnectTimerEnable = false;   //5000ms
        private bool boolDevice_TimerEnable = false;   //1000ms

        public Device(int id, string ip, int port)
        {
            deviceID = id;
            connector = new DeviceConnector(ip, port);
        }

        public void task()
        {
            ThreadStart threadStart = new ThreadStart(Connect);//通过ThreadStart委托告诉子线程执行什么方法　　
            Thread thread = new Thread(threadStart);
            thread.Start();                                      //启动新线程                                              //thread.Abort();                                    //终止改线程，比较粗暴
        }

        public void Connect()
        {
            if (!connector.IsConnectedToDevice)
            {
                ConnectToDevice();
                ShowConnect_flag = true;
            }
        }

        public void CloseByButton()
        {
            boolDevice_TimerEnable = false;
            boolReconnectTimerEnable = false;
            Disconnect();
        }

        public void ConnectToDevice()
        {
            try
            {
                boolDevice_TimerEnable = true;//是否执行RemoteIO连接状态事件；
                boolReconnectTimerEnable = true;//是否执行Reconnect事件；

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
            Connect_status = enum_ConnectStatus.Disconnected;
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
            if ((time > 3) && (ShowConnect_flag == true))
            {
                Heartbeat = false;
                Connect_status = enum_ConnectStatus.Connecting;
            }
            else
            {
                Heartbeat = true;
                if (connector.IsConnectedToDevice)
                {
                    Connect_status = enum_ConnectStatus.Connected;
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
                    if (boolDevice_TimerEnable)
                    {
                        Device_TimeOut();
                    }
                }

                if (plctask_count % 5 == 0)  //每5秒钟需要做的事情,重连设备
                {
                    //Console.WriteLine(Thread.CurrentThread.Name);
                    if (boolReconnectTimerEnable)
                    {
                        ReconnectTimeOut();
                    }
                }
            }
        }

        public void Dispose()
        {
            ;
        }
    }
}




