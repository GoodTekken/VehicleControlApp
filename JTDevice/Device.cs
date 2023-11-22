using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JTDevice
{
    public class Logger
    {
        public static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }

    internal class Device : IDisposable
    {
        private int deviceID;
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private string remote_IP;
        private int RemotePort = 5677;  //车载控制器的默认端口：5677

        private bool IsConnectedToPLC = false;
        public string RemoteIp_choose;

        public bool Is_ConnectedToPLC;  //对外接口

        private bool Heartbeat = false;
        private bool ShowConnect_flag = false;

        private int tryconnect_count = 0;

        DateTime ReceivePlcDataTime;

        public string Remote_IP { get { return remote_IP; } set { remote_IP = value; } }
        public enum_ConnectStatus Connect_status;

        public void task()
        {
            ThreadStart threadStart = new ThreadStart(Connect);//通过ThreadStart委托告诉子线程执行什么方法　　
            Thread thread = new Thread(threadStart);
            thread.Start();                                      //启动新线程
                                                                 //thread.Abort();                                    //终止改线程，比较粗暴
        }

        public void Connect()
        {
            if (!IsConnectedToPLC)
            {
                ConnectToPLC();
                ShowConnect_flag = true;
            }
        }


        public void CloseByButton()
        {
            boolRemoteIO_TimerEnable = false;
            boolReconnectTimerEnable = false;
            DisconnectPLC();
        }

        public void ConnectToPLC()
        {
            try
            {
                tcpClient = new TcpClient();
                IPAddress ipaddress = IPAddress.Parse(remote_IP);
                tcpClient.BeginConnect(ipaddress, RemotePort, new AsyncCallback(AsynConnect), tcpClient);

                boolRemoteIO_TimerEnable = true;//是否执行RemoteIO连接状态事件；
                boolReconnectTimerEnable = true;//是否执行Reconnect事件；

                tryconnect_count++;
                if (tryconnect_count == int.MaxValue) { tryconnect_count = 0; }
                Console.WriteLine($"{DateTime.Now}:Device{this.deviceID} Try to connect：{remote_IP}，Count：{tryconnect_count}");
                Logger.logger.Info(string.Format($"Device{this.deviceID}  Try to connect：{remote_IP}，Count：{tryconnect_count}"));
                if (tryconnect_count > 99999)
                {
                    tryconnect_count = 0;

                }

            }
            catch (Exception ex)
            {
                ShowMessageInConsoleDebug(ex.Message + "请检查远程IO服务器连接");
                Console.WriteLine(ex.Message);
            }
        }

        public void DisconnectPLC()
        {
            if (tcpClient != null)
            {
                tcpClient.Close();
                //关闭连接后马上更新连接状态标志
                if (tcpClient != null)
                {
                    tcpClient.Close();
                    //关闭连接后马上更新连接状态标志
                }
            }
            IsConnectedToPLC = false;
            Is_ConnectedToPLC = IsConnectedToPLC;
            Connect_status = enum_ConnectStatus.Disconnected;
        }

        public void ReconnectPLC()
        {
            if (tcpClient != null)
            {
                tcpClient.Close();
                //关闭连接后马上更新连接状态标志
                if (tcpClient != null)
                {
                    tcpClient.Close();
                    //关闭连接后马上更新连接状态标志
                }
            }
            ConnectToPLC();
        }

        byte[] TempBytes = new byte[4096];
        public void AsynConnect(IAsyncResult iar)
        {
            try
            {
                //连接成功
                tcpClient.EndConnect(iar);
                //连接成功标志

                tryconnect_count = 0;

                Logger.logger.Info(string.Format("Try to connect：{0}，Successful", remote_IP));

                IsConnectedToPLC = true;
                networkStream = tcpClient.GetStream();
                //开始异步读取返回数据
                networkStream.BeginRead(TempBytes, 0, TempBytes.Length, new AsyncCallback(AsynReceiveData), TempBytes);
            }
            catch (Exception ex)
            {
                ShowMessageInConsoleDebug(ex.Message + "请检查远程IO服务器连接");
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 发送数据
        /// <param name="SendBytes">需要发送的数据</param>
        /// </summary>
        public void SendDataToPLC(byte[] SendBytes)
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
                ShowMessageInConsoleDebug(ex.Message);
                Console.WriteLine(ex.Message + "SendDataToPLC");
                IsConnectedToPLC = false;
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
                if (IsConnectedToPLC)
                {
                    if (tcpClient.Connected)
                    {
                        //结束了本次数据接收
                        int num = networkStream.EndRead(iar);
                        //这里展示结果为InfoModel的CurrBytes属性，将返回的数据添加至返回数据容器中
                        ReceiveData(CurrentBytes);
                        ReceivePlcDataTime = DateTime.Now;
                        //Console.WriteLine($"{DateTime.Now}:Rceicetime:{ReceivePlcDataTime}");
                        networkStream.BeginRead(NewBytes, 0, NewBytes.Length, new AsyncCallback(AsynReceiveData), NewBytes);
                    }
                }

            }
            catch (Exception ex)
            {
                ShowMessageInConsoleDebug(ex.Message);
                Console.WriteLine(ex.Message + "AsynReceiveData");
                IsConnectedToPLC = false;
            }
        }

        public void RemoteIO_TimeOut()
        {
            RemoteIO_TimeOut_event();
        }

        public void ReconnectTimeOut()
        {
            if (!IsConnectedToPLC)
            {
                ReconnectPLC();
            }
        }

        public void Button_connect_Show()
        {
            DateTime startTime = DateTime.Now;
            long time = (long)(startTime - ReceivePlcDataTime).TotalSeconds;
            Is_ConnectedToPLC = IsConnectedToPLC;

            //Console.WriteLine($"{DateTime.Now}:time:{time}");
            if ((time > 3) && (ShowConnect_flag == true))
            {
                Heartbeat = false;
                Connect_status = enum_ConnectStatus.Connecting;
            }
            else
            {
                Heartbeat = true;
                if (IsConnectedToPLC)
                {
                    Connect_status = enum_ConnectStatus.Connected;
                }
            }
        }

        public void RemoteIO_TimeOut_event()  //写入DO
        {
            Button_connect_Show();
        }

        public Device(int deviceID)
        {
            this.deviceID = deviceID;
            PLCTaskStart(deviceID);
            Console.WriteLine($"{DateTime.Now}:设备{this.deviceID}开始连接");
        }

        private void PLCTaskStart(int id)
        {
            Thread t;
            t = new Thread(PLCTask);
            t.IsBackground = true;
            t.Name = $"PLCTask:" + id.ToString();
            t.Start();
        }

        private int plctask_count = 0;
        private bool boolReconnectTimerEnable = false;   //5000ms
        private bool boolRemoteIO_TimerEnable = false;   //1000ms

        private void PLCTask()
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
                    if (boolRemoteIO_TimerEnable)
                    {
                        RemoteIO_TimeOut();
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
            Close();
        }
        public void Close()
        {
            try
            {
                ;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void ShowMessageInConsoleDebug(string str)
        {
#if Debug
		MessageBox.Show(str);
#endif
        }

        public void ReceiveData(byte[] ReceiveBytes)
        {
            ;
        }
    }
}




