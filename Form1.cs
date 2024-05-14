using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Management;
using Microsoft.Win32;
using System.Net;  
using System.Net.Sockets;  
using System.Threading;
using System.Reflection;
using System.Net.NetworkInformation;
using System.Drawing.Drawing2D;

namespace access_control
{
    public partial class Form1 : Form
    {

        private int ipSize = 24;//intersection point size 

        public Form1()
        {
            InitializeComponent();
        }

        public class Line
        {
            public Point s { get; set; }//start point
            public Point e { get; set; }//end point
            public Line(Point s, Point e)//line from start point to end point
            {
                this.s = s;
                this.e = e;
            }
        }

        Bitmap bm = null;

        static SerialPort _serialPort1 = null;
        static SerialPort _serialPort2 = null;

        static List<string> _rxDataList = new List<string>();

        static UdpClient _udpClient;
        static IPEndPoint _udpSender;

        static Thread _readUdpThread1;

        static int BadPackets = 0;

        public event EventHandler OnNewLineReceived1;
        public event EventHandler OnNewLineReceived2;

        int StateMachine1 = 0;
        StringBuilder stringBuffer1 = new StringBuilder();
        int StateMachine2 = 0;
        StringBuilder stringBuffer2 = new StringBuilder();

        int[] arrayAverage1;
        int[] arrayAverage2;

        int[] arrayAngle1;
        int[] arrayAngle2;
        int oldArrayAngle1 = 0;
        int oldArrayAngle2 = 0;
        int arrayIndexAngle1 = 0;
        int arrayIndexAngle2 = 0;

        public static object Dispatcher { get; private set; }
        public static object DispatcherPriority { get; private set; }

        System.Windows.Forms.Timer NewDataTimer1 = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer NewDataTimer2 = new System.Windows.Forms.Timer();
        private void InitTimer()
        {
            NewDataTimer1.Interval = 50;
            NewDataTimer1.Tick += NewDataTimer1_Tick;

            NewDataTimer2.Interval = 50;
            NewDataTimer2.Tick += NewDataTimer2_Tick;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            this.BackColor = Color.FromArgb(26, 26, 26);

            _rxDataList = new List<string>();

            InitOnNewLineReceived1();
            InitOnNewLineReceived2();

            InitTimer();

            arrayAverage1 = new int[(int)numericUpDownAverage.Value];
            arrayAverage2 = new int[(int)numericUpDownAverage.Value];

            arrayAngle1 = new int[(int)numericUpDownAngle.Value];
            arrayAngle2 = new int[(int)numericUpDownAngle.Value];

            comboBoxUpdateInterval.SelectedIndex = 0;

            getFTDISerialPort();

            if (String.IsNullOrWhiteSpace(access_control.Properties.Settings.Default.Comport1) == false)
            {
                for (int i = 0; i < this.listBoxSerialPort1.Items.Count; i++)
                {
                    if (this.listBoxSerialPort1.Items[i].ToString().Contains(access_control.Properties.Settings.Default.Comport1) == true)
                    {
                        this.listBoxSerialPort1.SelectedIndex = i;
                        break;
                    }
                }
            }

            if (String.IsNullOrWhiteSpace(access_control.Properties.Settings.Default.Comport2) == false)
            {
                for (int i = 0; i < this.listBoxSerialPort2.Items.Count; i++)
                {
                    if (this.listBoxSerialPort2.Items[i].ToString().Contains(access_control.Properties.Settings.Default.Comport2) == true)
                    {
                        this.listBoxSerialPort2.SelectedIndex = i;
                        break;
                    }
                }
            }

            comboBoxUpdateInterval.Text = access_control.Properties.Settings.Default.UpdateInterval;
            checkBoxFlow.Checked = access_control.Properties.Settings.Default.UartFlow;
            textBoxUdpPort.Text = access_control.Properties.Settings.Default.udpPort;
            comboBoxAnchor1.Text = access_control.Properties.Settings.Default.anchorAddress1;
            comboBoxAnchor2.Text = access_control.Properties.Settings.Default.anchorAddress2;
            comboBoxTag.Text = access_control.Properties.Settings.Default.tagAddress;
            checkBoxUdpOpen.Checked = access_control.Properties.Settings.Default.UdpOpen;
            checkBoxSerialOpen.Checked = access_control.Properties.Settings.Default.SerialOpen;
            calibratePoint.X = access_control.Properties.Settings.Default.PosX;
            calibratePoint.Y = access_control.Properties.Settings.Default.PosY;
            numericUpDownAverage.Value = access_control.Properties.Settings.Default.AverageAngle;

            if (access_control.Properties.Settings.Default.SerialSelected)
            {
                tabControlInterface.SelectedTab = tabPageSerial;
            }
            else
            {
                tabControlInterface.SelectedTab = tabPageUdp;
            }

            labelAzimuth1.Text = "Left Angle: " + trackBarAzimuth1.Value + "°";
            labelAzimuth2.Text = "Right Angle: " + trackBarAzimuth2.Value + "°";

            trackBarAzimuth1_Scroll(this, null);
            trackBarAzimuth2_Scroll(this, null);



            if(checkBoxUdpOpen.Checked)
            {
                buttonOpenUdp_Click(this, null);
            }
            if (checkBoxSerialOpen.Checked)
            {
                buttonOpenSerial_Click(this, null);
            }

            bm = new Bitmap(pictureBoxAngle.Size.Width, pictureBoxAngle.Size.Height);
        }

