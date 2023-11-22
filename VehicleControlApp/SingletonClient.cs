using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VehicleControlApp
{
    using System;
    using System.Net.Sockets;
    using System.Text;

    public class SingletonClient
    {
        private static SingletonClient _instance;
        private TcpClient client;
        private NetworkStream stream;

        // 定义一个事件，用于通知数据接收
        public event EventHandler<string> DataReceived;
        // 假设消息以换行符结束  
        private string delimiter = "\n"; 

        public static SingletonClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SingletonClient();
                }
                return _instance;
            }
        }

        private SingletonClient() { }

        public void Connect(string serverIP, int port)
        {
            if (client == null)
            {
                client = new TcpClient(serverIP, port);
                stream = client.GetStream();

                // 启动接收数据的异步任务
                Task.Run(() => StartReceivingAsync());
            }
        }

        public void Disconnect()
        {
            if (client != null)
            {
                // 关闭网络流
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }

                // 关闭TCP客户端
                client.Close();
                client = null;
            }
        }

        //public string SendAndReceive(string message)
        //{
        //    if (client != null && stream != null)
        //    {
        //        byte[] data = Encoding.ASCII.GetBytes(message);
        //        stream.Write(data, 0, data.Length);

        //        byte[] responseData = new byte[256];
        //        int bytes = stream.Read(responseData, 0, responseData.Length);
        //        return Encoding.ASCII.GetString(responseData, 0, bytes);
        //    }
        //    return null;
        //}

        public void Send(string message)
        {
            if (client != null && stream != null)
            {
                byte[] data = Encoding.ASCII.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
        }


        public async Task StartReceivingAsync()
        {
            try
            {
                while (client != null && stream != null)
                {
                    byte[] responseData = new byte[4096];
                    int bytes = await stream.ReadAsync(responseData, 0, responseData.Length);
                    // 如果没有接收到数据，退出循环
                    //if (bytes == 0)
                    //{
                    //    break;
                    //}
                    string receivedData = Encoding.ASCII.GetString(responseData, 0, bytes);

                    // 触发事件
                    DataReceived?.Invoke(this, receivedData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in receiving data: " + ex.Message);
                // 可能需要处理断开连接的情况
            }
        }
    }
}
