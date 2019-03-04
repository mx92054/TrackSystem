using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks ;
using System.Data.SQLite;
using WSMBT;//取消注释，用于Modbus TCP
//using WSMBS;//Modbus master，用于C＃，VB和托管C ++的Modbus主控.NET控件。支持Modbus RTU / ASCII    

namespace InsituSystem
{
    class situController
    {
        public const short maxInputAddr = 100;
        public const short maxHoldAddr = 170;
        public const int maxCmdLen = 170;

        public WSMBTControl svr = new WSMBTControl();//取消注释
        //public WSMBSControl svr = new WSMBSControl();
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
        //public string curCPTName;
        //public string curVanName;
        public int cycle_time = 100;

        public SqLiteHelper sql;
        //？？？？？
        public bool Push(short adr, short val)
        {
            if ( !bConnected )
                return false ;

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
    class SvrFactory
    {
        private situController cntl = new situController() ;

        private Thread thread;
        private static AutoResetEvent startEvent;//常常被用来在两个线程之间进行信号发送。线程可以通过调用AutoResetEvent对象的WaitOne()方法进入等待状态，然后另外一个线程通过调用AutoResetEvent对象的Set()方法取消等待的状态。
        private static AutoResetEvent stopEvent;
        private System.Threading.Timer tmr;
            
        //--------------------初始化函数----------------------------------------------------------
        public SvrFactory()
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
            situController ctl = (situController)ob;
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
                //---------------------------云台 record------------------------   
                str = string.Format("INSERT INTO Pan_Tilt VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}')",
                       DateTime.Now, ctl.inputReg[70], ctl.inputReg[71], ctl.inputReg[72], ctl.inputReg[73],ctl. holdReg[46], ctl.holdReg[47]);
                dcmd.CommandText = str;
                dcmd.ExecuteNonQuery();
                
                //---------------------------CPT record------------------------
                //if (ctl.holdReg[1] == 1 || ctl.holdReg[3] == 1)//如果正转（贯入），holdReg读保持寄存器数据
                //    ctl.bRecCpt = true;
                //else
                //    ctl.bRecCpt = false;

                //if (ctl.bLastRecCpt == false && ctl.bRecCpt == true)//上一时刻未正转，此时正转则记录
                //{
                //    DateTime now = DateTime.Now;
                //    //表名
                //    ctl.curCPTName = string.Format("CPT{0:D4}{1:D2}{2:D2}_{3:D2}{4:D4}{5:D2}", now.Year,
                //       now.Month, now.Day, now.Hour, now.Minute, now.Second);
                //    //不存在则创建时间命名的表及结构
                //    str = string.Format("CREATE TABLE IF NOT EXISTS {0} (time DATETIME, CVal INTEGER, PVal INTEGER, TVal INTEGER)", ctl.curCPTName);
                //    dcmd.CommandText = str;
                //    dcmd.ExecuteNonQuery();
                //    //记录创建的表
                //    str = string.Format("INSERT INTO Record VALUES ( '{0}','{1}','CPT')", ctl.curCPTName, now);
                //    dcmd.CommandText = str;
                //    dcmd.ExecuteNonQuery();
                //}

                //ctl.bLastRecCpt = ctl.bRecCpt;
                ////向CPT表中插入数据，一次10条
                //if (ctl.bRecCpt)
                //{
                //    for(int i = 0 ; i < 10 ; i++)
                //    {
                //        str = string.Format("INSERT INTO {0} VALUES ('{1}','{2}','{3}','{4}')",ctl.curCPTName, DateTime.Now, ctl.inputReg[1 + i], ctl.inputReg[11 + i], ctl.inputReg[21 + i]);
                //        dcmd.CommandText = str;
                //        dcmd.ExecuteNonQuery();
                //    }
                //}

                ////---------------------------Vane record------------------------
                //if (ctl.holdReg[60] == 1)//如果剪切装置自动控制则记录
                //    ctl.bRecVan = true;
                //else
                //    ctl.bRecVan = false;

                //if (ctl.bLastRecVan == false && ctl.bRecVan == true)
                //{
                //    DateTime now = DateTime.Now;
                //    //VAN表名
                //    ctl.curVanName = string.Format("VAN{0:D4}{1:D2}{2:D2}_{3:D2}{4:D4}{5:D2}", now.Year,
                //       now.Month, now.Day, now.Hour, now.Minute, now.Second);
                //    //不存在则创建时间命名的表及结构
                //    str = string.Format("CREATE TABLE IF NOT EXISTS {0} (time DATETIME, Nerq INTEGER, Ang INTEGER)", ctl.curVanName);
                //    dcmd.CommandText = str;
                //    dcmd.ExecuteNonQuery();
                //    //记录创建的表
                //    str = string.Format("INSERT INTO Record VALUES ( '{0}','{1}','VAN')", ctl.curVanName, now);
                //    dcmd.CommandText = str;
                //    dcmd.ExecuteNonQuery();
                //}

                //ctl.bLastRecVan = ctl.bRecVan;
                ////向VAN表中插入10条数据
                //if (ctl.bRecVan)
                //{
                //    for (int i = 0; i < 10; i++)
                //    {
                //        str = string.Format("INSERT INTO {0} VALUES ('{1}','{2}','{3}')", ctl.curVanName, DateTime.Now, ctl.inputReg[40 + i], ctl.inputReg[50]);
                //        dcmd.CommandText = str;
                //        dcmd.ExecuteNonQuery();
                //    }
                //}

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
            if (nAddr < situController.maxInputAddr)//maxInputAddr=100
                return cntl.inputReg[nAddr];
            else
                return 0;
        }

        //--------------------读取保持寄存器内容----------------------------------------------------------
        public short GetHoldValue(int nAddr)
        {
            if (nAddr < situController.maxHoldAddr)
                return cntl.holdReg[nAddr];
            else
                return 0;
        }

        //--------------------读取发送成功次数（输出，写寄存器）----------------------------------------------------------
        public int GetCurRxCounter()//反了吧？Rx是发送，Tx是接收啊
        {
            return cntl.nRx;
        }

        //--------------------读取接收成功次数（输入，读寄存器）----------------------------------------------------------
        public int GetCurTxCounter()
        {
            return cntl.nTx;
        }

        //--------------------完成数据读取---------------------------------------------------------
        public bool CanReadHoldingReg()
        {
            if (cntl.bReadHoldReg)//说明读保持寄存器成功
            {
                cntl.bReadHoldReg = false;
                return true;
            }
            else
                return false;
        }

        //--------------------设置保持寄存器内容（界面通过操作此函数来改变保持寄存器数值）----------------------------------------------------------
        public void SetHoldValue(int nAddr, short val)//val为写单个寄存器变量
        {
            if (nAddr < situController.maxHoldAddr)//maxHoldAddr=100
            {
                if (cntl.holdReg[nAddr] != val)
                {
                    cntl.holdReg[nAddr] = val;
                    cntl.Push((short)nAddr, val);
                    cntl.bChanged = true;
                }
            }
        }

        //--------------------切换保持寄存器内容----------------------------------------------------------
        public short SwitchHoldValue(int nAddr)//开闭串口开关
        {
            if (nAddr < situController.maxHoldAddr)//maxHoldAddr=100
            {
                if (cntl.holdReg[nAddr] == 0)//若保持寄存器为空
                {
                    cntl.holdReg[nAddr] = 1;
                    cntl.Push((short)nAddr, 1);
                }
                else
                {
                    cntl.holdReg[nAddr] = 0;
                    cntl.Push((short)nAddr, 0);
                }

                cntl.bChanged = true;
                return cntl.holdReg[nAddr];
            }

            return 0 ;
        }

        //--------------------读取仪器连接状态----------------------------------------------------------
        public bool GetConnectStatus()
        {
            return cntl.bConnected ;
        }

        public void GetCycleTime(int ctime)//不返回值，则定义void类型
        {
            cntl.cycle_time = ctime;
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
            //int nErr = 0 ;
            Result result;
            bool bRunning = false;
            bool bSuccess = false;
            bool bFirst = true ;
            short adr=0, val=0;
            situController ctl = (situController)b;

            while (true)
            {
                int starttime_m = DateTime.Now.Millisecond;
                int starttime_s = DateTime.Now.Second;
                if (startEvent.WaitOne(10))//收到Set()信号，10ms后停止等待，waitone 只是挂起当前这个线程，没有Set()信号则会一直挂起
                {
                    bRunning = true;
                    bFirst = true;
                    //nErr = 0;
                    ctl.nRx = 0;
                    ctl.nTx = 0;
                    //用于Modbus TCP
                    result = ctl.svr.Connect(ctl.strIPAdr,1024);//取消注释                                       
                    ctl.svr.LicenseKey("2222222222222222222222222AAF2");//TCP密钥，不用密钥可用30分钟                    
                    if (result == Result.SUCCESS)
                        ctl.bConnected = true;
                    else
                        MessageBox.Show("Connect_a failure -" + result.ToString());                   
                }

                if (stopEvent.WaitOne(10))
                {
                    bRunning = false;
                    ctl.svr.Close();
                    ctl.bConnected = false;
                }

                if (bRunning && ctl.bConnected )
                {
                    try
                    {
                        ctl.nRx++;//写入成功次数
                        bSuccess = true;//写入成功标志

                        while (!ctl.Empty())//不相等才进行写入。ref是有进有出，out是只出不进。
                        {
                            ctl.Pop(ref adr, ref val);//ref 关键字使参数按引用传递
                            result = ctl.svr.WriteSingleRegister(1, (ushort)(100 + adr), val);//0x06写单个寄存器
                            if (result != Result.SUCCESS)
                            {
                                ctl.Push(adr, val);
                            }
                        }                       
                        //System.DateTime currentTime = new System.DateTime();
                        //currentTime = System.DateTime.Now;
                        //int starttime = currentTime.Millisecond;
                        //Thread.Sleep(100);
                        result = ctl.svr.ReadInputRegisters(1, (ushort)(0), 100, ctl.inputReg, 0);//0x04，对应服务器0x04，模拟量输入（只能读不能写，通常是状态寄存器），Read Input Registers，最大连续读125个，最后一个参数：寄存器数组中的偏移量开始写入
                        //System.DateTime currentTime_end = new System.DateTime();
                        //currentTime_end = System.DateTime.Now;
                        //int endtime = currentTime_end.Millisecond;
                        //int time = endtime - starttime;

                        if (result != Result.SUCCESS)
                        {
                            //bSuccess = false;
                            //if (nErr++ > 3)//错误次数>3，则认为连接断开
                            //{
                            //    ctl.bConnected = false;
                            //    MessageBox.Show("ReadInputRegisters Error :" + result.ToString());
                            //    break;
                            //}
                        }

                        if (bSuccess) ctl.nTx++;//读输入寄存器成功次数

                        if (bFirst)//控制寄存器，成功读取一次即可
                        {
                            result = ctl.svr.ReadHoldingRegisters(1, 100, 70, ctl.holdReg);//0x03，对应服务器0x03，读保持寄存器（可读可写，通常是功能控制寄存器，有些会掉电保持）
                            if (result == Result.SUCCESS)
                            {
                                bFirst = false;
                                ctl.bReadHoldReg = true;
                            }
                            else
                                MessageBox.Show("Read Holding Error :" + result.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Alarm");
                    }
                 }
                //定周期
                if (DateTime.Now.Second - starttime_s >= 0)
                {
                    int spendtime = (DateTime.Now.Second - starttime_s) * 1000 + DateTime.Now.Millisecond - starttime_m;
                    while (Math.Abs(spendtime) < ctl.cycle_time)
                    {
                        Thread.Sleep(1);
                        spendtime = (DateTime.Now.Second - starttime_s) * 1000 + DateTime.Now.Millisecond - starttime_m;
                    }
                }


            }
        }

        //--------------------------------------读取时间段内各种类型的数据个数--------------------------------------------------------------
        public SQLiteDataReader GetRecordFilePeriod(DateTime start, DateTime end, string kind)
        {
            string str ;
            str = string.Format("SELECT File FROM Record WHERE Date BETWEEN '{0}' AND '{1}' AND Type = '{2}'", start, end, kind);
            return cntl.sql.ExecuteQuery(str);
        }

        //--------------------------------------读数据库中的数据--------------------------------------------------------------
        public SQLiteDataReader GetDataFromTable(string table)
        {
            string str;
            str = string.Format("SELECT * FROM {0}", table);
            return cntl.sql.ExecuteQuery(str);
        }
    }
}
