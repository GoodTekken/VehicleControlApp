using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using JTDevice;

namespace VehicleControlApp
{
    public partial class MainWindow : Window
    {
        private PositionData positionData = new PositionData();
        Device device;

        public MainWindow()
        {
            InitializeComponent();
            SingletonClient.Instance.DataReceived += OnDataReceived;
            this.DataContext = positionData;
            device = new Device(0, txtServerIP.Text, int.Parse(txtPort.Text));
        }
        private bool isConnected = false;
        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                try
                {
                    //SingletonClient.Instance.Connect(txtServerIP.Text, int.Parse(txtPort.Text));
                    device.Connect();
                    AppendMessage("Connected to server");
                    isConnected = true;
                    btnConnect.Content = "Disconnect";
                    btnConnect.Background = new SolidColorBrush(Colors.Green);
                }
                catch (Exception ex)
                {
                    AppendMessage($"Error: {ex.Message}");
                }
            }
            else
            {
                //SingletonClient.Instance.Disconnect();
                device.Disconnect();
                AppendMessage("Disconnected from server");
                isConnected = false;
                btnConnect.Content = "Connect";
                btnConnect.Background = new SolidColorBrush(Colors.Red);
            }
        }

        //private void SendCommand_Click(object sender, RoutedEventArgs e)
        //{
        //    string response = SingletonClient.Instance.SendAndReceive(BuildCommand(GetSelectedMode()));
        //    txtMessages.Text += $"Server Response: {response}\n";
        //}

        private void Mode_Checked(object sender, RoutedEventArgs e)
        {
            // 确保窗口已完全加载，避免在初始化时触发事件
            if (!IsInitialized) return;

            if (isConnected == false)
            {
                AppendMessage("请先连接车载服务器，端口:5677.");
                return;
            }

            var radioButton = sender as RadioButton;
            if (radioButton != null && radioButton.IsChecked == true)
            {
                string selectedMode = GetSelectedMode();
                SendModeChangeCommand(selectedMode);
            }
        }

        private string GetSelectedMode()
        {
            if (rbNT8000.IsChecked == true)
                return "NT8000";
            if (rbOther.IsChecked == true)
                return "Other";
            if (rbLocalModeTM.IsChecked == true)
                return "LocalModeTM";
            return "";
        }

        private void SendModeChangeCommand(string mode)
        {
            // 根据选择的模式构建指令
            string command = BuildModeChangeCommand(mode);

            // 发送指令
            SingletonClient.Instance.Send(command);
            //AppendMessage($"Server Response: {response}");
        }

        private string BuildModeChangeCommand(string mode)
        {
            switch (mode)
            {
                //Master Mode
                case "NT8000":
                    return "<CPI2><Request><Operation Tag=\"Operation.OrderMode\" Path=\"Automatic\"><OrderMode Source=\"NT8000\" /></Operation></Request></CPI2>\n";

                //Local Mode
                case "Other":
                    return "<CPI2><Request><Operation Tag=\"Operation.OrderMode\" Path=\"Automatic\"><OrderMode Source=\"Other\" /></Operation></Request></CPI2>\n";

                //Local Mode with TM
                case "LocalModeTM":
                    return "<CPI2><Request><Operation Tag=\"Operation.OrderMode\" Path=\"Automatic\"><OrderMode Source=\"LocalModeTM\" /></Operation></Request></CPI2>\n";
            }
            return string.Empty;
        }

        private void SendCommand_Click(object sender, RoutedEventArgs e)
        {
            if(isConnected == false)
            {
                AppendMessage("请先连接车载服务器，端口:5677.");
                return;
            }

            if(rbOther.IsChecked != true)
            {
                AppendMessage("请先选择单机模式.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtPointID.Text))
            {
                string command = BuildLocalOrderCommand();
                SingletonClient.Instance.Send(command);
                //AppendMessage($"Server Response: {response}");
            }
            else
            {
                AppendMessage("Destination Point is required to send a command.");
            }
        }

        private string BuildLocalOrderCommand()
        {
            StringBuilder commandBuilder = new StringBuilder();
            commandBuilder.Append("<CPI2><Request><Operation Tag=\"Operation.LocalOrder\" Path=\"Automatic\"><GotoPoint");

            // 添加必要的PointID
            commandBuilder.Append($" PointID=\"{txtPointID.Text}\"");

            // 可选的ViaPoints
            if (!string.IsNullOrWhiteSpace(txtViaPoints.Text))
            {
                commandBuilder.Append($" ViaPoints=\"{txtViaPoints.Text}\"");
            }

            commandBuilder.Append(">");

            // 可选的StartLoadOperation
            if (!string.IsNullOrWhiteSpace(txtOperationCode.Text))
            {
                commandBuilder.Append($"<StartLoadOperation OperationCode=\"{txtOperationCode.Text}\"");

                if (!string.IsNullOrWhiteSpace(txtOperationParam1.Text))
                {
                    commandBuilder.Append($" OperationParam1=\"{txtOperationParam1.Text}\"");
                }

                if (!string.IsNullOrWhiteSpace(txtOperationParam2.Text))
                {
                    commandBuilder.Append($" OperationParam2=\"{txtOperationParam2.Text}\"");
                }

                commandBuilder.Append(" />");
            }

            commandBuilder.Append("</GotoPoint></Operation></Request></CPI2>\n");
            return commandBuilder.ToString();
        }

        private void ClearMessages_Click(object sender, RoutedEventArgs e)
        {
            txtMessages.Clear();
        }

        private void AppendMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtMessages.AppendText(message + "\n");
                txtMessages.ScrollToEnd();
            });
        }

        private void UpdateUI()
        {
            // 确保UI更新在UI线程上执行
            Dispatcher.Invoke(() =>
            {
                txtAngle.Text = positionData.Angle?.ToString() ?? "N/A";
                txtAngle2.Text = positionData.Angle2?.ToString() ?? "N/A";
                txtNavLevel.Text = positionData.NavLevel?.ToString() ?? "N/A";
                txtValid.Text = positionData.Valid?.ToString() ?? "N/A";
                txtX.Text = positionData.X?.ToString() ?? "N/A";
                txtY.Text = positionData.Y?.ToString() ?? "N/A";
                txtReqExtSegment.Text = positionData.ReqExtSegment?.ToString() ?? "N/A";
                txtReqSegmentId.Text = positionData.ReqSegmentId?.ToString() ?? "N/A";
                // 更新其他TextBlock
            });
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                var tabControl = (TabControl)sender;
                var selectedTab = (TabItem)tabControl.SelectedItem;

                if (selectedTab.Name == "ExternalPathTab")
                {
                    string command = "<CPI2><Request><Subscribe Tag=\"external_638361791109148754\" MinInterval=\"500\"><Item Tag=\"Position.Angle\" Path=\"Status.Position.Angle\" /><Item Tag=\"Position.Angle2\" Path=\"Status.Position.Angle2\" /><Item Tag=\"Position.NavLevel\" Path=\"Status.Position.NavLevel\" /><Item Tag=\"Position.Valid\" Path=\"Status.Position.Valid\" /><Item Tag=\"Position.X\" Path=\"Status.Position.X\" /><Item Tag=\"Position.Y\" Path=\"Status.Position.Y\" /><Item Tag=\"ExternalPath.ReqExtSegment\" Path=\"Status.ExternalPath.ReqExtSegment\" /><Item Tag=\"ExternalPath.ReqSegmentId\" Path=\"Status.ExternalPath.ReqSegmentId\" /></Subscribe></Request></CPI2>\n";
                    // 发送指令
                    SingletonClient.Instance.Send(command);
                    //AppendMessage($"Server Response: {response}");
                }
            }
        }

        private void OnDataReceived(object sender, string data)
        {
            // 处理接收到的数据
            AppendMessage("Received data: " + data);
            // 接收到的数据进行解析
            ParseAndCategorizeMessage(data);
        }

        private void ParseAndCategorizeMessage(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNode operationNode = doc.SelectSingleNode("//Operation");
            XmlNode subscribeNode = doc.SelectSingleNode("//Subscribe");

            if (operationNode != null)
            {
                // 处理Operation类型的报文
     
                string operationTag = operationNode.Attributes["Tag"]?.Value ?? "未知";

                foreach (XmlNode childNode in operationNode.ChildNodes)
                {
                    string error = childNode.Attributes["Error"]?.Value ?? "未知";
                    string errorID = childNode.Attributes["ErrorID"]?.Value ?? "未知";
                    AppendMessage($"Operation: {operationTag}, Node: {childNode.Name}, Error = {error}, ErrorID = {errorID}");
                }
            }
            else if (subscribeNode != null)
            {
                XmlNodeList items = doc.SelectNodes("//Item");
                foreach (XmlNode item in items)
                {
                    string tag = item.Attributes["Tag"].Value;
                    int value = int.Parse(item.InnerText);

                    switch (tag)
                    {
                        case "Position.Angle":
                            positionData.Angle = value;
                            break;
                        case "Position.Angle2":
                            positionData.Angle2 = value;
                            break;
                        case "Position.NavLevel":
                            positionData.NavLevel = value;
                            break;
                        case "Position.Valid":
                            positionData.Valid = value;
                            break;
                        case "Position.X":
                            positionData.X = value;
                            break;
                        case "Position.Y":
                            positionData.Y = value;
                            break;
                        case "ExternalPath.ReqExtSegment":
                            positionData.ReqExtSegment = value;
                            break;
                        case "ExternalPath.ReqSegmentId":
                            positionData.ReqSegmentId = value;
                            break;
                        default: 
                            break;        
                    }
                }
                AppendMessage(positionData.ToString());
                //UpdateUI();  //手动赋值  本工程使用的是自动绑定
            }
            else
            {
                AppendMessage("Unknown Message Type");
            }
        }


    }
}
