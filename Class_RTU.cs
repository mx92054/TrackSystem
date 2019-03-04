using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SQLite;
//using WSMBT;//取消注释，用于Modbus TCP
using WSMBS;//Modbus master，用于C＃，VB和托管C ++的Modbus主控.NET控件。支持Modbus RTU / ASCII    

namespace InsituSystem
{
    class Class_RTU
    {
        public const short maxInputAddr = 100;

        //public WSMBTControl svr = new WSMBTControl();//取消注释
        public WSMBSControl svr = new WSMBSControl();
        public short[] inputReg = new short[maxInputAddr];
        public string strIPAdr;//服务器IP地址，RTU时则为端口号
        public int nTx = 0;//读输入寄存器成功次数

        public bool bConnected = false;//仪器连接状态       
        public SqLiteHelper sql;
    }

    //=======================Worker thread==============================================================
    class SvrFactory_RTU
    {
        private Class_RTU cntl = new Class_RTU();

        private Thread thread;
        private static AutoResetEvent startEvent;//常常被用来在两个线程之间进行信号发送。线程可以通过调用AutoResetEvent对象的WaitOne()方法进入等待状态，然后另外一个线程通过调用AutoResetEvent对象的Set()方法取消等待的状态。
        private static AutoResetEvent stopEvent;
        private System.Threading.Timer tmr;

        //--------------------初始化函数----------------------------------------------------------
        public SvrFactory_RTU()
        {
            thread = new Thread(new ParameterizedThreadStart(ServerTask));//通信服务线程
            thread.IsBackground = true;//后台线程。当前台线程结束，所有后台线程都会被停止
            startEvent = new AutoResetEvent(false);
            stopEvent = new AutoResetEvent(false);
            thread.Start(cntl);
            InitDBF();
            //定时器记录数据
            tmr = new System.Threading.Timer(RecData, cntl, 0, 1000);//第一个参数是：回调方法，表示要定时执行的方法，第二个参数是：回调方法要使用的信息的对象，或者为空引用，第三个参数是：调用 callback 之前延迟的时间量（以毫秒为单位）。
        }

