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


namespace xplr_aoa_door_demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        
        static SerialPort _serialPort1 = null;
        static SerialPort _serialPort2 = null;
        static bool _continue1 = true;
        static bool _continue2 = true;
        static List<string> _rxDataList = new List<string>();
        static Thread _readThread1;
        static Thread _readThread2;

        static UdpClient _udpClient;
        static IPEndPoint _udpSender;

        static Thread _readUdpThread1;

        static int BadPackets = 0;


        public static object Dispatcher { get; private set; }
        public static object DispatcherPriority { get; private set; }

        private void Form1_Load(object sender, EventArgs e)
        {
            _rxDataList = new List<string>();

            comboBoxRssi.SelectedIndex = 0;
            comboBoxUpdateInterval.SelectedIndex = 0;

            getFTDISerialPort();

            if (String.IsNullOrWhiteSpace(xplr_aoa_door_demo.Properties.Settings.Default.Comport1) == false)
            {
                for (int i = 0; i < this.listBoxSerialPort1.Items.Count; i++)
                {
                    if (this.listBoxSerialPort1.Items[i].ToString().Contains(xplr_aoa_door_demo.Properties.Settings.Default.Comport1) == true)
                    {
                        this.listBoxSerialPort1.SelectedIndex = i;
                        break;
                    }
                }
            }

            if (String.IsNullOrWhiteSpace(xplr_aoa_door_demo.Properties.Settings.Default.Comport2) == false)
            {
                for (int i = 0; i < this.listBoxSerialPort2.Items.Count; i++)
                {
                    if (this.listBoxSerialPort2.Items[i].ToString().Contains(xplr_aoa_door_demo.Properties.Settings.Default.Comport2) == true)
                    {
                        this.listBoxSerialPort2.SelectedIndex = i;
                        break;
                    }
                }
            }

            trackBarAzimuth1Max.Value = xplr_aoa_door_demo.Properties.Settings.Default.trackBarAzimuth1Max;
            trackBarAzimuth1Min.Value = xplr_aoa_door_demo.Properties.Settings.Default.trackBarAzimuth1Min;
            trackBarAzimuth2Max.Value = xplr_aoa_door_demo.Properties.Settings.Default.trackBarAzimuth2Max;
            trackBarAzimuth2Min.Value = xplr_aoa_door_demo.Properties.Settings.Default.trackBarAzimuth2Min;
            comboBoxUpdateInterval.Text = xplr_aoa_door_demo.Properties.Settings.Default.UpdateInterval;
            checkBoxFlow.Checked = xplr_aoa_door_demo.Properties.Settings.Default.UartFlow;
            textBoxUdpPort.Text = xplr_aoa_door_demo.Properties.Settings.Default.udpPort;
            comboBoxAnchor1.Text = xplr_aoa_door_demo.Properties.Settings.Default.anchorAddress1;
            comboBoxAnchor2.Text = xplr_aoa_door_demo.Properties.Settings.Default.anchorAddress2;
            comboBoxTag.Text = xplr_aoa_door_demo.Properties.Settings.Default.tagAddress;
            checkBoxUdpOpen.Checked = xplr_aoa_door_demo.Properties.Settings.Default.UdpOpen;
            checkBoxSerialOpen.Checked = xplr_aoa_door_demo.Properties.Settings.Default.SerialOpen;

            if (xplr_aoa_door_demo.Properties.Settings.Default.SerialSelected)
            {
                tabControlInterface.SelectedTab = tabPageSerial;
            }
            else
            {
                tabControlInterface.SelectedTab = tabPageUdp;
            }

            labelAzimuth1.Text = "Azimuth Left Angle: " + trackBarAzimuth1.Value + "°";
            labelAzimuth2.Text = "Azimuth Right Angle: " + trackBarAzimuth2.Value + "°";
            labelAzimuth1Max.Text = "Azimuth Left Max Angle: " + trackBarAzimuth1Max.Value + "°";
            labelAzimuth1Min.Text = "Azimuth Left Min Angle: " + trackBarAzimuth1Min.Value + "°";
            labelAzimuth2Max.Text = "Azimuth Right Max Angle: " + trackBarAzimuth2Max.Value + "°";
            labelAzimuth2Min.Text = "Azimuth Right Min Angle: " + trackBarAzimuth2Min.Value + "°";

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

                    _readThread1 = new Thread(Read1);
                    _readThread2 = new Thread(Read2);
                    _continue1 = true;
                    _continue2 = true;
                    _readThread1.Start();
                    _readThread2.Start();

                    timerRxData.Interval = 1;
                    timerRxData.Enabled = true;

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


        public static void Read1()
        {
            while (_continue1)
            {
                try
                {
                    if (_serialPort1 != null)
                    {
                        if (_serialPort1.IsOpen)
                        {
                            string message = _serialPort1.ReadLine();

                            if (message.Contains("+UUDF:"))
                            {
                                _rxDataList.Add(message);
                            }
                        }
                    }
                }
                catch (TimeoutException) { }
                catch (System.IO.IOException) { }
            }
        }

        public static void Read2()
        {
            while (_continue2)
            {
                try
                {
                    if (_serialPort2 != null)
                    {
                        if (_serialPort2.IsOpen)
                        {
                            string message = _serialPort2.ReadLine();

                            if (message.Contains("+UUDF:"))
                            {
                                _rxDataList.Add(message);
                            }
                        }
                    }
                }
                catch (TimeoutException) { }
                catch (System.IO.IOException) { }
            }
        }

        public static void ReadUdp1()
        {
            while (_continue1)
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
                        xplr_aoa_door_demo.Properties.Settings.Default.Comport1 = listBoxSerialPort1.SelectedItem.ToString();
                    }
                }
                if (listBoxSerialPort2.SelectedItem != null)
                {
                    if (String.IsNullOrWhiteSpace(listBoxSerialPort2.SelectedItem.ToString()) == false)
                    {
                        xplr_aoa_door_demo.Properties.Settings.Default.Comport2 = listBoxSerialPort2.SelectedItem.ToString();
                    }
                }

                xplr_aoa_door_demo.Properties.Settings.Default.trackBarAzimuth1Max = trackBarAzimuth1Max.Value;
                xplr_aoa_door_demo.Properties.Settings.Default.trackBarAzimuth1Min = trackBarAzimuth1Min.Value;
                xplr_aoa_door_demo.Properties.Settings.Default.trackBarAzimuth2Max = trackBarAzimuth2Max.Value;
                xplr_aoa_door_demo.Properties.Settings.Default.trackBarAzimuth2Min = trackBarAzimuth2Min.Value;

                xplr_aoa_door_demo.Properties.Settings.Default.UpdateInterval = comboBoxUpdateInterval.Text;
                xplr_aoa_door_demo.Properties.Settings.Default.UartFlow = checkBoxFlow.Checked;
                xplr_aoa_door_demo.Properties.Settings.Default.udpPort = textBoxUdpPort.Text;
                xplr_aoa_door_demo.Properties.Settings.Default.anchorAddress1 = comboBoxAnchor1.Text;
                xplr_aoa_door_demo.Properties.Settings.Default.anchorAddress2 = comboBoxAnchor2.Text;
                xplr_aoa_door_demo.Properties.Settings.Default.tagAddress = comboBoxTag.Text;
                xplr_aoa_door_demo.Properties.Settings.Default.UdpOpen = checkBoxUdpOpen.Checked;
                xplr_aoa_door_demo.Properties.Settings.Default.SerialOpen = checkBoxSerialOpen.Checked;

                if (tabControlInterface.SelectedTab == tabPageSerial)
                {
                    xplr_aoa_door_demo.Properties.Settings.Default.SerialSelected = true;
                }
                else
                {
                    xplr_aoa_door_demo.Properties.Settings.Default.SerialSelected = false;
                }

                xplr_aoa_door_demo.Properties.Settings.Default.Save();
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
                timerRxData.Enabled = false;
                _continue1 = false;
                _continue2 = false;

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

        Int32 averageRssi37_1 = 0;
        Int32 averageRssi38_1 = 0;
        Int32 averageRssi39_1 = 0;

        Int32 averageAngle_1_1 = 0;
        Int32 averageAngle_1_2 = 0;
        Int32 averageAngle_1_3 = 0;
        
        Int32 averageAngle_2_1 = 0;
        Int32 averageAngle_2_2 = 0;
        Int32 averageAngle_2_3 = 0;

        Boolean Trigger1 = false;
        Boolean Trigger2 = false;

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

        private void trackBarAzimuth1Min_Scroll(object sender, EventArgs e)
        {
            if (trackBarAzimuth1Max.Value <= trackBarAzimuth1Min.Value)
            {
                trackBarAzimuth1Min.Value = trackBarAzimuth1Max.Value;
            }

            labelAzimuth1Min.Text = "Azimuth Left Min Angle: " + trackBarAzimuth1Min.Value + "°";

            trackBarAzimuth1_Scroll(this, null);
        }

        private void trackBarAzimuth1Max_Scroll(object sender, EventArgs e)
        {
            if (trackBarAzimuth1Min.Value >= trackBarAzimuth1Max.Value)
            {
                trackBarAzimuth1Max.Value = trackBarAzimuth1Min.Value;
            }

            labelAzimuth1Max.Text = "Azimuth Left Max Angle: " + trackBarAzimuth1Max.Value + "°";

            trackBarAzimuth1_Scroll(this, null);
        }

        private void trackBarAzimuth2Max_Scroll(object sender, EventArgs e)
        {
            if (trackBarAzimuth2Min.Value >= trackBarAzimuth2Max.Value)
            {
                trackBarAzimuth2Max.Value = trackBarAzimuth2Min.Value;
            }

            labelAzimuth2Max.Text = "Azimuth Right Max Angle: " + trackBarAzimuth2Max.Value + "°";

            trackBarAzimuth2_Scroll(this, null);
        }

        private void trackBarAzimuth2Min_Scroll(object sender, EventArgs e)
        {
            if (trackBarAzimuth2Max.Value <= trackBarAzimuth2Min.Value)
            {
                trackBarAzimuth2Min.Value = trackBarAzimuth2Max.Value;
            }

            labelAzimuth2Min.Text = "Azimuth Right Min Angle: " + trackBarAzimuth2Min.Value + "°";

            trackBarAzimuth2_Scroll(this, null);
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

            _continue1 = true;

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
            _continue1 = false;

            
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
            Int32 angle = trackBarAzimuth1.Value;
            labelAzimuth1.Text = "Azimuth Left Angle: " + trackBarAzimuth1.Value + "°";

            Trigger1 = false;

            if ((trackBarAzimuth1Min.Value <= angle) && (trackBarAzimuth1Max.Value >= angle))
            {
                panelAzimuth1.BackColor = Color.Green;
                Trigger1 = true;
            }
            else
            {
                panelAzimuth1.BackColor = Color.Red;

            }

            if (Trigger1 && Trigger2)
            {
                panelDoor.BackColor = Color.Green;
                labelDoor.Text = "Door is Open";
            }
            else
            {
                panelDoor.BackColor = Color.Red;
                labelDoor.Text = "Door is Closed";
            }
        }

        private void trackBarAzimuth2_Scroll(object sender, EventArgs e)
        {
            Int32 angle = trackBarAzimuth2.Value;
            labelAzimuth2.Text = "Azimuth Right Angle: " + trackBarAzimuth2.Value + "°";

            Trigger2 = false;

            if ((trackBarAzimuth2Min.Value <= angle) && (trackBarAzimuth2Max.Value >= angle))
            {
                panelAzimuth2.BackColor = Color.Green;
                Trigger2 = true;
            }
            else
            {
                panelAzimuth2.BackColor = Color.Red;

            }

            if (Trigger1 && Trigger2)
            {
                panelDoor.BackColor = Color.Green;
                labelDoor.Text = "Door is Open";
            }
            else
            {
                panelDoor.BackColor = Color.Red;
                labelDoor.Text = "Door is Closed";
            }
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
                comboBoxRssi.Visible = true;
            }
            else
            {
                chartAzimuth.Visible = false;
                progressBarRssi1.Visible = false;
                progressBarRssi2.Visible = false;
                labelRssi1.Visible = false;
                labelRssi2.Visible = false;
                comboBoxRssi.Visible = false;
            }
        }

        private void buttonCalibrate_Click(object sender, EventArgs e)
        {
            if ((trackBarAzimuth1.Value - 20) >= trackBarAzimuth1Min.Minimum)
            {
                trackBarAzimuth1Min.Value = trackBarAzimuth1.Value - 20;
            }
            else
            {
                trackBarAzimuth1Min.Value = trackBarAzimuth1.Minimum;
            }

            if ((trackBarAzimuth1.Value + 20) <= trackBarAzimuth1Max.Maximum)
            {
                trackBarAzimuth1Max.Value = trackBarAzimuth1.Value + 20;
            }
            else
            {
                trackBarAzimuth1Max.Value = trackBarAzimuth1.Maximum;
            }

            trackBarAzimuth1Min_Scroll(this, null);
            trackBarAzimuth1Max_Scroll(this, null);

            if ((trackBarAzimuth2.Value - 20) >= trackBarAzimuth2Min.Minimum)
            {
                trackBarAzimuth2Min.Value = trackBarAzimuth2.Value - 20;
            }
            else
            {
                trackBarAzimuth2Min.Value = trackBarAzimuth2.Minimum;
            }

            if ((trackBarAzimuth2.Value + 20) <= trackBarAzimuth2Max.Maximum)
            {
                trackBarAzimuth2Max.Value = trackBarAzimuth2.Value + 20;
            }
            else
            {
                trackBarAzimuth2Max.Value = trackBarAzimuth2.Maximum;
            }

            trackBarAzimuth2Min_Scroll(this, null);
            trackBarAzimuth2Max_Scroll(this, null);
        }

        private void buttonSwap_Click(object sender, EventArgs e)
        {
            string temp = "";
            temp = comboBoxAnchor1.Text;
            comboBoxAnchor1.Text = comboBoxAnchor2.Text;
            comboBoxAnchor2.Text = temp;
        }

        private void timerRxData_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_rxDataList.Count > 0)
                {
                    labelBadPackets.Text = "Bad Packets: " + BadPackets.ToString();

                    if (BadPackets > 100)
                    {
                        buttonCloseUdp_Click(this, null);
                        buttonOpenUdp_Click(this, null);
                        BadPackets = 0;
                    }

                    if (String.IsNullOrWhiteSpace(_rxDataList.First()))
                    {
                        BadPackets++;
                        return;
                    }
                    string message = _rxDataList.First().ToString();
                    _rxDataList.RemoveAt(0);

                    labelRxBuffer1.Text = _rxDataList.Count.ToString();

                    if (String.IsNullOrWhiteSpace(message))
                    {
                        BadPackets++;
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

                            averageAngle_1_3 = averageAngle_1_2;
                            averageAngle_1_2 = averageAngle_1_1;
                            averageAngle_1_1 = angle1;

                            Int32 averageAngle1 = (averageAngle_1_1 + averageAngle_1_2 + averageAngle_1_3) / 3;

                            trackBarAzimuth1.Value = averageAngle1;
                            labelAzimuth1.Text = "Azimuth Left Angle: " + trackBarAzimuth1.Value + "°";

                            Trigger1 = false;

                            if ((trackBarAzimuth1Min.Value <= averageAngle1) && (trackBarAzimuth1Max.Value >= averageAngle1))
                            {
                                panelAzimuth1.BackColor = Color.Green;
                                Trigger1 = true;
                            }
                            else
                            {
                                panelAzimuth1.BackColor = Color.Red;
                            }

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


                            chartAzimuth.Series["azimuth_left"].Points.AddXY(timestamp_ms, averageAngle1);

                            if (chartAzimuth.Series["azimuth_left"].Points.Count > 200)
                            {
                                chartAzimuth.Series["azimuth_left"].Points.RemoveAt(0);
                            }

                        }
                        else if (comboBoxAnchor2.Text.Contains(anchor_id) == true)
                        {
                            Int32 angle2 = Convert.ToInt32(angle_azimuth);

                            angle2 = -angle2;

                            averageAngle_2_3 = averageAngle_2_2;
                            averageAngle_2_2 = averageAngle_2_1;
                            averageAngle_2_1 = angle2;

                            Int32 averageAngle2 = (averageAngle_2_1 + averageAngle_2_2 + averageAngle_2_3) / 3;

                            trackBarAzimuth2.Value = averageAngle2;
                            labelAzimuth2.Text = "Azimuth Right Angle: " + trackBarAzimuth2.Value + "°";

                            Trigger2 = false;

                            if ((trackBarAzimuth2Min.Value <= averageAngle2) && (trackBarAzimuth2Max.Value >= averageAngle2))
                            {
                                panelAzimuth2.BackColor = Color.Green;
                                Trigger2 = true;
                            }
                            else
                            {
                                panelAzimuth2.BackColor = Color.Red;
                            }

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

                            chartAzimuth.Series["azimuth_right"].Points.AddXY(timestamp_ms, averageAngle2);

                            if (chartAzimuth.Series["azimuth_right"].Points.Count > 200)
                            {
                                chartAzimuth.Series["azimuth_right"].Points.RemoveAt(0);
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

                        if (Trigger1 && Trigger2)
                        {
                            panelDoor.BackColor = Color.Green;
                            labelDoor.Text = "Door is Open";
                        }
                        else
                        {
                            panelDoor.BackColor = Color.Red;
                            labelDoor.Text = "Door is Closed";
                        }


                        if (checkBoxLog.Checked)
                        {
                            loggInfo("id: " + ed_instance_id + ", rssi:" + rssi_pol1 + ", azimuth:" + angle_azimuth + ", elevation: " + angle_elevation + /*", rssi2: " + rssi_pol2 +*/ ", channel: " + channel + ", anchor_id: " + anchor_id + ", timestamp_ms: " + timestamp_ms);
                        }

                        if (comboBoxRssi.SelectedItem.ToString().Contains("All average"))
                        {
                            Int32 rssi = Convert.ToInt32(rssi_pol1);

                            if (channel.Contains("37"))
                            {
                                averageRssi37_1 = (averageRssi37_1 + rssi) / 2;
                            }
                            if (channel.Contains("38"))
                            {
                                averageRssi38_1 = (averageRssi38_1 + rssi) / 2;
                            }
                            if (channel.Contains("39"))
                            {
                                averageRssi39_1 = (averageRssi39_1 + rssi) / 2;
                            }

                            Int32 averageRssi = (averageRssi37_1 + averageRssi38_1 + averageRssi39_1) / 3;
                            progressBarRssi1.Value = 100 + averageRssi;
                            labelRssi1.Text = "RSSI (All average): " + averageRssi.ToString();
                        }
                        else
                        {
                            if (comboBoxRssi.SelectedItem.ToString().Contains("All"))
                            {
                                Int32 rssi = Convert.ToInt32(rssi_pol1);
                                progressBarRssi1.Value = 100 + rssi;
                                labelRssi1.Text = "RSSI (All): " + rssi.ToString();
                            }
                            else if (comboBoxRssi.SelectedItem.ToString().Contains("37") && channel.Contains("37"))
                            {
                                Int32 rssi = Convert.ToInt32(rssi_pol1);
                                progressBarRssi1.Value = 100 + rssi;
                                labelRssi1.Text = "RSSI (37): " + rssi.ToString();
                            }
                            else if (comboBoxRssi.SelectedItem.ToString().Contains("38") && channel.Contains("38"))
                            {
                                Int32 rssi = Convert.ToInt32(rssi_pol1);
                                progressBarRssi1.Value = 100 + rssi;
                                labelRssi1.Text = "RSSI (38): " + rssi.ToString();
                            }
                            else if (comboBoxRssi.SelectedItem.ToString().Contains("39") && channel.Contains("39"))
                            {
                                Int32 rssi = Convert.ToInt32(rssi_pol1);
                                progressBarRssi1.Value = 100 + rssi;
                                labelRssi1.Text = "RSSI (39): " + rssi.ToString();
                            }
                        }
                    }
                }
            }
            catch
            { }
        }
    }
}


