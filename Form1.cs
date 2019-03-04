using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Windows.Forms;
using log4net;
using System.Data.SQLite;
using Iocomp.Classes;

using System.IO;
using System.IO.Ports;

namespace InsituSystem
{
    public partial class frMain : Form
    {
        private SvrFactory svr = new SvrFactory();//初始化函数
        private SvrFactory_b svr_b = new SvrFactory_b();
        private SvrFactory_RTU svr_rtu = new SvrFactory_RTU();
        //1.声明自适应类实例  
        AutoSizeFormClass asc = new AutoSizeFormClass(); 


        private int curpos = 0;
        public int[] Encoder_Circle = new int[3];
        public int[] Last_Circle = new int[3];
        
        //SQLite日志记录
        public static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);//获取一个日志记录器
        public static void WriteLog(string str)
        {
            log.Info(str);//写入一条新log
        }
        
        //定义全局变量
        public delegate void MyInvoke(string str);
        
        public frMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //2. 为窗体添加Load事件，并在其方法Form1_Load中，调用类的初始化方法，记录窗体和其控件的初始位置和大小
            asc.controllInitializeSize(this);  
            //this.WindowState = FormWindowState.Maximized;
            log.Info("Program Load");
            //？？？
            RegistryKey hklm = Registry.LocalMachine;
            RegistryKey hs = hklm.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
            string[] values = new string[hs.ValueCount];
            for (int i = 0; i < hs.ValueCount; i++)//组合框设置
            {
                cbPort.Items.Add(hs.GetValue(hs.GetValueNames()[i]).ToString());
                cbPort.SelectedIndex = 0;
            }
           