        class SerialStringMessgae : EventArgs
        {
            public string message;
        }


        private void InitOnNewLineReceived1()
        {
            OnNewLineReceived1 += Form1_OnNewLineReceived1;
        }

        private void InitOnNewLineReceived2()
        {
            OnNewLineReceived2 += Form1_OnNewLineReceived2;
        }

        void Form1_OnNewLineReceived1(object sender, EventArgs e)
        {
            SerialStringMessgae STM = e as SerialStringMessgae;
            string messgae = STM.message;

            if (String.IsNullOrWhiteSpace(messgae) == false)
            {
                RxData(1, messgae);

                if (checkBoxLog.Checked)
                {
                    loggInfo("1: " + messgae);
                }
            }


        }

        void Form1_OnNewLineReceived2(object sender, EventArgs e)
        {
            SerialStringMessgae STM = e as SerialStringMessgae;
            string messgae = STM.message;

            if (String.IsNullOrWhiteSpace(messgae) == false)
            {
                RxData(2, messgae);

                if (checkBoxLog.Checked)
                {
                    loggInfo("2: " + messgae);
                }
            }
        }

        private  void getFTDISerialPort()
        {
            listBoxSerialPort1.BeginUpdate();
            listBoxSerialPort2.BeginUpdate();

            listBoxSerialPort1.Items.Clear();
            listBoxSerialPort2.Items.Clear();

            using (ManagementClass i_Entity = new ManagementClass("Win32_PnPEntity"))
            {
                foreach (ManagementObject i_Inst in i_Entity.GetInstances())
                {
                    Object o_Guid = i_Inst.GetPropertyValue("ClassGuid");
                    if (o_Guid == null || o_Guid.ToString().ToUpper() != "{4D36E978-E325-11CE-BFC1-08002BE10318}")
                        continue; // Skip all devices except device class "PORTS"

                    String s_Caption = i_Inst.GetPropertyValue("Caption").ToString();
                    String s_Manufact = i_Inst.GetPropertyValue("Manufacturer").ToString();
                    String s_DeviceID = i_Inst.GetPropertyValue("PnpDeviceID").ToString();
                    String s_RegPath = "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Enum\\" + s_DeviceID + "\\Device Parameters";
                    String s_PortName = Registry.GetValue(s_RegPath, "PortName", "").ToString();

                    int s32_Pos = s_Caption.IndexOf(" (COM");
                    if (s32_Pos > 0) // remove COM port from description
                        s_Caption = s_Caption.Substring(0, s32_Pos);

                    if(checkBoxFTDI.Checked)
                    {
                        if (s_Manufact.Contains("FTDI"))
                        {
                            listBoxSerialPort1.Items.Add(s_PortName);
                          }
                    }
                    else
                    {
                        listBoxSerialPort1.Items.Add(s_PortName);
                    }

                    /*loggInfo("Port Name:    " + s_PortName);
                    loggInfo("Description:  " + s_Caption);
                    loggInfo("Manufacturer: " + s_Manufact);
                    loggInfo("Device ID:    " + s_DeviceID);
                    loggInfo("-----------------------------------");*/
                    
                }
            }

            // Sort the comport list
            string[] portNames = new string[listBoxSerialPort1.Items.Count];

            for (int i = 0; i < listBoxSerialPort1.Items.Count; i++)
            {
                portNames[i] = listBoxSerialPort1.Items[i].ToString();
            }

            var sortedList = portNames.OrderBy(port => Convert.ToInt32(port.Replace("COM", string.Empty)));

            listBoxSerialPort1.Items.Clear();

            foreach (string port in sortedList)
            {
                listBoxSerialPort1.Items.Add(port);
            }

            listBoxSerialPort2.Items.AddRange(listBoxSerialPort1.Items);

            listBoxSerialPort1.EndUpdate();
            listBoxSerialPort2.EndUpdate();
        }