        //----------------初始化数据库-------------------------------------------------------------------
        public void InitDBF()
        {
            try
            {
                cntl.sql = new SqLiteHelper("InsituData_Deep0128.db");
                cntl.sql.CreateTable("Record", new string[] { "File", "Date", "Type" }, new string[] { "TEXT", "DATETIME", "TEXT" });//创建表名，数据名称，数据类型
                cntl.sql.CreateTable("DCBoard1", new string[] { "time", "Vol1", "Amp1", "Ins1", "Vol2", "Amp2", "Ins2", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" });
                cntl.sql.CreateTable("DCBoard2", new string[] { "time", "Vol1", "Amp1", "Ins1", "Vol2", "Amp2", "Ins2", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" });
                cntl.sql.CreateTable("DCBoard3", new string[] { "time", "Vol1", "Amp1", "Ins1", "Vol2", "Amp2", "Ins2", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" });
                cntl.sql.CreateTable("DCBoard4", new string[] { "time", "Vol1", "Amp1", "Ins1", "Vol2", "Amp2", "Ins2", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" });
                cntl.sql.CreateTable("DCBoard5", new string[] { "time", "Vol1", "Amp1", "Ins1", "Vol2", "Amp2", "Ins2", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" });
                cntl.sql.CreateTable("DCBoard6", new string[] { "time", "Vol1", "Amp1", "Ins1", "Vol2", "Amp2", "Ins2", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" });

                cntl.sql.CreateTable("System", new string[] { "time", "Addr_36", "Addr_37", "Addr_38", "Addr_39", "Addr_40", "Addr_41", "Addr_42", "Addr_43", "Addr_44", "Addr_45", "Addr_46", "Addr_47", "Addr_48", "Addr_49", "Addr_50", "Addr_51", "Addr_52", "Addr_53", "Addr_54", },
                    new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" });

                cntl.sql.CreateTable("Encoder1", new string[] { "time", "Addr_55", "Addr_56", "Addr_57", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER" });
                cntl.sql.CreateTable("Encoder2", new string[] { "time", "Addr_60", "Addr_61", "Addr_62", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER" });
                cntl.sql.CreateTable("Encoder3", new string[] { "time", "Addr_65", "Addr_66", "Addr_67", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER" });
                cntl.sql.CreateTable("Pan_Tilt", new string[] { "time", "Addr_70", "Addr_71", "Addr_72", "Addr_73", "H_Addr_46", "H_Addr_47", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" });

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //----------------数据定时记录-------------------------------------------------------------------
        public static void RecData(object ob)
        {
            Class_RTU ctl = (Class_RTU)ob;
            if (!ctl.bConnected)
                return;

            //-----------------DC DC record---------------------------------
            SQLiteTransaction trans = ctl.sql.dbConnection.BeginTransaction();//启动一个事务处理
            SQLiteCommand dcmd = ctl.sql.dbConnection.CreateCommand();//sql为SqLiteHelper的实例
            string str;
            try
            {
                //---------------------------六块电源板 record------------------------            
                for (int n = 0; n < 6; n++)
                {
                    str = string.Format("INSERT INTO DCBoard{0} VALUES ( '{1}','{2}','{3}','{4}','{5}','{6}','{7}')",
                        n + 1, DateTime.Now, ctl.inputReg[n * 6], ctl.inputReg[n * 6 + 1], ctl.inputReg[n * 6 + 2], ctl.inputReg[n * 6 + 3], ctl.inputReg[n * 6 + 4],
                        ctl.inputReg[n * 6 + 5]);
                    dcmd.CommandText = str;
                    dcmd.ExecuteNonQuery();//Command对象通过ExecuteNonQuery方法更新数据库
                }
                //---------------------------系统 record------------------------     

                str = string.Format("INSERT INTO System VALUES ( '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}','{19}')",
                    DateTime.Now, ctl.inputReg[36], ctl.inputReg[37], ctl.inputReg[38], ctl.inputReg[39], ctl.inputReg[40], ctl.inputReg[41], ctl.inputReg[42], ctl.inputReg[43], ctl.inputReg[44], ctl.inputReg[45],
                    ctl.inputReg[46], ctl.inputReg[47], ctl.inputReg[48], ctl.inputReg[49], ctl.inputReg[50], ctl.inputReg[51], ctl.inputReg[52], ctl.inputReg[53], ctl.inputReg[54]);
                dcmd.CommandText = str;
                dcmd.ExecuteNonQuery();


                //---------------------------编码器 record------------------------   
                for (int n = 0; n < 3; n++)
                {
                    str = string.Format("INSERT INTO Encoder{0} VALUES ( '{1}','{2}','{3}','{4}')",
                        n + 1, DateTime.Now, ctl.inputReg[n * 5 + 55], ctl.inputReg[n * 5 + 56], ctl.inputReg[n * 5 + 57]);
                    dcmd.CommandText = str;
                    dcmd.ExecuteNonQuery();
                }
                
                trans.Commit();//commit后，便看不到事务了
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //--------------------读取输入寄存器内容----------------------------------------------------------
        public short GetInputValue(int nAddr)
        {
            if (nAddr < Class_RTU.maxInputAddr)//maxInputAddr=100
                return cntl.inputReg[nAddr];
            else
                return 0;
        }

        //--------------------读取接收成功次数（输入，读寄存器）----------------------------------------------------------
        public int GetCurTxCounter()
        {
            return cntl.nTx;
        }

        //--------------------读取仪器连接状态----------------------------------------------------------
        public bool GetConnectStatus()
        {
            return cntl.bConnected;
        }

        //--------------------启动服务函数----------------------------------------------------------
        public void Start(string str)
        {
            cntl.strIPAdr = str;
            startEvent.Set();//取消线程等待状态
        }

        //--------------------停止服务函数----------------------------------------------------------
        public void Stop()
        {
            stopEvent.Set();
        }

        //--------------------通信服务函数----------------------------------------------------------
        private static void ServerTask(object b)
        {
            int nErr = 0;
            Result result;
            bool bRunning = false;
            bool bSuccess = false;
            Class_RTU ctl = (Class_RTU)b;

            while (true)
            {
                if (startEvent.WaitOne(10))//收到Set()信号，10ms后停止等待，waitone 只是挂起当前这个线程，没有Set()信号则会一直挂起
                {
                    bRunning = true;
                    nErr = 0;
                    ctl.nTx = 0;
                    //用于Modbus RTU                 
                    ctl.svr.PortName = ctl.strIPAdr; // "COM8";
                    ctl.svr.Parity = Parity.None;
                    ctl.svr.DataBits = 8;
                    ctl.svr.StopBits = 1;
                    ctl.svr.BaudRate = 115200;
                    result = ctl.svr.Open();
                    ctl.svr.LicenseKey("2222222222222222222222222F3AA");//RTU密钥，不用密钥可用30分钟                    
                    if (result == Result.SUCCESS)
                        ctl.bConnected = true;
                    else
                        MessageBox.Show("Connect_RTU failure -" + result.ToString());
                }

                if (stopEvent.WaitOne(10))
                {
                    bRunning = false;
                    ctl.svr.Close();
                    ctl.bConnected = false;
                }

                if (bRunning && ctl.bConnected)
                {                    
                    bSuccess = true;//写入成功标志                
                    result = ctl.svr.ReadInputRegisters(1, (ushort)(0), 90, ctl.inputReg, 0);//0x04，对应服务器0x04，模拟量输入（只能读不能写，通常是状态寄存器），Read Input Registers，最大连续读125个
                    if (result != Result.SUCCESS)
                    {
                        bSuccess = false;
                        if (nErr++ > 3)//错误次数>3，则认为连接断开
                        {
                            ctl.bConnected = false;
                            MessageBox.Show("ReadInputRegisters Error :" + result.ToString());
                            break;
                        }
                    }
                    if (bSuccess) ctl.nTx++;//读输入寄存器成功次数                   
                }
            }
        }
        
    }
}
