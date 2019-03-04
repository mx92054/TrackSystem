using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SQLite;
using WSMBT;//取消注释，用于Modbus TCP
//using WSMBS;//Modbus master，用于C＃，VB和托管C ++的Modbus主控.NET控件。支持Modbus RTU / ASCII    

namespace InsituSystem
{
    class situController_b
    {
        public const short maxInputAddr = 100;
        public const short maxHoldAddr = 170;
        public const int maxCmdLen = 170;
        public WSMBTControl svr = new WSMBTControl();//取消注释
        public short[] holdReg = new short[maxHoldAddr];
        public short[] inputReg = new short[maxInputAddr];
        public string strIPAdr;//服务器IP地址，RTU时则为端口号
        public int nRx = 0;//写单个寄存器成功次数
        public int nTx = 0;//读输入寄存器成功次数
        public short[] stackAddr = new short[maxCmdLen];
        public short[] stackVal = new short[maxCmdLen];
        public int stackInptr = 0;
        public int stackOutptr = 0;
        public bool bConnected = false;//仪器连接状态
        public bool bChanged = false;//设置保持寄存器状态
        public bool bReadHoldReg = false;//读保持寄存器成功与否
        public bool bLastRecCpt = false;
        public bool bLastRecVan = false;
        public bool bRecCpt = false;
        public bool bRecVan = false;
        public int cycle_time = 100;

        public SqLiteHelper sql;
        //？？？？？
        public bool Push(short adr, short val)
        {
            if (!bConnected)
                return false;
            int cur = (stackInptr + 1) % maxCmdLen;//取余，/为整除
            if (cur == stackOutptr) // overflow
                return false;
            stackAddr[stackInptr] = adr;
            stackVal[stackInptr] = val;
            stackInptr = cur;
            return true;
        }

        public bool Pop(ref short adr, ref short val)
        {
            if (stackOutptr == stackInptr)
                return false;
            adr = stackAddr[stackOutptr];
            val = stackVal[stackOutptr];
            stackOutptr = (stackOutptr + 1) % maxCmdLen;
            return true;
        }

        public bool Empty()
        {
            return (stackOutptr == stackInptr);//判断是否相等，相等则返回true
        }
    }

    //=======================Worker thread==============================================================
    class SvrFactory_b
    {
        private situController_b cntl_b = new situController_b();

        private Thread thread;
        private static AutoResetEvent startEvent;//常常被用来在两个线程之间进行信号发送。线程可以通过调用AutoResetEvent对象的WaitOne()方法进入等待状态，然后另外一个线程通过调用AutoResetEvent对象的Set()方法取消等待的状态。
        private static AutoResetEvent stopEvent;
        private System.Threading.Timer tmr;

        //--------------------初始化函数----------------------------------------------------------
        public SvrFactory_b()
        {
            thread = new Thread(new ParameterizedThreadStart(ServerTask));//通信服务线程
            thread.IsBackground = true;//后台线程。当前台线程结束，所有后台线程都会被停止
            startEvent = new AutoResetEvent(false);
            stopEvent = new AutoResetEvent(false);
            thread.Start(cntl_b);
            InitDBF();
            //定时器记录数据
            tmr = new System.Threading.Timer(RecData, cntl_b, 0, 1000);//第一个参数是：回调方法，表示要定时执行的方法，第二个参数是：回调方法要使用的信息的对象，或者为空引用，第三个参数是：调用 callback 之前延迟的时间量（以毫秒为单位）。
        }