        private void buttonOpenSerial_Click(object sender, EventArgs e)
        {
            try
            {
                if(listBoxSerialPort1.SelectedItem != null)
                {
                    _serialPort1 = new SerialPort(listBoxSerialPort1.SelectedItem.ToString(), 115200);
                    if(checkBoxFlow.Checked)
                    {
                       _serialPort1.Handshake = Handshake.RequestToSend;
                    }
                    else
                    {
                        _serialPort1.Handshake = Handshake.None;
                    }
                        _serialPort1.Open();
                }
                if (listBoxSerialPort2.SelectedItem != null)
                {
                    _serialPort2 = new SerialPort(listBoxSerialPort2.SelectedItem.ToString(), 115200);
                    if (checkBoxFlow.Checked)
                    {
                        _serialPort2.Handshake = Handshake.RequestToSend;
                    }
                    else
                    {
                        _serialPort2.Handshake = Handshake.None;
                    }
                    _serialPort2.Open();
                }
                
                if ((_serialPort1 != null) && (_serialPort1.IsOpen))
                {
                    _serialPort1.ReadExisting();
                }

                if ((_serialPort2 != null) && (_serialPort2.IsOpen))
                {
                    _serialPort2.ReadExisting();
                }


                if (_serialPort1.IsOpen)
                {
                    NewDataTimer1.Enabled = true;
                }

                if (_serialPort2.IsOpen)
                {
                    NewDataTimer2.Enabled = true;
                }

                { 
                    buttonOpenSerial.Enabled = false;
                    buttonCloseSerial.Enabled = true;

                    var objChartRssi = chartRssi.ChartAreas[0];
                    objChartRssi.AxisX.IntervalType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Number;
                    objChartRssi.AxisX.Minimum = 1;
                    objChartRssi.AxisX.Maximum = 200;

                    objChartRssi.AxisY.IntervalType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Number;
                    objChartRssi.AxisY.Minimum = -100;
                    objChartRssi.AxisY.Maximum = 0;

                    InitAzimuth();
                }
            }
            catch
            {

            }
        }

        public void InitAzimuth()
        {
            var objChartAzimuth = chartAzimuth.ChartAreas[0];
            objChartAzimuth.AxisX.IntervalType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Number;
            objChartAzimuth.AxisX.Minimum = 1;
            objChartAzimuth.AxisX.Maximum = 200;

            objChartAzimuth.AxisY.IntervalType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Number;
            objChartAzimuth.AxisY.Minimum = -90;
            objChartAzimuth.AxisY.Maximum = 90;

            chartAzimuth.Series.Clear();

            chartAzimuth.Series.Add("azimuth_left");
            chartAzimuth.Series["azimuth_left"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chartAzimuth.Series["azimuth_left"].Color = Color.Blue;

            chartAzimuth.Series.Add("azimuth_right");
            chartAzimuth.Series["azimuth_right"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chartAzimuth.Series["azimuth_right"].Color = Color.Red;
        }

        void NewDataTimer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if ((_serialPort1 != null) && _serialPort1.IsOpen)
                {
                    string newData = _serialPort1.ReadExisting();
                    foreach (char c in newData)
                    {
                        switch (StateMachine1)
                        {
                            case 0:
                                // waiting for '\r'
                                if (c == '\r')
                                {
                                    StateMachine1 = 1;
                                }
                                else
                                {
                                    stringBuffer1.Append(c);                                }
                                break;
                            case 1:
                                // waiting for '\n'
                                if (c == '\n')
                                {
                                    if (OnNewLineReceived1 != null)
                                    {
                                        if (String.IsNullOrWhiteSpace(stringBuffer1.ToString()) == false)
                                        {
                                            SerialStringMessgae STM = new SerialStringMessgae();
                                            STM.message = stringBuffer1.ToString();
                                            OnNewLineReceived1(this, STM);
                                        }
                                    }
                                }
                                // after parsing the message we reset the state machine
                                stringBuffer1 = new StringBuilder();
                                StateMachine1 = 0;
                                break;
                        }
                    }
                }
            }
            catch (TimeoutException) { }
            catch (System.IO.IOException) { }
        }

        void NewDataTimer2_Tick(object sender, EventArgs e)
        {
            try
            {
                if ((_serialPort2 != null) && _serialPort2.IsOpen)
                {
                    string newData = _serialPort2.ReadExisting();
                    foreach (char c in newData)
                    {
                        switch (StateMachine2)
                        {
                            case 0:
                                // waiting for '\r'
                                if (c == '\r')
                                {
                                    StateMachine2 = 1;
                                }
                                else
                                {
                                    stringBuffer2.Append(c);
                                }
                                break;
                            case 1:
                                // waiting for '\n'
                                if (c == '\n')
                                {
                                    if (OnNewLineReceived2 != null)
                                    {
                                        if (String.IsNullOrWhiteSpace(stringBuffer2.ToString()) == false)
                                        {
                                            SerialStringMessgae STM = new SerialStringMessgae();
                                            STM.message = stringBuffer2.ToString();
                                            OnNewLineReceived2(this, STM);
                                        }
                                    }
                                }
                                // after parsing the message we reset the state machine
                                stringBuffer2 = new StringBuilder();
                                StateMachine2 = 0;
                                break;
                        }
                    }
                }
            }
            catch (TimeoutException) { }
            catch (System.IO.IOException) { }
        }

