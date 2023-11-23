using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace JTDevice
{
    // 设备连接管理器
    public class DeviceConnector : IDisposable
    {
        private TcpClient tcpClient;
        private string deviceIP;
        private int devicePort;
        private NetworkStream networkStream;
        public DateTime ReceiveDeviceDataTime;

        public bool IsConnectedToDevice { get; private set; } = false;
        // 定义一个事件，用于当数据接收时通知订阅者
        //public event EventHandler<byte[]> DataReceived;
        public event EventHandler<string> DataReceived;

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
            IsConnectedToDevice = false;
            if (tcpClient != null)
            {
                tcpClient.Close();
                //tcpClient = null;
                //if (tcpClient != null)
                //{
                //    tcpClient.Close();
                //}
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
                IsConnectedToDevice = true;
                networkStream = tcpClient.GetStream();
                networkStream.BeginRead(TempBytes, 0, TempBytes.Length, AsynReceiveData, TempBytes);
            }
            catch (Exception ex)
            {
                Logger.logger.Error(ex, "Connection failed.");
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
                        int bytesRead = networkStream.EndRead(iar);
                        ReceiveDeviceDataTime = DateTime.Now;

                        string receivedData = Encoding.ASCII.GetString(CurrentBytes, 0, bytesRead);

                        // 触发事件
                        DataReceived?.Invoke(this, receivedData);
                        networkStream.BeginRead(CurrentBytes, 0, CurrentBytes.Length, AsynReceiveData, CurrentBytes);

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
        public void SendData(byte[] SendBytes)
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
                Console.WriteLine(ex.Message + "SendData");
                IsConnectedToDevice = false;
            }
        }

        public void SendData(string message)
        {
            if (tcpClient != null && networkStream != null)
            {
                byte[] data = Encoding.ASCII.GetBytes(message);
                networkStream.Write(data, 0, data.Length);
            }
        }

        public void Dispose()
        {
            if (tcpClient != null)
            {
                tcpClient.Dispose();
            }
            if (networkStream != null)
            {
                networkStream.Dispose();
            }
        }
    }
}