        //----------------初始化数据库-------------------------------------------------------------------
        public void InitDBF()
        {
            try
            {
                cntl_b.sql = new SqLiteHelper("InsituData_Deep0225.db");
                cntl_b.sql.CreateTable("Record", new string[] { "File", "Date", "Type" }, new string[] { "TEXT", "DATETIME", "TEXT" });//创建表名，数据名称，数据类型
                cntl_b.sql.CreateTable("DCBoard1", new string[] { "time", "Vol1", "Amp1", "Ins1", "Vol2", "Amp2", "Ins2", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" });
                cntl_b.sql.CreateTable("DCBoard2", new string[] { "time", "Vol1", "Amp1", "Ins1", "Vol2", "Amp2", "Ins2", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" });
                cntl_b.sql.CreateTable("DCBoard3", new string[] { "time", "Vol1", "Amp1", "Ins1", "Vol2", "Amp2", "Ins2", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" });
                cntl_b.sql.CreateTable("DCBoard4", new string[] { "time", "Vol1", "Amp1", "Ins1", "Vol2", "Amp2", "Ins2", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" });
                cntl_b.sql.CreateTable("DCBoard5", new string[] { "time", "Vol1", "Amp1", "Ins1", "Vol2", "Amp2", "Ins2", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" });
                cntl_b.sql.CreateTable("DCBoard6", new string[] { "time", "Vol1", "Amp1", "Ins1", "Vol2", "Amp2", "Ins2", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" });

                cntl_b.sql.CreateTable("System", new string[] { "time", "Addr_36", "Addr_37", "Addr_38", "Addr_39", "Addr_40", "Addr_41", "Addr_42", "Addr_43", "Addr_44", "Addr_45", "Addr_46", "Addr_47", "Addr_48", "Addr_49", "Addr_50", "Addr_51", "Addr_52", "Addr_53", "Addr_54", },
                    new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" });

                cntl_b.sql.CreateTable("Encoder1", new string[] { "time", "Addr_55", "Addr_56", "Addr_57", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER" });
                cntl_b.sql.CreateTable("Encoder2", new string[] { "time", "Addr_60", "Addr_61", "Addr_62", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER" });
                cntl_b.sql.CreateTable("Encoder3", new string[] { "time", "Addr_65", "Addr_66", "Addr_67", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER" });
                cntl_b.sql.CreateTable("Pan_Tilt", new string[] { "time", "Addr_70", "Addr_71", "Addr_72", "Addr_73", "H_Addr_46", "H_Addr_47", }, new string[] { "DATETIME", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" });

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //----------------数据定时记录-------------------------------------------------------------------
        public static void RecData(object ob)
        {
            situController_b ctl_b = (situController_b)ob;
            if (!ctl_b.bConnected)
                return;

            //-----------------DC DC record---------------------------------
            SQLiteTransaction trans = ctl_b.sql.dbConnection.BeginTransaction();//启动一个事务处理
            SQLiteCommand dcmd = ctl_b.sql.dbConnection.CreateCommand();//sql为SqLiteHelper的实例
            string str;
            try
            {
                //---------------------------六块电源板 record------------------------            
                for (int n = 0; n < 6; n++)
                {
                    str = string.Format("INSERT INTO DCBoard{0} VALUES ( '{1}','{2}','{3}','{4}','{5}','{6}','{7}')",
                        n + 1, DateTime.Now, ctl_b.inputReg[n * 6], ctl_b.inputReg[n * 6 + 1], ctl_b.inputReg[n * 6 + 2], ctl_b.inputReg[n * 6 + 3], ctl_b.inputReg[n * 6 + 4],
                        ctl_b.inputReg[n * 6 + 5]);
                    dcmd.CommandText = str;
                    dcmd.ExecuteNonQuery();//Command对象通过ExecuteNonQuery方法更新数据库
                }
                //---------------------------系统 record------------------------     
                str = string.Format("INSERT INTO System VALUES ( '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}','{19}')",
                    DateTime.Now, ctl_b.inputReg[36], ctl_b.inputReg[37], ctl_b.inputReg[38], ctl_b.inputReg[39], ctl_b.inputReg[40], ctl_b.inputReg[41], ctl_b.inputReg[42], ctl_b.inputReg[43], ctl_b.inputReg[44], ctl_b.inputReg[45],
                    ctl_b.inputReg[46], ctl_b.inputReg[47], ctl_b.inputReg[48], ctl_b.inputReg[49], ctl_b.inputReg[50], ctl_b.inputReg[51], ctl_b.inputReg[52], ctl_b.inputReg[53], ctl_b.inputReg[54]);
                dcmd.CommandText = str;
                dcmd.ExecuteNonQuery();

                //---------------------------编码器 record------------------------   
                for (int n = 0; n < 3; n++)
                {
                    str = string.Format("INSERT INTO Encoder{0} VALUES ( '{1}','{2}','{3}','{4}')",
                        n + 1, DateTime.Now, ctl_b.inputReg[n * 5 + 55], ctl_b.inputReg[n * 5 + 56], ctl_b.inputReg[n * 5 + 57]);
                    dcmd.CommandText = str;
                    dcmd.ExecuteNonQuery();
                }
                //---------------------------云台 record------------------------   
                str = string.Format("INSERT INTO Pan_Tilt VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}')",
                       DateTime.Now, ctl_b.inputReg[70], ctl_b.inputReg[71], ctl_b.inputReg[72], ctl_b.inputReg[73], ctl_b.holdReg[46], ctl_b.holdReg[47]);
                dcmd.CommandText = str;
                dcmd.ExecuteNonQuery();                

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
            if (nAddr < situController_b.maxInputAddr)//maxInputAddr=100
                return cntl_b.inputReg[nAddr];
            else
                return 0;
        }

        //--------------------读取保持寄存器内容----------------------------------------------------------
        public short GetHoldValue(int nAddr)
        {
            if (nAddr < situController_b.maxHoldAddr)
                return cntl_b.holdReg[nAddr];
            else
                return 0;
        }

        //--------------------读取发送成功次数（输出，写寄存器）----------------------------------------------------------
        public int GetCurRxCounter()//反了吧？Rx是发送，Tx是接收啊
        {
            return cntl_b.nRx;
        }

        //--------------------读取接收成功次数（输入，读寄存器）----------------------------------------------------------
        public int GetCurTxCounter()
        {
            return cntl_b.nTx;
        }

        //--------------------完成数据读取---------------------------------------------------------
        public bool CanReadHoldingReg()
        {
            if (cntl_b.bReadHoldReg)//说明读保持寄存器成功
            {
                cntl_b.bReadHoldReg = false;
                return true;
            }
            else
                return false;
        }

        //--------------------设置保持寄存器内容（界面通过操作此函数来改变保持寄存器数值）----------------------------------------------------------
        public void SetHoldValue(int nAddr, short val)//val为写单个寄存器变量
        {
            if (nAddr < situController_b.maxHoldAddr)//maxHoldAddr=100
            {
                if (cntl_b.holdReg[nAddr] != val)
                {
                    cntl_b.holdReg[nAddr] = val;
                    cntl_b.Push((short)nAddr, val);
                    cntl_b.bChanged = true;
                }
            }
        }

        //--------------------切换保持寄存器内容----------------------------------------------------------
        public short SwitchHoldValue(int nAddr)
        {
            if (nAddr < situController_b.maxHoldAddr)//maxHoldAddr=100
            {
                if (cntl_b.holdReg[nAddr] == 0)//若保持寄存器为空
                {
                    cntl_b.holdReg[nAddr] = 1;
                    cntl_b.Push((short)nAddr, 1);
                }
                else
                {
                    cntl_b.holdReg[nAddr] = 0;
                    cntl_b.Push((short)nAddr, 0);
                }

                cntl_b.bChanged = true;
                return cntl_b.holdReg[nAddr];
            }

            return 0;
        }

        //--------------------读取仪器连接状态----------------------------------------------------------
        public bool GetConnectStatus()
        {
            return cntl_b.bConnected;
        }

        public void GetCycleTime(int ctime)//不返回值，则定义void类型
        {
            cntl_b.cycle_time = ctime;
        }

        //--------------------启动服务函数----------------------------------------------------------
        public void Start(string str)
        {
            cntl_b.strIPAdr = str;
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
            //int nErr = 0;
            Result result;
            bool bRunning = false;
            bool bSuccess = false;
            bool bFirst = true;
            short adr = 0, val = 0;
            situController_b ctl_b = (situController_b)b;

            while (true)
            {
                int starttime_m = DateTime.Now.Millisecond;
                int starttime_s = DateTime.Now.Second;
                if (startEvent.WaitOne(10))//收到Set()信号，10ms后停止等待，waitone 只是挂起当前这个线程，没有Set()信号则会一直挂起
                {
                    bRunning = true;
                    bFirst = true;
                    //nErr = 0;
                    ctl_b.nRx = 0;
                    ctl_b.nTx = 0;
                    //用于Modbus TCP
                    result = ctl_b.svr.Connect(ctl_b.strIPAdr, 1025);//取消注释                                       
                    ctl_b.svr.LicenseKey("2222222222222222222222222AAF2");//TCP密钥，不用密钥可用30分钟                    
                    if (result == Result.SUCCESS)
                        ctl_b.bConnected = true;
                    else
                        MessageBox.Show("Connect_b failure -" + result.ToString());
                }

                if (stopEvent.WaitOne(10))
                {
                    bRunning = false;
                    ctl_b.svr.Close();
                    ctl_b.bConnected = false;
                }

                if (bRunning && ctl_b.bConnected)
                {
                    try
                    {
                        ctl_b.nRx++;//写入成功次数
                        bSuccess = true;//写入成功标志

                        while (!ctl_b.Empty())//不相等才进行写入。ref是有进有出，out是只出不进。
                        {
                            ctl_b.Pop(ref adr, ref val);//ref 关键字使参数按引用传递
                            result = ctl_b.svr.WriteSingleRegister(1, (ushort)(100 + adr), val);//0x06写单个寄存器
                            if (result != Result.SUCCESS)
                            {
                                ctl_b.Push(adr, val);
                            }
                        }
                        result = ctl_b.svr.ReadInputRegisters(1, (ushort)(0), 100, ctl_b.inputReg, 0);//0x04，对应服务器0x04，模拟量输入（只能读不能写，通常是状态寄存器），Read Input Registers，最大连续读125个
                        if (result != Result.SUCCESS)
                        {
                            //bSuccess = false;
                            //if (nErr++ > 3)//错误次数>3，则认为连接断开
                            //{
                            //    ctl_b.bConnected = false;
                            //    MessageBox.Show("Read InputRegisters Error :" + result.ToString());
                            //    break;
                            //}
                        }

                        if (bSuccess) ctl_b.nTx++;//读输入寄存器成功次数

                        if (bFirst)//控制寄存器，成功读取一次即可
                        {
                            result = ctl_b.svr.ReadHoldingRegisters(1, 100, 70, ctl_b.holdReg);//0x03，对应服务器0x03，读保持寄存器（可读可写，通常是功能控制寄存器，有些会掉电保持）
                            if (result == Result.SUCCESS)
                            {
                                bFirst = false;
                                ctl_b.bReadHoldReg = true;
                            }
                            else
                                MessageBox.Show("Read_b Holding Error :" + result.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Alarm");
                    }
                    //定周期
                    if (DateTime.Now.Second - starttime_s >= 0)
                    {
                        int spendtime = (DateTime.Now.Second - starttime_s) * 1000 + DateTime.Now.Millisecond - starttime_m;
                        while (Math.Abs(spendtime) < ctl_b.cycle_time)
                        {
                            Thread.Sleep(1);
                            spendtime = (DateTime.Now.Second - starttime_s) * 1000 + DateTime.Now.Millisecond - starttime_m;
                        }
                    }

                }             
            }
        }

        //--------------------------------------读取时间段内各种类型的数据个数--------------------------------------------------------------
        public SQLiteDataReader GetRecordFilePeriod(DateTime start, DateTime end, string kind)
        {
            string str;
            str = string.Format("SELECT File FROM Record WHERE Date BETWEEN '{0}' AND '{1}' AND Type = '{2}'", start, end, kind);
            return cntl_b.sql.ExecuteQuery(str);
        }

        //--------------------------------------读数据库中的数据--------------------------------------------------------------
        public SQLiteDataReader GetDataFromTable(string table)
        {
            string str;
            str = string.Format("SELECT * FROM {0}", table);
            return cntl_b.sql.ExecuteQuery(str);
        }
    }
}