        public static void ReadUdp1()
        {
            //while (_continue1)
            {
                try
                {
                    if (_udpClient != null && _udpSender != null)
                    {
                        byte[] data = new byte[1500];

                        try
                        {
                            data = _udpClient.Receive(ref _udpSender);
                        }
                        catch (ObjectDisposedException)
                        { }
                        catch (System.Net.Sockets.SocketException)
                        { }

                        string message = Encoding.ASCII.GetString(data);

                          
                        if (message.Contains("+UUDF:"))
                        {
                            _rxDataList.Add(message);
                            message = "";
                        }
                        else
                        {
                            BadPackets++;
                        }
                    }
                }
                catch (TimeoutException) { }
            }
        }

        private void loggInfo(string log)
        {
            richTextBoxInfo.AppendText(log + "\n");
            richTextBoxInfo.SelectionStart = richTextBoxInfo.Text.Length;
            richTextBoxInfo.ScrollToCaret();
        }
        
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (listBoxSerialPort1.SelectedItem != null)
                {
                    if (String.IsNullOrWhiteSpace(listBoxSerialPort1.SelectedItem.ToString()) == false)
                    {
                        access_control.Properties.Settings.Default.Comport1 = listBoxSerialPort1.SelectedItem.ToString();
                    }
                }
                if (listBoxSerialPort2.SelectedItem != null)
                {
                    if (String.IsNullOrWhiteSpace(listBoxSerialPort2.SelectedItem.ToString()) == false)
                    {
                        access_control.Properties.Settings.Default.Comport2 = listBoxSerialPort2.SelectedItem.ToString();
                    }
                }

                access_control.Properties.Settings.Default.UpdateInterval = comboBoxUpdateInterval.Text;
                access_control.Properties.Settings.Default.UartFlow = checkBoxFlow.Checked;
                access_control.Properties.Settings.Default.udpPort = textBoxUdpPort.Text;
                access_control.Properties.Settings.Default.anchorAddress1 = comboBoxAnchor1.Text;
                access_control.Properties.Settings.Default.anchorAddress2 = comboBoxAnchor2.Text;
                access_control.Properties.Settings.Default.tagAddress = comboBoxTag.Text;
                access_control.Properties.Settings.Default.UdpOpen = checkBoxUdpOpen.Checked;
                access_control.Properties.Settings.Default.SerialOpen = checkBoxSerialOpen.Checked;
                access_control.Properties.Settings.Default.PosX = calibratePoint.X;
                access_control.Properties.Settings.Default.PosY = calibratePoint.Y;

                access_control.Properties.Settings.Default.AverageAngle = (int)numericUpDownAverage.Value;

                if (tabControlInterface.SelectedTab == tabPageSerial)
                {
                    access_control.Properties.Settings.Default.SerialSelected = true;
                }
                else
                {
                    access_control.Properties.Settings.Default.SerialSelected = false;
                }

                access_control.Properties.Settings.Default.Save();
                buttonCloseSerial_Click(this, null);