            //订阅委托事件，，事件触发需产生一个线程处理
            serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived);
            comboBox_debug.SelectedIndex = 0;
            timer2.Start();

            comboBox1.SelectedIndex = 0;
            comboBox4.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
        }
        //3.为窗体添加SizeChanged事件，并在其方法Form1_SizeChanged中，调用类的自适应方法，完成自适应
        private void frMain_SizeChanged(object sender, EventArgs e)
        {
            asc.controlAutoSize(this);
        }
        //启动通信1
        private void switchLed1_ValueChanged(object sender, Iocomp.Classes.ValueBooleanEventArgs e)
        {
            if (switchLed1.Value)
            {
                svr.Start(txtIPAdr.Text);//取消注释，服务器IP地址，启动服务函数
            }
            else
                svr.Stop();
        }
        //启动通信2
        private void switchLed31_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            if (switchLed31.Value)
            {
                svr_b.Start(txtIPAdr.Text);
            }
            else                
                svr_b.Stop();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            switchLed1.Value = svr.GetConnectStatus();//开关状态
            switchLed31.Value = svr_b.GetConnectStatus();
            switchLed33.Value = svr_rtu.GetConnectStatus();
            labRxTx.Text = string.Format("{0}/{1}", svr.GetCurRxCounter(), svr.GetCurTxCounter());//下位机1，发/收次数
            label148.Text = string.Format("{0}/{1}", svr_b.GetCurRxCounter(), svr_b.GetCurTxCounter());//下位机2
            label32.Text = string.Format("{0}", svr_rtu.GetCurTxCounter());//RTU通信
            svnClock.Value = DateTime.Now;//系统时间
          
            if (!svr.GetConnectStatus() && !svr_b.GetConnectStatus() && !svr_rtu.GetConnectStatus())
                return;


            
            if (svr.CanReadHoldingReg())//完成数据读取，读保持寄存器,每次连接仅有第一次，成功之后置false
            {
                for (int i = 0; i < 6; i++)//显示串口开关状态，状态寄存器中和1080-1085对应
                {
                    if (svr.GetHoldValue(10 + i) == 1)
                        mtBtnCom.Buttons[i].BackColor = Color.Green;//串口开关
                    else
                        mtBtnCom.Buttons[i].BackColor = Color.Gray;
                }
                //GetHoldValue(20)对应120地址，SetHoldValue(120)和实际地址对应               
                //实时显示开关状态(包括第一次读取及后面界面上的设置更改)
                switchRocker11.Value = svr.GetHoldValue(20) > 0 ? true : false;//1-1开关，执行完接着执行窗体按钮，为啥？？？？（因为按钮状态变化了，再执行了一次按钮动作）
                switchRocker1.Value = svr.GetHoldValue(21) > 0 ? true : false;

                switchRocker2.Value = svr.GetHoldValue(22) > 0 ? true : false;
                switchRocker3.Value = svr.GetHoldValue(23) > 0 ? true : false;
                switchRocker4.Value = svr.GetHoldValue(24) > 0 ? true : false;
                switchRocker5.Value = svr.GetHoldValue(25) > 0 ? true : false;
                switchRocker6.Value = true;//三路常开
                switchRocker7.Value = true;
                switchRocker8.Value = svr.GetHoldValue(28) > 0 ? true : false;
                switchRocker9.Value = true;
                switchRocker10.Value = svr.GetHoldValue(30) > 0 ? true : false;
                switchRocker12.Value = svr.GetHoldValue(31) > 0 ? true : false;

                editInteger5.Value = svr.GetHoldValue(2);//多串口板1启动次数
                              
            }
            if (svr_b.CanReadHoldingReg())//下位机2完成数据读取，为啥是true也进不去？？？？（调试太慢进不去，很快被改为false了，正常运行可进去）
            {               
                    svr_b.SetHoldValue(20, 32767);//动臂
                    svr_b.SetHoldValue(21, 32767);
                    svr_b.SetHoldValue(22, 32767);
                    svr_b.SetHoldValue(23, 32767);//破碎头
                    editInteger6.Value = svr_b.GetHoldValue(2);
            }
            //仪表盘
            cpEquip.Value = (((double)svr_b.GetInputValue(2)) / 10).ToString("f1");//表盘艏向
            displayString16.Value = (((double)svr_b.GetInputValue(0)) / 10).ToString("f1");//深度
            displayString1.Value = svr.GetInputValue(36).ToString();//220V


            textBox25.Text = (((double)svr_b.GetInputValue(2)) / 10).ToString("f1");//艏向角
            displayString5.Value = (((double)svr_b.GetInputValue(3)) / 10).ToString("f1");//侧滚
            displayString6.Value = (((double)svr_b.GetInputValue(4)) / 10).ToString("f1");//横滚

            ////处理行走机构位移和速度
            ////------------------------------------左轮------------------------------------------------------
            //if ((svr.GetInputValue(55) >= 0 && svr.GetInputValue(55) <= 1024) && (svr.GetInputValue(56) >= 3072 && svr.GetInputValue(56) <= 4096))
            //{
            //    Encoder_Circle[0]++;
            //}
            //else if ((svr.GetInputValue(56) >= 0 && svr.GetInputValue(56) <= 1024) && (svr.GetInputValue(55) >= 3072 && svr.GetInputValue(55) <= 4096))
            //{
            //    Encoder_Circle[0]--;
            //}
           
            ////位移
            //if (Encoder_Circle[0] < 0)//编码器反转
            //{
            //    displayDouble_5556.Value = (Encoder_Circle[0] + 1) * 43.35 - (4096 - svr.GetInputValue(55)) * 0.010583496;//编码器r=3.45cm，每圈43.35厘米，每度0.010583496厘米
            //}
            //else//正转
            //{
            //    displayDouble_5556.Value = Encoder_Circle[0] * 43.35 + svr.GetInputValue(55) * 0.010583496;//每圈100毫米，每度0.1毫米
            //}
            ////速度
            //if (Encoder_Circle[0] > Last_Circle[0])//直接认为正转
            //{
            //    displayDouble_57.Value = (13.8 * Math.PI * svr.GetInputValue(57)) / 4096;//编码器半径为3.45厘米
            //}
            //else if (Encoder_Circle[0] < Last_Circle[0])//反转
            //{
            //    displayDouble_57.Value = -(13.8 * Math.PI * svr.GetInputValue(57)) / 4096;
            //}
            //else
            //{
            //    if (svr.GetInputValue(55) > svr.GetInputValue(56))//正转
            //    {
            //        displayDouble_57.Value = (13.8 * Math.PI * svr.GetInputValue(57)) / 4096;
            //    }
            //    else if (svr.GetInputValue(55) < svr.GetInputValue(56))
            //    {
            //        displayDouble_57.Value = -(13.8 * Math.PI * svr.GetInputValue(57)) / 4096;
            //    }
            //}
            //Last_Circle[0] = Encoder_Circle[0];
            ////----------------------右轮---------------------------------------------------------
            //if ((svr.GetInputValue(60) >= 0 && svr.GetInputValue(60) <= 1024) && (svr.GetInputValue(61) >= 3072 && svr.GetInputValue(61) <= 4096))
            //{
            //    Encoder_Circle[1]++;
            //}
            //else if ((svr.GetInputValue(61) >= 0 && svr.GetInputValue(61) <= 1024) && (svr.GetInputValue(60) >= 3072 && svr.GetInputValue(60) <= 4096))
            //{
            //    Encoder_Circle[1]--;
            //}

            ////位移
            //if (Encoder_Circle[1] < 0)//编码器反转
            //{
            //    displayDouble_6061.Value = (Encoder_Circle[1] + 1) * 43.35 - (4096 - svr.GetInputValue(60)) * 0.010583496;//编码器r=3.45cm，每圈43.35厘米，每度0.010583496厘米
            //}
            //else//正转
            //{
            //    displayDouble_6061.Value = Encoder_Circle[1] * 43.35 + svr.GetInputValue(60) * 0.010583496;//每圈100毫米，每度0.1毫米
            //}
            ////速度
            //if (Encoder_Circle[1] > Last_Circle[1])//直接认为正转
            //{
            //    displayDouble_62.Value = (13.8 * Math.PI * svr.GetInputValue(62)) / 4096;//编码器半径为3.45厘米
            //}
            //else if (Encoder_Circle[1] < Last_Circle[1])//反转
            //{
            //    displayDouble_62.Value = -(13.8 * Math.PI * svr.GetInputValue(62)) / 4096;
            //}
            //else
            //{
            //    if (svr.GetInputValue(60) > svr.GetInputValue(61))//正转
            //    {
            //        displayDouble_62.Value = (13.8 * Math.PI * svr.GetInputValue(62)) / 4096;
            //    }
            //    else if (svr.GetInputValue(60) < svr.GetInputValue(61))
            //    {
            //        displayDouble_62.Value = -(13.8 * Math.PI * svr.GetInputValue(62)) / 4096;
            //    }
            //}
            //Last_Circle[1] = Encoder_Circle[1];
            ////----------------------破碎头---------------------------------------------------------
            //if ((svr.GetInputValue(65) >= 0 && svr.GetInputValue(65) <= 1024) && (svr.GetInputValue(66) >= 3072 && svr.GetInputValue(66) <= 4096))
            //{
            //    Encoder_Circle[2]++;
            //}
            //else if ((svr.GetInputValue(66) >= 0 && svr.GetInputValue(66) <= 1024) && (svr.GetInputValue(65) >= 3072 && svr.GetInputValue(65) <= 4096))
            //{
            //    Encoder_Circle[2]--;
            //}           
            ////速度
            //if (Encoder_Circle[2] > Last_Circle[2])//直接认为正转
            //{
            //    displayDouble_67.Value = (13.8 * Math.PI * svr.GetInputValue(67)) / 4096;//编码器半径为3.45厘米
            //}
            //else if (Encoder_Circle[2] < Last_Circle[2])//反转
            //{
            //    displayDouble_67.Value = -(13.8 * Math.PI * svr.GetInputValue(67)) / 4096;
            //}
            //else
            //{
            //    if (svr.GetInputValue(60) > svr.GetInputValue(61))//正转
            //    {
            //        displayDouble_67.Value = (13.8 * Math.PI * svr.GetInputValue(67)) / 4096;
            //    }
            //    else if (svr.GetInputValue(60) < svr.GetInputValue(61))
            //    {
            //        displayDouble_67.Value = -(13.8 * Math.PI * svr.GetInputValue(67)) / 4096;
            //    }
            //}
            //Last_Circle[2] = Encoder_Circle[2];

            //------------------------------------------曲线绘图---------------------------------------------------
            if (true)
            {
                //int[] y1 = new int[1];
                //int[] y2 = new int[1];
                //int[] y3 = new int[1];                
                //y1[0] = svr_b.GetInputValue(17);

                //plot1.AddDataArray(curpos, y1);//y1即为y1[0]的值，参数2需为数组
                plot1.Channels.Trace[0].AddXY(curpos, svr_b.GetInputValue(17));//实际左轮速度，参数2为double
                plot1.Channels.Trace[1].AddXY(curpos, int.Parse(textBox29.Text));
                plot2.Channels.Trace[0].AddXY(curpos, svr_b.GetInputValue(27));//实际右轮速度
                plot2.Channels.Trace[1].AddXY(curpos, int.Parse(textBox36.Text));
                plot3.Channels.Trace[0].AddXY(curpos, double.Parse((((double)svr_b.GetInputValue(2)) / 10).ToString("f1")));//实际艏向角
                plot3.Channels.Trace[1].AddXY(curpos, int.Parse(textBox17.Text));//设定艏向角
                plot4.Channels.Trace[0].AddXY(curpos, svr_b.GetInputValue(30));//动臂角度

                curpos += 1;
                displayDouble6.Value = svr_b.GetInputValue(17);
                displayString2.Value = textBox29.Text;
                displayDouble15.Value = svr_b.GetInputValue(27);
                displayString7.Value = textBox36.Text;
                displayDouble18.Value = (((double)svr_b.GetInputValue(2)) / 10).ToString("f1");
                displayString8.Value = textBox17.Text;
                displayDouble22.Value = svr_b.GetInputValue(30);

                textBox34.Text = (((double)((ushort)svr_b.GetInputValue(41) - 32768) * 10) / 32768).ToString("f1");//调节器1（编码器2）输出值DA#2
                textBox41.Text = (((double)((ushort)svr_b.GetInputValue(42) - 32768) * 10) / 32768).ToString("f1");//调节器2（编码器1）输出值DA#3
                textBox22.Text = (((double)((ushort)svr_b.GetInputValue(41) - 32768) * 10) / 32768).ToString("f1");//DA#2
                textBox42.Text = (((double)((ushort)svr_b.GetInputValue(42) - 32768) * 10) / 32768).ToString("f1");//DA#3
            }

            //--------------------------------------子窗体控件处理--------------------------------------
            if (tabForm.SelectedIndex == 0)
            {   //1#电源转换板
                guVol11.Value = 0.008106 * svr.GetInputValue(0);//1-1电压
                guAmp11.Value = 0.0029 * svr.GetInputValue(1);//1-1电流
                guIns11.Value = 0.00062 * svr.GetInputValue(2) - 1.239;//1-1绝缘

                guVol12.Value = 0.008106 * svr.GetInputValue(3);
                guAmp12.Value = 0.0029 * svr.GetInputValue(4);
                guIns12.Value = 0.00062 * svr.GetInputValue(5) - 1.239;
               
                //2#电源转换板
                gaugeAngular4.Value = 0.008106 * svr.GetInputValue(6);
                gaugeAngular5.Value = 0.0029 * svr.GetInputValue(7);
                gaugeAngular6.Value = 0.00062 * svr.GetInputValue(8) - 1.239;

                gaugeAngular1.Value = 0.008106 * svr.GetInputValue(9);
                gaugeAngular2.Value = 0.0029 * svr.GetInputValue(10);
                gaugeAngular3.Value = 0.00062 * svr.GetInputValue(11) - 1.239;
                //3#电源转换板
                gaugeAngular10.Value = 0.008106 * svr.GetInputValue(12);
                gaugeAngular11.Value = 0.0029 * svr.GetInputValue(13);
                gaugeAngular12.Value = 0.00062 * svr.GetInputValue(14) - 1.239;

                gaugeAngular7.Value = 0.008106 * svr.GetInputValue(15);
                gaugeAngular8.Value = 0.0029 * svr.GetInputValue(16);
                gaugeAngular9.Value = 0.00062 * svr.GetInputValue(17) - 1.239;             
            }

            if (tabForm.SelectedIndex == 1)
            {
                //4#电源转换板
                gaugeAngular16.Value = 0.008106 * svr.GetInputValue(18);
                gaugeAngular17.Value = 0.0029 * svr.GetInputValue(19);
                gaugeAngular18.Value = 0.00062 * svr.GetInputValue(20) - 1.239;

                gaugeAngular13.Value = 0.008106 * svr.GetInputValue(21);
                gaugeAngular14.Value = 0.0029 * svr.GetInputValue(22);
                gaugeAngular15.Value = 0.00062 * svr.GetInputValue(23) - 1.239;
                //5#电源转换板
                gaugeAngular22.Value = 0.008106 * svr.GetInputValue(24);
                gaugeAngular23.Value = 0.0029 * svr.GetInputValue(25);
                gaugeAngular24.Value = 0.00062 * svr.GetInputValue(26) - 1.239;

                gaugeAngular19.Value = 0.008106 * svr.GetInputValue(27);
                gaugeAngular20.Value = 0.0029 * svr.GetInputValue(28);
                gaugeAngular21.Value = 0.00062 * svr.GetInputValue(29) - 1.239;
                //6#电源转换板
                gaugeAngular28.Value = 0.008106 * svr.GetInputValue(30);
                gaugeAngular29.Value = 0.0029 * svr.GetInputValue(31);
                gaugeAngular30.Value = 0.00062 * svr.GetInputValue(32) - 1.239;

                gaugeAngular25.Value = 0.008106 * svr.GetInputValue(33);
                gaugeAngular26.Value = 0.0029 * svr.GetInputValue(34);
                gaugeAngular27.Value = 0.00062 * svr.GetInputValue(35) - 1.239;

            }

            if (tabForm.SelectedIndex == 2)
            {
                displayDouble11.Value = (((double)((ushort)svr_b.GetInputValue(40) - 32768) * 10) / 32768).ToString("f1");//DA#1
                displayDouble14.Value = (((double)((ushort)svr_b.GetInputValue(41) - 32768) * 10) / 32768).ToString("f1");//DA#2
                displayDouble16.Value = (((double)((ushort)svr_b.GetInputValue(42) - 32768) * 10) / 32768).ToString("f1");//DA#3
                displayDouble17.Value = (((double)((ushort)svr_b.GetInputValue(43) - 32768) * 10) / 32768).ToString("f1");//DA#4
            }


            if (tabForm.SelectedIndex == 3)
            {            
                //测试Modbus RTU
                displayInteger1.Value = svr_rtu.GetInputValue(1);//程序步骤
            }

            if (tabForm.SelectedIndex == 4)
            {
                //串口参数读取（首次及最新更改的参数），注意：GetHoldValue函数读取地址从100开始，0地址对应寄存器100地址
                if (comboBox1.SelectedIndex == 0)//多串口板1（下位机1）
                {
                    editInteger11.Value = svr.GetHoldValue(0);//站地址
                    editInteger12.Value = svr.GetHoldValue(1);//波特率
                    editInteger7.Value = svr.GetHoldValue(2);//设置启动次数
                }
                else if (comboBox1.SelectedIndex == 5)//多串口板2（下位机2）
                {
                    editInteger11.Value = svr_b.GetHoldValue(0);
                    editInteger12.Value = svr_b.GetHoldValue(1);
                    editInteger7.Value = svr_b.GetHoldValue(2);
                }
                else if (comboBox1.SelectedIndex <= 4 && comboBox1.SelectedIndex >= 1)
                {
                    editInteger12.Value = svr.GetHoldValue(3 + (comboBox1.SelectedIndex - 1) * 4);//波特率
                    editInteger11.Value = svr.GetHoldValue(4 + (comboBox1.SelectedIndex - 1) * 4);//站地址
                    editInteger10.Value = svr.GetHoldValue(5 + (comboBox1.SelectedIndex - 1) * 4);//起始地址
                    editInteger9.Value = svr.GetHoldValue(6 + (comboBox1.SelectedIndex - 1) * 4);//长度
                }
                else if (comboBox1.SelectedIndex <= 9 && comboBox1.SelectedIndex >= 6)
                {
                    editInteger12.Value = svr_b.GetHoldValue(3 + (comboBox1.SelectedIndex - 6) * 4);
                    editInteger11.Value = svr_b.GetHoldValue(4 + (comboBox1.SelectedIndex - 6) * 4);
                    editInteger10.Value = svr_b.GetHoldValue(5 + (comboBox1.SelectedIndex - 6) * 4);
                    editInteger9.Value = svr_b.GetHoldValue(6 + (comboBox1.SelectedIndex - 6) * 4);
                }
                else if (comboBox1.SelectedIndex == 10)
                {
                    editInteger12.Value = svr_b.GetHoldValue(19);
                }

                //设备通信监测
                if (comboBox2.SelectedIndex >= 0 && comboBox2.SelectedIndex <= 9)
                {
                    displayString4.Value = svr.GetInputValue(70 + comboBox2.SelectedIndex).ToString();//时间间隔
                    displayString3.Value = svr.GetInputValue(80 + comboBox2.SelectedIndex).ToString();//失败次数
                    displayString15.Value = svr.GetInputValue(90 + comboBox2.SelectedIndex).ToString();//成功次数
                }
                else if (comboBox2.SelectedIndex == 10 )
                {
                    displayString4.Value = svr_b.GetInputValue(1).ToString();//深度时间间隔
                }
                else if (comboBox2.SelectedIndex == 11)
                {
                    displayString4.Value = svr_b.GetInputValue(5).ToString();//姿态时间间隔
                }
                else if (comboBox2.SelectedIndex >= 12 && comboBox2.SelectedIndex <= 14)//失败和成功次数
                {                   
                    displayString3.Value = svr_b.GetInputValue(19 + (comboBox2.SelectedIndex - 12) * 10).ToString();
                    displayString15.Value = svr_b.GetInputValue(18 + (comboBox2.SelectedIndex - 12) * 10).ToString();
                }
                else if (comboBox2.SelectedIndex == 15)//模拟输出板
                {
                    displayString4.Value = svr.GetInputValue(44).ToString();//时间间隔
                    displayString3.Value = svr.GetInputValue(48).ToString();//失败次数
                    displayString15.Value = svr.GetInputValue(49).ToString();//成功次数
                }

                thermometer1.Value = 0.2432 * svr.GetInputValue(37) - 249.541;//五路温度
                thermometer2.Value = 0.2432 * svr.GetInputValue(38) - 249.541;
                thermometer3.Value = 0.2432 * svr.GetInputValue(39) - 249.541;
                thermometer4.Value = 0.2432 * svr.GetInputValue(40) - 249.541;
                thermometer5.Value = 0.2432 * svr.GetInputValue(41) - 249.541;

                displayDouble1.Value = (0.2432 * svr.GetInputValue(37) - 249.541).ToString("f1");
                displayDouble2.Value = (0.2432 * svr.GetInputValue(38) - 249.541).ToString("f1");
                displayDouble3.Value = (0.2432 * svr.GetInputValue(39) - 249.541).ToString("f1");
                displayDouble4.Value = (0.2432 * svr.GetInputValue(40) - 249.541).ToString("f1");
                displayDouble5.Value = (0.2432 * svr.GetInputValue(41) - 249.541).ToString("f1");
                
                if ((svr.GetInputValue(42) & 1) == 1)//漏水状态
                {
                    led1.Indicator.ColorActive = Color.Gray;
                    led1.Indicator.Text = "开路";
                }
                else if ((svr.GetInputValue(42) & 2) == 2)
                {
                    led1.Indicator.ColorActive = Color.Blue;
                    led1.Indicator.Text = "短路";
                }
                else if ((svr.GetInputValue(42) & 4) == 4)
                {
                    led1.Indicator.ColorActive = Color.Red;
                    led1.Indicator.Text = "漏水";
                }
                else
                {
                    led1.Indicator.ColorActive = Color.Lime;
                    led1.Indicator.Text = "";
                }
                if ((svr.GetInputValue(43) & 1) == 1)
                {
                    led2.Indicator.ColorActive = Color.Gray;
                    led2.Indicator.Text = "开路";
                }
                else if ((svr.GetInputValue(43) & 2) == 2)
                {
                    led2.Indicator.ColorActive = Color.Blue;
                    led2.Indicator.Text = "短路";
                }
                else if ((svr.GetInputValue(43) & 4) == 4)
                {
                    led2.Indicator.ColorActive = Color.Red;
                    led2.Indicator.Text = "漏水";
                }
                else
                {
                    led2.Indicator.ColorActive = Color.Lime;
                    led2.Indicator.Text = "";
                }
                if ((svr.GetInputValue(44) & 1) == 1)
                {
                    led3.Indicator.ColorActive = Color.Gray;
                    led3.Indicator.Text = "开路";
                }
                else if ((svr.GetInputValue(44) & 2) == 2)
                {
                    led3.Indicator.ColorActive = Color.Blue;
                    led3.Indicator.Text = "短路";
                }
                else if ((svr.GetInputValue(44) & 4) == 4)
                {
                    led3.Indicator.ColorActive = Color.Red;
                    led3.Indicator.Text = "漏水";
                }
                else
                {
                    led3.Indicator.ColorActive = Color.Lime;
                    led3.Indicator.Text = "";
                }
                if ((svr.GetInputValue(45) & 1) == 1)
                {
                    led4.Indicator.ColorActive = Color.Gray;
                    led4.Indicator.Text = "开路";
                }
                else if ((svr.GetInputValue(45) & 2) == 2)
                {
                    led4.Indicator.ColorActive = Color.Blue;
                    led4.Indicator.Text = "短路";
                }
                else if ((svr.GetInputValue(45) & 4) == 4)
                {
                    led4.Indicator.ColorActive = Color.Red;
                    led4.Indicator.Text = "漏水";
                }
                else
                {
                    led4.Indicator.ColorActive = Color.Lime;
                    led4.Indicator.Text = "";
                }
                if ((svr.GetInputValue(46) & 1) == 1)
                {
                    led5.Indicator.ColorActive = Color.Gray;
                    led5.Indicator.Text = "开路";
                }
                else if ((svr.GetInputValue(46) & 2) == 2)
                {
                    led5.Indicator.ColorActive = Color.Blue;
                    led5.Indicator.Text = "短路";
                }
                else if ((svr.GetInputValue(46) & 4) == 4)
                {
                    led5.Indicator.ColorActive = Color.Red;
                    led5.Indicator.Text = "漏水";
                }
                else
                {
                    led5.Indicator.ColorActive = Color.Lime;
                    led5.Indicator.Text = "";
                }
                if ((svr.GetInputValue(47) & 1) == 1)
                {
                    led6.Indicator.ColorActive = Color.Gray;
                    led6.Indicator.Text = "开路";
                }
                else if ((svr.GetInputValue(47) & 2) == 2)
                {
                    led6.Indicator.ColorActive = Color.Blue;
                    led6.Indicator.Text = "短路";
                }
                else if ((svr.GetInputValue(47) & 4) == 4)
                {
                    led6.Indicator.ColorActive = Color.Red;
                    led6.Indicator.Text = "漏水";
                }
                else
                {
                    led6.Indicator.ColorActive = Color.Lime;
                    led6.Indicator.Text = "";
                }
                if ((svr.GetInputValue(48) & 1) == 1)
                {
                    led7.Indicator.ColorActive = Color.Gray;
                    led7.Indicator.Text = "开路";
                }
                else if ((svr.GetInputValue(48) & 2) == 2)
                {
                    led7.Indicator.ColorActive = Color.Blue;
                    led7.Indicator.Text = "短路";
                }
                else if ((svr.GetInputValue(48) & 4) == 4)
                {
                    led7.Indicator.ColorActive = Color.Red;
                    led7.Indicator.Text = "漏水";
                }
                else
                {
                    led7.Indicator.ColorActive = Color.Lime;
                    led7.Indicator.Text = "";
                }
                gaugeAngular31.Value = svr.GetInputValue(50);//云台角度
                gaugeAngular32.Value = svr.GetInputValue(51);
                gaugeAngular33.Value = svr.GetInputValue(52);
                gaugeAngular34.Value = svr.GetInputValue(53);               
            }

           if (tabForm.SelectedIndex == 5)
           {                            
               //下位机1原始数据，前89个
               int Index = comboBox_debug.SelectedIndex;
               string[] str = new string[100];
               for (int i = 0; i < 40; i++)
               {
                   str[i] = string.Format("{0:D3}原数据：", Index * 40 + i);
               }
               displayString_d0.Value = str[0] + svr.GetInputValue(Index * 40 + 0).ToString();
               displayString_d1.Value = str[1] + svr.GetInputValue(Index * 40 + 1).ToString();
               displayString_d2.Value = str[2] + svr.GetInputValue(Index * 40 + 2).ToString();
               displayString_d3.Value = str[3] + svr.GetInputValue(Index * 40 + 3).ToString();
               displayString_d4.Value = str[4] + svr.GetInputValue(Index * 40 + 4).ToString();
               displayString_d5.Value = str[5] + svr.GetInputValue(Index * 40 + 5).ToString();
               displayString_d6.Value = str[6] + svr.GetInputValue(Index * 40 + 6).ToString();
               displayString_d7.Value = str[7] + svr.GetInputValue(Index * 40 + 7).ToString();
               displayString_d8.Value = str[8] + svr.GetInputValue(Index * 40 + 8).ToString();
               displayString_d9.Value = str[9] + svr.GetInputValue(Index * 40 + 9).ToString();
               if (Index != 2)
               {
                   displayString_d10.Value = str[10] + svr.GetInputValue(Index * 40 + 10).ToString();
                   displayString_d11.Value = str[11] + svr.GetInputValue(Index * 40 + 11).ToString();
                   displayString_d12.Value = str[12] + svr.GetInputValue(Index * 40 + 12).ToString();
                   displayString_d13.Value = str[13] + svr.GetInputValue(Index * 40 + 13).ToString();
                   displayString_d14.Value = str[14] + svr.GetInputValue(Index * 40 + 14).ToString();
                   displayString_d15.Value = str[15] + svr.GetInputValue(Index * 40 + 15).ToString();
                   displayString_d16.Value = str[16] + svr.GetInputValue(Index * 40 + 16).ToString();
                   displayString_d17.Value = str[17] + svr.GetInputValue(Index * 40 + 17).ToString();
                   displayString_d18.Value = str[18] + svr.GetInputValue(Index * 40 + 18).ToString();
                   displayString_d19.Value = str[19] + svr.GetInputValue(Index * 40 + 19).ToString();
                   displayString_d20.Value = str[20] + svr.GetInputValue(Index * 40 + 20).ToString();
                   displayString_d21.Value = str[21] + svr.GetInputValue(Index * 40 + 21).ToString();
                   displayString_d22.Value = str[22] + svr.GetInputValue(Index * 40 + 22).ToString();
                   displayString_d23.Value = str[23] + svr.GetInputValue(Index * 40 + 23).ToString();
                   displayString_d24.Value = str[24] + svr.GetInputValue(Index * 40 + 24).ToString();
                   displayString_d25.Value = str[25] + svr.GetInputValue(Index * 40 + 25).ToString();
                   displayString_d26.Value = str[26] + svr.GetInputValue(Index * 40 + 26).ToString();
                   displayString_d27.Value = str[27] + svr.GetInputValue(Index * 40 + 27).ToString();
                   displayString_d28.Value = str[28] + svr.GetInputValue(Index * 40 + 28).ToString();
                   displayString_d29.Value = str[29] + svr.GetInputValue(Index * 40 + 29).ToString();
                   displayString_d30.Value = str[30] + svr.GetInputValue(Index * 40 + 30).ToString();
                   displayString_d31.Value = str[31] + svr.GetInputValue(Index * 40 + 31).ToString();
                   displayString_d32.Value = str[32] + svr.GetInputValue(Index * 40 + 32).ToString();
                   displayString_d33.Value = str[33] + svr.GetInputValue(Index * 40 + 33).ToString();
                   displayString_d34.Value = str[34] + svr.GetInputValue(Index * 40 + 34).ToString();
                   displayString_d35.Value = str[35] + svr.GetInputValue(Index * 40 + 35).ToString();
                   displayString_d36.Value = str[36] + svr.GetInputValue(Index * 40 + 36).ToString();
                   displayString_d37.Value = str[37] + svr.GetInputValue(Index * 40 + 37).ToString();
                   displayString_d38.Value = str[38] + svr.GetInputValue(Index * 40 + 38).ToString();
                   displayString_d39.Value = str[39] + svr.GetInputValue(Index * 40 + 39).ToString();
               }         
           }          
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        //开闭串口开关
        private void mtBtnCom_ButtonClick(object sender, Iocomp.Classes.MatrixButtonEventArgs e)
        {
            if (svr.GetConnectStatus())//6路串口板，待协议实施
            {
                if (svr.SwitchHoldValue(80 + e.Button.RowIndex * 3 + e.Button.ColIndex) == 0)
                    e.Button.BackColor = Color.Gray;
                else
                    e.Button.BackColor = Color.Green;
            }
        }
        //远程控制盒串口
        private void switchLed29_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
             if (switchLed29.Value)
            {
                if (!serialPort1.IsOpen)//如果串口没开
                {
                    serialPort1.PortName = "COM9"/*comboBox1.Text*/;
                    serialPort1.BaudRate = 38400/*Convert.ToInt32(comboBox2.Text, 10)*/;
                    serialPort1.StopBits = StopBits.One;//内定
                    //设置数据位
                    serialPort1.DataBits = 8/*Convert.ToInt32(comboBox4.Text.Trim())*/;
                    serialPort1.Parity = Parity.None;//
                    try
                    {
                        serialPort1.Open();//打开串口                       
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
               
               }
            else
                serialPort1.Close();
        }
        
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Port1_DataReceived(serialPort1);//转出线程接收收据
        }
        //远程控制盒数据处理
        private void Port1_DataReceived(SerialPort serialPort1)
        {
            string strRcv = null;
            try
            {
                Byte[] receivedData = new Byte[/*serialPort1.BytesToRead*/21];        //创建接收字节数组  
                serialPort1.Read(receivedData, 0, receivedData.Length);         //读取数据                         
                serialPort1.DiscardInBuffer();//清空SerialPort控件的Buffer  
                if (receivedData[0] == 0xFF && receivedData[1] == 0xFF && receivedData[2] == 0xA5 && receivedData[3] == 0x40 && receivedData[20] == 0x26)
                {
                    for (int i = 0; i < receivedData.Length; i++) //窗体显示  
                    {
                        strRcv += receivedData[i].ToString("X2") + " ";  //16进制显示  
                    }
                    
                    ////操纵杆
                    //if (Math.Abs(receivedData[12] - 6) - Math.Abs(receivedData[14] - 6) >= -1)//前后作用大于左右作用
                    //{//前后行走


                    //    if ((short)(((receivedData[12] << 8) + receivedData[13]) * (16383 * 1.0f / 2816)) > 16383)
                    //    {
                    //        svr.SetHoldValue(46, 16383);
                    //        svr.SetHoldValue(47, 16383);
                    //    }
                    //    else 
                    //    {
                    //        svr.SetHoldValue(46, (short)(((receivedData[12] << 8) + receivedData[13]) * (16383 * 1.0f / 2816)));//左轮
                    //        svr.SetHoldValue(47, (short)(((receivedData[12] << 8) + receivedData[13]) * (16383 * 1.0f / 2816)));//右轮
                    //        left_encoder = (short)(((receivedData[12] << 8) + receivedData[13]) * (16383 * 1.0f / 2816));
                    //        right_encoder = (short)(((receivedData[12] << 8) + receivedData[13]) * (16383 * 1.0f / 2816));
                    //    }
                        
                    //}
                    //else
                    //{//左右
                    //    if (receivedData[14] > 6)
                    //    {
                    //        if ((short)(((receivedData[14] << 8) + receivedData[15]) * (16383 * 1.0f / 2816)) > 16383)
                    //        {
                    //            svr.SetHoldValue(47, 16383);
                    //            svr.SetHoldValue(46, 8192);
                    //        }
                    //        else
                    //        {
                    //            svr.SetHoldValue(47, (short)(((receivedData[14] << 8) + receivedData[15]) * (16383 * 1.0f / 2816)));//左转，右轮
                    //            svr.SetHoldValue(46, 8192);
                    //            left_encoder = (short)(((receivedData[14] << 8) + receivedData[15]) * (16383 * 1.0f / 2816));
                    //        }                            
                    //    }
                    //    else
                    //    {
                    //        if ((short)(16383 - ((receivedData[14] << 8) + receivedData[15]) * (16383 * 1.0f / 2816)) > 16383)
                    //        {
                    //            svr.SetHoldValue(46, 16383);
                    //            svr.SetHoldValue(47, 8192);
                    //        }
                    //        else
                    //        {
                    //            svr.SetHoldValue(46, (short)(16383 - ((receivedData[14] << 8) + receivedData[15]) * (16383 * 1.0f / 2816)));//右转，左轮
                    //            svr.SetHoldValue(47, 8192);
                    //            right_encoder = (short)(((receivedData[14] << 8) + receivedData[15]) * (16383 * 1.0f / 2816));
                    //        }
                            
                    //    }
                    //}

                    //11112222和三个BC直接下发寄存器
                    svr.SetHoldValue(60, receivedData[4]);//如果下一次控制盒里对应的数据不变，则Empty()中数据相等，不进行写入
                    svr.SetHoldValue(61, receivedData[5]);
                    svr.SetHoldValue(62, receivedData[6]);
                    svr.SetHoldValue(63, receivedData[7]);
                    svr.SetHoldValue(64, receivedData[16]);
                    svr.SetHoldValue(65, receivedData[17]);
                    svr.SetHoldValue(66, receivedData[18]);

                    if (flag_setvalue == 0)//设定值来源界面，不下发
                    {
                    
                    }
                    else//设定值来源控制盒，向下位机发送设定值并显示到界面
                    {
                        svr_b.SetHoldValue(33, (short)((receivedData[12] << 8) + receivedData[13]));//控制盒数据5555，左轮设定速度
                        svr_b.SetHoldValue(43, (short)((receivedData[14] << 8) + receivedData[15]));//控制盒数据6666，右轮设定速度
                        textBox29.Text = ((receivedData[12] << 8) + receivedData[13]).ToString();
                        textBox36.Text = ((receivedData[14] << 8) + receivedData[15]).ToString();
                    }

                    //在线程里以安全方式调用控件，C#中禁止跨线程直接访问控件
                    if (textBox1.InvokeRequired)//不是在创建此控件的线程返回true
                    {
                        Action<string> actionDelegate = (x) =>
                        {
                            textBox1.Text = x.ToString();//访问控件
                        };
                        textBox1.Invoke(actionDelegate, strRcv);
                    }
                    else
                    {
                        textBox1.Text = strRcv;
                    }
                }
            }
                
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        //灯1
        private void switchLed2_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(38, (short)(e.ValueNew ? 1 : 0));
        }
        //灯2
        private void switchLed3_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(39, (short)(e.ValueNew ? 1 : 0));
        }
        //深度计
        private void switchLed4_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(42, (short)(e.ValueNew ? 1 : 0));
        }
        //声呐
        private void switchLed30_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(41, (short)(e.ValueNew ? 1 : 0));
        }
        //网摄像机1
        private void switchLed5_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(32, (short)(e.ValueNew ? 1 : 0));
        }
        //网摄像机2
        private void switchLed6_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(33, (short)(e.ValueNew ? 1 : 0));
        }
        //网摄像机3
        private void switchLed7_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(34, (short)(e.ValueNew ? 1 : 0));
        }
        //模摄像机1
        private void switchLed8_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(40, (short)(e.ValueNew ? 1 : 0));
        }
        //模摄像机2
        private void switchLed9_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(49, (short)(e.ValueNew ? 1 : 0));
        }
        //编码器1
        private void switchLed10_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(35, (short)(e.ValueNew ? 1 : 0));
        }
        //编码器2
        private void switchLed11_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(36, (short)(e.ValueNew ? 1 : 0));
        }
        //编码器3
        private void switchLed12_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(37, (short)(e.ValueNew ? 1 : 0));
        }
        //第二路放大器
        private void swJBox_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(45, (short)(e.ValueNew ? 1 : 0));
        }
        //声学探测器
        private void swPowAli_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(44, (short)(e.ValueNew ? 1 : 0));
        }
        //第一路放大器
        private void switchLed13_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(43, (short)(e.ValueNew ? 1 : 0));
        }
        //第二路备用
        private void switchLed14_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(46, (short)(e.ValueNew ? 1 : 0));
        }
        //电磁阀1
        private void switchLed15_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(50, (short)(e.ValueNew ? 1 : 0));
        }
        //电磁阀2
        private void switchLed16_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(51, (short)(e.ValueNew ? 1 : 0));
        }

        private void switchLed17_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(52, (short)(e.ValueNew ? 1 : 0));
        }

        private void switchLed18_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(53, (short)(e.ValueNew ? 1 : 0));
        }
        //电磁阀5
        private void switchLed19_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(54, (short)(e.ValueNew ? 1 : 0));
        }
        //sbq传感器
        private void switchLed20_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(55, (short)(e.ValueNew ? 1 : 0));
        }
        //第三路备用24V
        private void switchLed21_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(58, (short)(e.ValueNew ? 1 : 0));
        }
        //第三路放大器
        private void switchLed22_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(47, (short)(e.ValueNew ? 1 : 0));
        }
        //第四路放大器
        private void switchLed23_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(48, (short)(e.ValueNew ? 1 : 0));
        }
        //灯3
        private void switchLed24_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(56, (short)(e.ValueNew ? 1 : 0));
        }
        //灯4
        private void switchLed25_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr.SetHoldValue(57, (short)(e.ValueNew ? 1 : 0));
        }
        //云台，就是电源板
        private void switchLed26_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            
        }
        //第八路24V，其实就等于电源板通电
        private void switchLed27_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            
        }
        //第九路24V，就是电源板
        private void switchLed28_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            
        }
        //子窗体开关控制
        private void switchRocker11_ValueChanged(object sender, ValueBooleanEventArgs e)//状态只要改变就进入，即使不按按钮，比如初始化改变状态也会进入再操作一次
        {
            if (switchRocker11.Value)
                svr.SetHoldValue(20, 1);//电源板1#-1控制
            else
                svr.SetHoldValue(20, 0);
        }

        private void switchRocker1_ValueChanged_1(object sender, ValueBooleanEventArgs e)
        {
            if (switchRocker1.Value)
                svr.SetHoldValue(21, 1);//电源板1#-2控制
            else
                svr.SetHoldValue(21, 0);
        }

        private void switchRocker2_ValueChanged_1(object sender, ValueBooleanEventArgs e)
        {
            if (switchRocker2.Value)
                svr.SetHoldValue(22, 1);//电源板2#-1控制
            else
                svr.SetHoldValue(22, 0);
        }

        private void switchRocker3_ValueChanged_1(object sender, ValueBooleanEventArgs e)
        {
            if (switchRocker3.Value)
                svr.SetHoldValue(23, 1);//电源板2#-2控制
            else
                svr.SetHoldValue(23, 0);
        }

        private void switchRocker4_ValueChanged_1(object sender, ValueBooleanEventArgs e)
        {
            if (switchRocker4.Value)
                svr.SetHoldValue(24, 1);//电源板3#-1控制
            else
                svr.SetHoldValue(24, 0);
        }

        private void switchRocker5_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            if (switchRocker5.Value)
                svr.SetHoldValue(25, 1);//电源板3#-2控制
            else
                svr.SetHoldValue(25, 0);
        }

        private void switchRocker8_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            if (switchRocker8.Value)
                svr.SetHoldValue(28, 1);//电源板5#-1控制
            else
                svr.SetHoldValue(28, 0);
        }

        private void switchRocker10_ValueChanged_1(object sender, ValueBooleanEventArgs e)
        {
            if (switchRocker10.Value)
                svr.SetHoldValue(30, 1);//电源板6#-1控制
            else
                svr.SetHoldValue(30, 0);
        }

        private void switchRocker12_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            if (switchRocker12.Value)
                svr.SetHoldValue(31, 1);//电源板6#-2控制
            else
                svr.SetHoldValue(31, 0);
        }

        private void slider3_ValueChanged(object sender, ValueDoubleEventArgs e)
        {
            label36.Text = ((int)e.ValueNew).ToString();
            svr_b.SetHoldValue(22, (short)slider3.Value);//动臂
            slider10.Value = slider3.Value;
        }

        private void slider4_ValueChanged(object sender, ValueDoubleEventArgs e)
        {
            label44.Text = ((int)e.ValueNew).ToString();
            svr_b.SetHoldValue(23, (short)slider4.Value);//破碎头
        }

        private void timer2_Tick(object sender, EventArgs e)
        {

            //发送COM1数据
            if (serialPort1.IsOpen)
            {//如果串口开启               
                Byte[] send = new Byte[8];
                send[0] = 0xFF;
                send[1] = 0xFF;
                send[2] = 0xA5;
                send[3] = 0x40;
                send[4] = 0x01;
                send[5] = 0x05;
                send[6] = 0x44;
                send[7] = 0x26;
                serialPort1.Write(send, 0, 8);//写数据               
            }
            else
            {
                //return;
                //MessageBox.Show("串口未打开");
            }
     
        }

        private void slider1_ValueChanged(object sender, ValueDoubleEventArgs e)
        {
            label145.Text = ((int)e.ValueNew).ToString();
            svr_b.SetHoldValue(20, (short)slider1.Value);//左马达
        }

        private void slider2_ValueChanged(object sender, ValueDoubleEventArgs e)
        {
            label146.Text = ((int)e.ValueNew).ToString();
            svr_b.SetHoldValue(21, (short)slider2.Value);//右马达
            slider9.Value = slider2.Value;
        }
        //——————————————————————————————————地址2————————————————————————————
        //——————————————————————————————————地址2————————————————————————————
        private void switchLed32_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            svr_b.SetHoldValue(18, (short)(e.ValueNew ? 1 : 0));
        }
        //——————————————————————————————————Modbus RTU————————————————————————————
        //——————————————————————————————————Modbus RTU————————————————————————————
        private void switchLed33_ValueChanged(object sender, ValueBooleanEventArgs e)
        {
            if (switchLed33.Value)
            {             
                svr_rtu.Start(cbPort.SelectedItem.ToString());//RTU通信，选择串口号
            }
            else
                svr_rtu.Stop();
        }

        //——————————————————————————————————下位机数据设置处理————————————————————————————
        //—————————————————————————————————————————————————————————————————
        private void button12_Click(object sender, EventArgs e)
        {
            if (comboBox4.SelectedIndex == 0)//多串口板1（下位机1）
            {              
                svr.SetHoldValue(0, (short)editInteger2.Value);//站地址
                svr.SetHoldValue(1, (short)(editInteger1.Value / 100));//波特率
                svr.SetHoldValue(2, (short)editInteger8.Value);//设置启动次数
            }
            else if (comboBox4.SelectedIndex == 5)//多串口板2（下位机2）
            {
                svr_b.SetHoldValue(0, (short)editInteger2.Value);
                svr_b.SetHoldValue(1, (short)(editInteger1.Value / 100));
                svr_b.SetHoldValue(2, (short)editInteger8.Value);
            }
            else if (comboBox4.SelectedIndex <= 4 && comboBox4.SelectedIndex >= 1)
            {
                svr.SetHoldValue(3 + (comboBox4.SelectedIndex - 1) * 4, (short)(editInteger1.Value / 100));
                svr.SetHoldValue(4 + (comboBox4.SelectedIndex - 1) * 4, (short)editInteger2.Value);
                svr.SetHoldValue(5 + (comboBox4.SelectedIndex - 1) * 4, (short)editInteger3.Value);
                svr.SetHoldValue(6 + (comboBox4.SelectedIndex - 1) * 4, (short)editInteger4.Value);
            }
            else if (comboBox4.SelectedIndex <= 9 && comboBox4.SelectedIndex >= 6)
            {
                svr_b.SetHoldValue(3 + (comboBox4.SelectedIndex - 6) * 4, (short)(editInteger1.Value / 100));
                svr_b.SetHoldValue(4 + (comboBox4.SelectedIndex - 6) * 4, (short)editInteger2.Value);
                svr_b.SetHoldValue(5 + (comboBox4.SelectedIndex - 6) * 4, (short)editInteger3.Value);
                svr_b.SetHoldValue(6 + (comboBox4.SelectedIndex - 6) * 4, (short)editInteger4.Value);
            }
            else if (comboBox4.SelectedIndex == 10)
            {
                svr_b.SetHoldValue(19, (short)(editInteger1.Value / 100));               
            }

        }
        //调节器1设置
        short slider13_value, slider14_value;
        short hold138_value;
        private void button5_Click(object sender, EventArgs e)
        {
            svr_b.SetHoldValue(30, short.Parse(textBox28.Text));//输入输出变量地址
            svr_b.SetHoldValue(31, short.Parse(textBox33.Text));
            svr_b.SetHoldValue(33, short.Parse(textBox29.Text));//PID参数
            svr_b.SetHoldValue(34, short.Parse(textBox32.Text));
            svr_b.SetHoldValue(35, short.Parse(textBox31.Text));
            svr_b.SetHoldValue(36, short.Parse(textBox30.Text));
            if(radioButton13.Checked)//工作模式
            {
                svr_b.SetHoldValue(39, 0);
            }
            else if (radioButton15.Checked)
            {
                svr_b.SetHoldValue(39, 1);
            }
            else if (radioButton16.Checked)
            {
                svr_b.SetHoldValue(39, 2);
            }
            else if (radioButton14.Checked)
            {
                svr_b.SetHoldValue(39, 3);
            }
            svr_b.SetHoldValue(32, slider13_value);//输入缩放
            svr_b.SetHoldValue(37, slider14_value);
            svr_b.SetHoldValue(38, hold138_value);//作用方式
        }
        //调节器2设置
        short slider15_value, slider16_value;
        short hold148_value;
        private void button6_Click(object sender, EventArgs e)
        {
            svr_b.SetHoldValue(40, short.Parse(textBox35.Text));//输入输出变量地址
            svr_b.SetHoldValue(41, short.Parse(textBox40.Text));
            svr_b.SetHoldValue(43, short.Parse(textBox36.Text));//PID参数
            svr_b.SetHoldValue(44, short.Parse(textBox39.Text));
            svr_b.SetHoldValue(45, short.Parse(textBox38.Text));
            svr_b.SetHoldValue(46, short.Parse(textBox37.Text));
            if (radioButton17.Checked)//工作模式
            {
                svr_b.SetHoldValue(49, 0);
            }
            else if (radioButton19.Checked)
            {
                svr_b.SetHoldValue(49, 1);
            }
            else if (radioButton20.Checked)
            {
                svr_b.SetHoldValue(49, 2);
            }
            else if (radioButton18.Checked)
            {
                svr_b.SetHoldValue(49, 3);
            }
            svr_b.SetHoldValue(42, slider15_value);//输入缩放
            svr_b.SetHoldValue(47, slider16_value);
            svr_b.SetHoldValue(48, hold148_value);//作用方式
        }
        //设定值来源切换
        int flag_setvalue;
        private void button3_Click(object sender, EventArgs e)
        {
            if (button3.Text == "设定值来源界面")
            {
                //控制盒的数据向下位机发送并显示到界面设定值中              
                flag_setvalue = 1;
                button3.Text = "设定值来源控制盒";
            }
            else
            {
                //屏蔽控制盒指令向下位机发送设定值5555左轮速度，6666右轮速度，指令仅由界面提供
                flag_setvalue = 0;
                button3.Text = "设定值来源界面";
            }

        }
        //行走上电
        private void button4_Click(object sender, EventArgs e)
        {
            svr.SetHoldValue(45, 1);//放大器2
            svr.SetHoldValue(47, 1);
            svr.SetHoldValue(53, 1);//电磁阀4
            svr.SetHoldValue(54, 0);
        }
        //输入缩放
        private void slider13_ValueChanged(object sender, ValueDoubleEventArgs e)
        {
            //label36.Text = ((short)e.ValueNew).ToString();
            slider13_value = (short)slider13.Value;      
        }
        //输出限幅
        private void slider14_ValueChanged(object sender, ValueDoubleEventArgs e)
        {
            slider14_value = (short)slider14.Value; 
        }
        
        //调节器1正反作用
        private void switchRotary4_ValueChanged(object sender, ValueIntegerEventArgs e)
        {
            switch (e.ValueNew)
            {
                case 0://正作用
                    hold138_value = 0;                    
                    break;
                case 1://反作用
                    hold138_value = 1;
                    break;              
            }
        }
        //调节器2正反作用
        private void switchRotary5_ValueChanged(object sender, ValueIntegerEventArgs e)
        {
            switch (e.ValueNew)
            {
                case 0://正作用
                    hold148_value = 0; 
                    break;
                case 1://反作用
                    hold148_value = 1; 
                    break;
            }
        }

        private void slider15_ValueChanged(object sender, ValueDoubleEventArgs e)
        {
            slider15_value = (short)slider15.Value;     
        }

        private void slider16_ValueChanged(object sender, ValueDoubleEventArgs e)
        {
            slider16_value = (short)slider16.Value;    
        }

        private void slider9_ValueChanged(object sender, ValueDoubleEventArgs e)
        {            
            svr_b.SetHoldValue(21, (short)slider9.Value);//DA#2
            displayDouble12.Value.AsInteger = slider9.Value;
            slider2.Value = slider9.Value;
        }

        private void slider10_ValueChanged(object sender, ValueDoubleEventArgs e)
        {
            svr_b.SetHoldValue(22, (short)slider10.Value);//DA#3
            displayDouble13.Value.AsInteger = slider10.Value;
            slider3.Value = slider10.Value;
        }
        //动臂（调节器3）
        short slider11_value, slider12_value;
        short hold158_value;
        private void button10_Click(object sender, EventArgs e)
        {
            svr_b.SetHoldValue(50, short.Parse(textBox16.Text));//输入输出变量地址
            svr_b.SetHoldValue(51, short.Parse(textBox21.Text));
            svr_b.SetHoldValue(53, short.Parse(textBox17.Text));//PID参数
            svr_b.SetHoldValue(54, short.Parse(textBox20.Text));
            svr_b.SetHoldValue(55, short.Parse(textBox19.Text));
            svr_b.SetHoldValue(56, short.Parse(textBox18.Text));
            if (radioButton9.Checked)//工作模式
            {
                svr_b.SetHoldValue(59, 0);
            }
            else if (radioButton11.Checked)
            {
                svr_b.SetHoldValue(59, 1);
            }
            else if (radioButton12.Checked)
            {
                svr_b.SetHoldValue(59, 2);
            }
            else if (radioButton10.Checked)
            {
                svr_b.SetHoldValue(59, 3);
            }
            svr_b.SetHoldValue(52, slider11_value);//输入缩放
            svr_b.SetHoldValue(57, slider12_value);
            svr_b.SetHoldValue(58, hold158_value);//作用方式
        }

        private void button11_Click(object sender, EventArgs e)
        {
            svr.SetHoldValue(45, 1);//放大器2
            svr.SetHoldValue(47, 1);
            svr.SetHoldValue(53, 0);//电磁阀4
            svr.SetHoldValue(54, 1);   
        }
        //动臂
        private void button7_Click(object sender, EventArgs e)
        {
            svr.SetHoldValue(43, 1);//放大器1
        }
        //破碎头
        private void button9_Click(object sender, EventArgs e)
        {
            svr.SetHoldValue(48, 1);//放大器4
        }

        private void slider11_ValueChanged(object sender, ValueDoubleEventArgs e)
        {
            slider11_value = (short)slider11.Value;
        }

        private void slider12_ValueChanged(object sender, ValueDoubleEventArgs e)
        {
            slider12_value = (short)slider12.Value; 
        }
        //调节器3正反作用
        private void switchRotary3_ValueChanged(object sender, ValueIntegerEventArgs e)
        {
            switch (e.ValueNew)
            {
                case 0://正作用
                    hold158_value = 0;
                    break;
                case 1://反作用
                    hold158_value = 1;
                    break;
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            svr.GetCycleTime((int)numericUpDown1.Value);
            svr_b.GetCycleTime((int)numericUpDown1.Value);
        }

        

        

      

    }
}