                buttonCloseUdp_Click(this, null);
            }
            catch
            { }
        }
        
        private void buttonCloseSerial_Click(object sender, EventArgs e)
        {
            try
            {
                Thread.Sleep(100);

                _serialPort1.Close();
                _serialPort2.Close();
                _serialPort1.Dispose();
                _serialPort2.Dispose();
                _serialPort1 = null;
                _serialPort2 = null;
            }
            catch
            {

            }

            buttonOpenSerial.Enabled = true;
            buttonCloseSerial.Enabled = false;
        }

        private void checkBoxFTDI_CheckedChanged(object sender, EventArgs e)
        {
            getFTDISerialPort();
        }

        private void buttonEnable_Click(object sender, EventArgs e)
        {
            if(_serialPort1 != null)
            {
                if(_serialPort1.IsOpen)
                {
                    _serialPort1.ReadExisting();
                    _serialPort1.Write("AT+UDFENABLE=1\r");
                }
            }
            if (_serialPort2 != null)
            {
                if (_serialPort2.IsOpen)
                {
                    _serialPort2.ReadExisting();
                    _serialPort2.Write("AT+UDFENABLE=1\r");
                }
            }
        }

        private void buttonDisable_Click(object sender, EventArgs e)
        {
            if(_serialPort1 != null)
            {
                _serialPort1.Write("AT+UDFENABLE=0\r");
                Thread.Sleep(200);
                _serialPort1.ReadExisting();
            }
            if (_serialPort2 != null)
            {
                _serialPort2.Write("AT+UDFENABLE=0\r");
                Thread.Sleep(200);
                _serialPort2.ReadExisting();
            }
        }

        private void checkBoxLog_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxLog.Checked)
            {
                richTextBoxInfo.Visible = true;
                labelRxBuffer1.Visible = true;
                labelRx.Visible = true;
                labelBadPackets.Visible = true;
            }
            else
            {
                richTextBoxInfo.Visible = false;
                labelRxBuffer1.Visible = false;
                labelRx.Visible = false;
                labelBadPackets.Visible = false;
            }
        }

        private void buttonOpenUdp_Click(object sender, EventArgs e)
        {
            byte[] data = new byte[1024];

            int port = Convert.ToInt32(textBoxUdpPort.Text);

            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, port);
            _udpClient = new UdpClient(ipep);
            _udpClient.DontFragment = true;

            Console.WriteLine("Waiting for a client...");

            _udpSender = new IPEndPoint(IPAddress.Any, 0);

            InitAzimuth();

            
            _readUdpThread1 = new Thread(ReadUdp1);
            _readUdpThread1.Priority = ThreadPriority.Normal;
            _readUdpThread1.Start();

            timerRxData.Interval = 1;
            timerRxData.Enabled = true;

        
            buttonOpenUdp.Enabled = false;
            buttonCloseUdp.Enabled = true;
        }

        private void buttonCloseUdp_Click(object sender, EventArgs e)
        {
            buttonOpenUdp.Enabled = true;
            buttonCloseUdp.Enabled = false;
            timerRxData.Enabled = false;
            
            
            Thread.Sleep(100);

            _udpClient.Close();
            _udpSender = null;
        }

           
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://u-blox.com");
        }

        private void trackBarAzimuth1_Scroll(object sender, EventArgs e)
        {
            //this.Invalidate();

            Int32 angle = trackBarAzimuth1.Value;
            labelAzimuth1.Text = "Left Angle: " + trackBarAzimuth1.Value + "°";

            pictureBoxAngle.Refresh();
        }

        private void trackBarAzimuth2_Scroll(object sender, EventArgs e)
        {
            Int32 angle = trackBarAzimuth2.Value;
            labelAzimuth2.Text = "Right Angle: " + trackBarAzimuth2.Value + "°";

            pictureBoxAngle.Refresh();
        }

        private void panelDoor_Resize(object sender, EventArgs e)
        {
            int x = (panelDoor.Size.Width - labelDoor.Size.Width) / 2;
            int y = (panelDoor.Size.Height - labelDoor.Size.Height) / 2;

            labelDoor.Location = new Point(x, y);
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            try
            {
                comboBoxAnchor1.Items.Clear();
                comboBoxAnchor1.Text = "";

                comboBoxAnchor2.Items.Clear();
                comboBoxAnchor2.Text = "";

                comboBoxTag.Items.Clear();
                comboBoxTag.Text = "";

                BadPackets = 0;

                _rxDataList.Clear();
                richTextBoxInfo.Clear();

                chartAzimuth.Series["azimuth_left"].Points.Clear();
                chartAzimuth.Series["azimuth_right"].Points.Clear();

            }
            catch { }
        }

        private void checkBoxShowDoor_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBoxGraph.Checked)
            {
                chartAzimuth.Visible = true;
                progressBarRssi1.Visible = true;
                progressBarRssi2.Visible = true;
                labelRssi1.Visible = true;
                labelRssi2.Visible = true;
                labelAzimuth1.Visible = true;
                labelAzimuth2.Visible = true;
                trackBarAzimuth1.Visible = true;
                trackBarAzimuth2.Visible = true;
            }
            else
            {
                chartAzimuth.Visible = false;
                progressBarRssi1.Visible = false;
                progressBarRssi2.Visible = false;
                labelRssi1.Visible = false;
                labelRssi2.Visible = false;
                trackBarAzimuth1.Visible = false;
                trackBarAzimuth2.Visible = false;
            }
        }

        private void buttonCalibrate_Click(object sender, EventArgs e)
        {
            calibratePoint = new Point(0, 0);
            pictureBoxAngle.Refresh();
        }

        private void buttonSwap_Click(object sender, EventArgs e)
        {
            string temp = "";
            temp = comboBoxAnchor1.Text;
            comboBoxAnchor1.Text = comboBoxAnchor2.Text;
            comboBoxAnchor2.Text = temp;
        }

        void RxData(int device, string message)
        {
            try
            {
                //if (_rxDataList.Count > 0)
                {
                    labelBadPackets.Text = "Bad Packets: " + BadPackets.ToString();

                    if (BadPackets > 100)
                    {
                        //buttonCloseUdp_Click(this, null);
                        //buttonOpenUdp_Click(this, null);
                        //BadPackets = 0;
                    }

                    if (String.IsNullOrWhiteSpace(message))
                    {
                        //BadPackets++;
                        return;
                    }

                    if (message.Contains("+UUDF:"))
                    {
                        string[] param = message.Split(',');

                        if (param.Length < 9)
                        {
                            BadPackets++;
                            return;
                        }

                        string ed_instance_id = param[0].Trim();
                        ed_instance_id = ed_instance_id.Replace("+UUDF:", "");
                        string rssi_pol1 = param[1].Trim();
                        string angle_azimuth = param[2].Trim();
                        string angle_elevation = param[3].Trim();
                        string rssi_pol2 = param[4].Trim();
                        string channel = param[5].Trim();
                        string anchor_id = param[6].Trim();
                        anchor_id = anchor_id.Replace("\"", "");
                        string user_defined_str = param[7].Trim();
                        string timestamp_ms = param[8].Trim();


                        if (comboBoxTag.Text.Contains(ed_instance_id) == false)
                        {
                            if (comboBoxTag.Items.Contains(ed_instance_id) == false)
                            {
                                if (String.IsNullOrWhiteSpace(comboBoxTag.Text) == true)
                                {
                                    comboBoxTag.Text = ed_instance_id;
                                }
                                comboBoxTag.Items.Add(ed_instance_id);
                            }
                        }

                        if (comboBoxAnchor1.Text.Contains(anchor_id) == true)
                        {
                            Int32 angle1 = Convert.ToInt32(angle_azimuth);

                            angle1 = -angle1;

                            if(listBox1.Visible)
                            {
                                listBox1.Items.Insert(0, angle1.ToString());

                                if(listBox1.Items.Count > 200)
                                {
                                    listBox1.Items.RemoveAt(200);
                                }
                            }

                            if (numericUpDownAngle.Value > 0)
                            {
                                if (arrayIndexAngle1 < numericUpDownAngle.Value)
                                {
                                    arrayAngle1[arrayIndexAngle1] = angle1;
                                    arrayIndexAngle1++;
                                }
                                else
                                {
                                    arrayIndexAngle1 = 0;
                                    arrayAngle1[arrayIndexAngle1] = angle1;
                                }

                                Array.Sort(arrayAngle1);


                                double median1;

                                if (arrayAngle1.Length % 2 == 0)
                                {
                                    int middleIndex = arrayAngle1.Length / 2;
                                    median1 = (arrayAngle1[middleIndex - 1] + arrayAngle1[middleIndex]) / 2.0;
                                }
                                else
                                {
                                    int middleIndex = arrayAngle1.Length / 2;
                                    median1 = arrayAngle1[middleIndex];
                                }

                                trackBarAzimuth1.Value = (int)median1;

                            }
                            else
                            {
                                trackBarAzimuth1.Value = (int)angle1;
                            }

                            labelAzimuth1.Text = "Left Angle: " + trackBarAzimuth1.Value + "°";



                            try
                            {
                                if (timestamp_ms.Contains("+UUFD:"))
                                {
                                    BadPackets++;
                                    return;
                                }
                                Int32 test = Convert.ToInt32(timestamp_ms);
                            }
                            catch
                            {
                                BadPackets++;
                                return;
                            }

                        }
                        else if (comboBoxAnchor2.Text.Contains(anchor_id) == true)
                        {
                            Int32 angle2 = Convert.ToInt32(angle_azimuth);

                            angle2 = -angle2;

                            if (listBox2.Visible)
                            {
                                listBox2.Items.Insert(0, angle2.ToString());

                                if (listBox2.Items.Count > 200)
                                {
                                    listBox2.Items.RemoveAt(200);
                                }
                            }

                            if (numericUpDownAngle.Value > 0)
                            {
                                if (arrayIndexAngle2 < numericUpDownAngle.Value)
                                {
                                    arrayAngle2[arrayIndexAngle2] = angle2;
                                    arrayIndexAngle2++;
                                }
                                else
                                {
                                    arrayIndexAngle2 = 0;
                                    arrayAngle2[arrayIndexAngle2] = angle2;
                                }

                                Array.Sort(arrayAngle2);


                                double median2;

                                if (arrayAngle2.Length % 2 == 0)
                                {
                                    int middleIndex = arrayAngle2.Length / 2;
                                    median2 = (arrayAngle1[middleIndex - 1] + arrayAngle1[middleIndex]) / 2.0;
                                }
                                else
                                {
                                    int middleIndex = arrayAngle2.Length / 2;
                                    median2 = arrayAngle2[middleIndex];
                                }

                                trackBarAzimuth2.Value = (int)median2;

                            }
                            else
                            {
                                trackBarAzimuth2.Value = (int)angle2;
                            }
                            labelAzimuth2.Text = "Right Angle: " + trackBarAzimuth2.Value + "°";

                            try
                            {
                                if (timestamp_ms.Contains("+UUFD:"))
                                {
                                    BadPackets++;
                                    return;
                                }
                                Int32 test = Convert.ToInt32(timestamp_ms);
                            }
                            catch
                            {
                                BadPackets++;
                                return;
                            }

                            //Int32 rssi = Convert.ToInt32(rssi_pol1);

                            //if (arrayIndexAverage1 < numericUpDownAverage.Value)
                            //{
                            //    arrayAverage2[arrayIndexAverage1] = rssi;
                            //    arrayIndexAverage1++;
                            //}
                            //else
                            //{
                            //    arrayIndexAverage1 = 0;
                            //    arrayAverage2[arrayIndexAverage1] = rssi;
                            //}

                            //Array.Sort(arrayAverage2);


                            //int sum = 0;
                            //int average = 0;

                            //// Skip highest and lowest for better average
                            //for (int i = 0; i < arrayAverage2.Length; i++)
                            //{
                            //    sum += arrayAverage2[i];
                            //}

                            //average = sum / arrayAverage2.Length;

                            //average = (average + oldArrayAverage2) / 2;

                            //oldArrayAverage2 = average;



                            //progressBarRssi2.Value = 100 + Convert.ToInt32(average);
                            //labelRssi2.Text = "RSSI (All average): " + average.ToString();


                            if (checkBoxGraph.Checked)
                            {
                                chartAzimuth.Series["azimuth_right"].Points.AddXY(timestamp_ms, angle2);

                                if (chartAzimuth.Series["azimuth_right"].Points.Count > 500)
                                {
                                    chartAzimuth.Series["azimuth_right"].Points.RemoveAt(0);
                                }
                            }
                        }
                        else
                        {
                            if (comboBoxAnchor1.Items.Contains(anchor_id) == false)
                            {
                                if (String.IsNullOrWhiteSpace(comboBoxAnchor1.Text) == true)
                                {
                                    comboBoxAnchor1.Text = anchor_id;
                                }
                                comboBoxAnchor1.Items.Add(anchor_id);
                            }
                            if (comboBoxAnchor2.Items.Contains(anchor_id) == false)
                            {
                                if (String.IsNullOrWhiteSpace(comboBoxAnchor2.Text) == true)
                                {
                                    comboBoxAnchor2.Text = anchor_id;
                                }
                                comboBoxAnchor2.Items.Add(anchor_id);
                                if (comboBoxAnchor2.Text.Equals(comboBoxAnchor1.Text))
                                {
                                    if (comboBoxAnchor2.Items.Count > 1)
                                    {
                                        comboBoxAnchor2.SelectedIndex = 1;
                                    }
                                }

                            }
                        }

                        Application.DoEvents();
                        pictureBoxAngle.Refresh();
                        

                        if (checkBoxLog.Checked)
                        {
                            loggInfo("id: " + ed_instance_id + ", rssi:" + rssi_pol1 + ", azimuth:" + angle_azimuth + ", elevation: " + angle_elevation + /*", rssi2: " + rssi_pol2 +*/
                            ", channel: " + channel + ", anchor_id: " + anchor_id + ", timestamp_ms: " + timestamp_ms);
                        }


                    }
                }
            }
            catch (Exception e)
            {
                string test = e.ToString();
            }
        }


        private Point Intersect(Line a, Line b)
        { 
            //try
            {
                double A1 = a.e.Y - a.s.Y;
                double B1 = a.s.X - a.e.X;
                double C1 = A1 * a.s.X + B1 * a.s.Y;

                double A2 = b.e.Y - b.s.Y;
                double B2 = b.s.X - b.e.X;
                double C2 = A2 * b.s.X + B2 * b.s.Y;

                double numitor = A1 * B2 - A2 * B1;
                if (numitor == 0) return new Point(0, 0);
                else
                {
                    double x = (B2 * C1 - B1 * C2) / numitor;
                    double y = (A1 * C2 - A2 * C1) / numitor;
                    return new Point(Convert.ToInt32(x), Convert.ToInt32(y));
                }
            }
            //catch
            { }

        }

        private void numericUpDownAverage_ValueChanged(object sender, EventArgs e)
        {
            arrayAverage1 = new int[(int)numericUpDownAverage.Value];
            arrayAverage2 = new int[(int)numericUpDownAverage.Value];
        }

        private void buttonMacFilter_Click(object sender, EventArgs e)
        {
            try
            {
                if (_serialPort1 != null)
                {
                    _serialPort1.Write("AT+UDFFILT=2,2,\"" + comboBoxTag.Text + "\"\r");
                    Thread.Sleep(200);
                    _serialPort1.ReadExisting();

                    _serialPort1.Write("AT&W\r");
                    Thread.Sleep(200);
                    _serialPort1.ReadExisting();

                    _serialPort1.Write("AT+CPWROFF\r");
                    Thread.Sleep(200);
                    _serialPort1.ReadExisting();
                }
                if (_serialPort2 != null)
                {
                    _serialPort2.Write("AT+UDFFILT=2,2,\"" + comboBoxTag.Text + "\"\r");
                    Thread.Sleep(200);
                    _serialPort2.ReadExisting();

                    _serialPort2.Write("AT&W\r");
                    Thread.Sleep(200);
                    _serialPort2.ReadExisting();

                    _serialPort2.Write("AT+CPWROFF\r");
                    Thread.Sleep(200);
                    _serialPort2.ReadExisting();
                }
            }
            catch
            { }

        }

     
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(tabControlInterface.Visible == false)
            {
                tabControlInterface.Visible = true;
            }
            else
            {
                tabControlInterface.Visible = false;
            }
        }

        private void labelDoor_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDownAngle_ValueChanged(object sender, EventArgs e)
        {
            arrayAngle1 = new int[(int)numericUpDownAngle.Value];
            arrayAngle2 = new int[(int)numericUpDownAngle.Value];
        }

        Point calibratePoint;

        private void pictureBoxAngle_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                Graphics g = e.Graphics;
                //g.DrawImage(bm, 0, 0);
                g.SmoothingMode = SmoothingMode.AntiAlias;

               
                Pen p1 = new Pen(Color.FromArgb(255, 110, 89), 10);
                Pen p2 = new Pen(Color.FromArgb(255, 110, 89), 10);
                Pen pi = new Pen(Color.White, 10);

                Line A = null;
                Line B = null;

                if (trackBarAzimuth1.Value < 0)
                {
                    A = new Line(new Point(0, 0), new Point((pictureBoxAngle.Width + trackBarAzimuth1.Value * 9), pictureBoxAngle.Height));
                }
                else
                {
                    A = new Line(new Point(0, 0), new Point(pictureBoxAngle.Width, pictureBoxAngle.Height - trackBarAzimuth1.Value * 9));
                }

                if (trackBarAzimuth2.Value < 0)
                {
                    B = new Line(new Point(pictureBoxAngle.Width, 0), new Point(0, pictureBoxAngle.Height + trackBarAzimuth2.Value * 9));//blue line
                }
                else
                {
                    B = new Line(new Point(pictureBoxAngle.Width, 0), new Point(trackBarAzimuth2.Value * 9, pictureBoxAngle.Height));
                }

                g.DrawLine(p1, A.s, A.e);
                g.DrawLine(p2, B.s, B.e);

                Point iPoint = Intersect(A, B);

                if ((calibratePoint.X == 0) && (calibratePoint.Y == 0))
                {
                    calibratePoint = iPoint;
                }


                Pen paccess = new Pen(Color.ForestGreen, 20);

                g.DrawRectangle(paccess, paccess.Width /2, paccess.Width/2, pictureBoxAngle.Width - paccess.Width, calibratePoint.Y);

                if(iPoint.Y < calibratePoint.Y)
                {
                    panelDoor.BackColor = Color.ForestGreen;
                    labelDoor.Text = "Access Granted!";
                }
                else
                {
                    panelDoor.BackColor = Color.FromArgb(255, 110, 89);
                    labelDoor.Text = "No Access";
                }

                //g.DrawArc(pi, iPoint.X - ipSize, iPoint.Y - ipSize, 2 * ipSize, 2 * ipSize, 0, 360);
                //g.DrawArc(pi, iPoint.X - ipSize * 2, iPoint.Y - ipSize * 2, 4 * ipSize, 4 * ipSize, 0, 360);
                g.FillEllipse(Brushes.White, iPoint.X - ipSize * 2, iPoint.Y - ipSize * 2, ipSize * 4, ipSize * 4);
                //g.FillEllipse(Brushes.White, iPoint.X - ipSize / 2, iPoint.Y - ipSize / 2, ipSize, ipSize);

            }
            catch
            {

            }
        }

        private void checkBoxRawAngle_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBoxRawAngle.Checked == true)
            {
                listBox1.Visible = true;
                listBox2.Visible = true;
            }
            else
            {
                listBox1.Visible = false;
                listBox2.Visible = false;
            }
        }

        private void numericUpDownCalibrate_ValueChanged(object sender, EventArgs e)
        {
           
        }

        private void buttonSmooth_Click(object sender, EventArgs e)
        {
            try
            {
                _serialPort1.Write("AT+UDCFG=10," + numericUpDownSmooth.Value.ToString() + "\r");
                Thread.Sleep(200);
                _serialPort1.ReadExisting();

                _serialPort1.Write("AT&W\r");
                Thread.Sleep(200);
                _serialPort1.ReadExisting();

                _serialPort1.Write("AT+CPWROFF\r");
                Thread.Sleep(200);
                _serialPort1.ReadExisting();


                _serialPort2.Write("AT+UDCFG=10," + numericUpDownSmooth.Value.ToString() + "\r");
                Thread.Sleep(200);
                _serialPort2.ReadExisting();

                _serialPort2.Write("AT&W\r");
                Thread.Sleep(200);
                _serialPort2.ReadExisting();

                _serialPort2.Write("AT+CPWROFF\r");
                Thread.Sleep(200);
                _serialPort2.ReadExisting();
            }
            catch
            { }
        }

        private void buttonFactory_Click(object sender, EventArgs e)
        {
            try
            {
                _serialPort1.Write("AT+UFACTORY\r");
                Thread.Sleep(200);
                _serialPort1.ReadExisting();

                _serialPort1.Write("AT+CPWROFF\r");
                Thread.Sleep(200);
                _serialPort1.ReadExisting();

                _serialPort2.Write("AT+UFACTORY\r");
                Thread.Sleep(200);
                _serialPort2.ReadExisting();

                _serialPort2.Write("AT+CPWROFF\r");
                Thread.Sleep(200);
                _serialPort2.ReadExisting();
            }
            catch
            { }
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            try
            {
                _serialPort1.Write("AT+CPWROFF\r");
                Thread.Sleep(200);
                _serialPort1.ReadExisting();

                _serialPort2.Write("AT+CPWROFF\r");
                Thread.Sleep(200);
                _serialPort2.ReadExisting();
            }
            catch
            { }
        }
    }
}


