using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace InsituSystem
{
    class AutoSizeFormClass
    {
        //(1).声明结构,只记录窗体和其控件的初始位置和大小。  
        public struct controlRect
        {
            public int Left;
            public int Top;
            public int Width;
            public int Height;
        }
        //(2).声明 1个对象  
        //注意这里不能使用控件列表记录 List<Control> nCtrl;，因为控件的关联性，记录的始终是当前的大小。  
        public List<controlRect> oldCtrl, oldCtrl_Child;       
        //(3). 创建两个函数  
        //(3.1)记录窗体和其控件的初始位置和大小,  
        public void controllInitializeSize(Form mForm)
        {            
            {               
                oldCtrl = new List<controlRect>();
                oldCtrl_Child = new List<controlRect>();
                controlRect cR;
                cR.Left = mForm.Left; cR.Top = mForm.Top; cR.Width = mForm.Width; cR.Height = mForm.Height;
                oldCtrl.Add(cR);
                foreach (Control a in mForm.Controls)
                {
                    if (a is TabControl)
                    {
                        foreach (Control b in a.Controls)
                        {
                            foreach (Control c in b.Controls)
                            {
                                controlRect objCtrl_Child_c;
                                objCtrl_Child_c.Left = c.Left; objCtrl_Child_c.Top = c.Top; objCtrl_Child_c.Width = c.Width; objCtrl_Child_c.Height = c.Height;
                                oldCtrl_Child.Add(objCtrl_Child_c);
                                foreach (Control d in c.Controls)
                                {
                                    controlRect objCtrl_Child_d;
                                    objCtrl_Child_d.Left = d.Left; objCtrl_Child_d.Top = d.Top; objCtrl_Child_d.Width = d.Width; objCtrl_Child_d.Height = d.Height;
                                    oldCtrl_Child.Add(objCtrl_Child_d);
                                }
                            }
                        }
                    }
                    else if (a is Panel)
                    {
                        foreach (Control b in a.Controls)
                        {
                            controlRect objCtrl_Child_b;
                            objCtrl_Child_b.Left = b.Left; objCtrl_Child_b.Top = b.Top; objCtrl_Child_b.Width = b.Width; objCtrl_Child_b.Height = b.Height;
                            oldCtrl_Child.Add(objCtrl_Child_b);
                            foreach (Control c in b.Controls)
                            {
                                controlRect objCtrl_Child;
                                objCtrl_Child.Left = c.Left; objCtrl_Child.Top = c.Top; objCtrl_Child.Width = c.Width; objCtrl_Child.Height = c.Height;
                                oldCtrl_Child.Add(objCtrl_Child);
                            }
                        }
                    }
                    controlRect objCtrl;
                    objCtrl.Left = a.Left; objCtrl.Top = a.Top; objCtrl.Width = a.Width; objCtrl.Height = a.Height;
                    oldCtrl.Add(objCtrl);
                }
            }            
        }
        //(3.2)控件自适应大小,  
        public void controlAutoSize(Form mForm)
        {            
            float wScale = (float)mForm.Width / (float)oldCtrl[0].Width;//新旧窗体之间的比例，与最早的旧窗体  
            float hScale = (float)mForm.Height / (float)oldCtrl[0].Height;//.Height;  
            int ctrLeft0, ctrTop0, ctrWidth0, ctrHeight0;
            int ctrlNo = 1;//第1个是窗体自身的 Left,Top,Width,Height，所以窗体控件从ctrlNo=1开始  
            int ctrlNo_Child = 0;
            foreach (Control a in mForm.Controls)
            {
                if (a is TabControl)
                {
                    foreach (Control b in a.Controls)
                    {
                        foreach (Control c in b.Controls)
                        {
                            ctrLeft0 = oldCtrl_Child[ctrlNo_Child].Left;
                            ctrTop0 = oldCtrl_Child[ctrlNo_Child].Top;
                            ctrWidth0 = oldCtrl_Child[ctrlNo_Child].Width;
                            ctrHeight0 = oldCtrl_Child[ctrlNo_Child].Height;
                            c.Left = (int)(ctrLeft0 * wScale);//新旧控件之间的线性比例。控件位置只相对于窗体，所以不能加 + wLeft1  
                            c.Top = (int)(ctrTop0 * hScale);//  
                            c.Width = (int)(ctrWidth0 * wScale);//只与最初的大小相关，所以不能与现在的宽度相乘 (int)(c.Width * w);  
                            c.Height = (int)(ctrHeight0 * hScale);//  
                            ctrlNo_Child += 1;
                            foreach (Control d in c.Controls)
                            {
                                ctrLeft0 = oldCtrl_Child[ctrlNo_Child].Left;//获取的不正确
                                ctrTop0 = oldCtrl_Child[ctrlNo_Child].Top;
                                ctrWidth0 = oldCtrl_Child[ctrlNo_Child].Width;
                                ctrHeight0 = oldCtrl_Child[ctrlNo_Child].Height;
                                d.Left = (int)(ctrLeft0 * wScale);//新旧控件之间的线性比例。控件位置只相对于窗体，所以不能加 + wLeft1  
                                d.Top = (int)(ctrTop0 * hScale);//  
                                d.Width = (int)(ctrWidth0 * wScale);//只与最初的大小相关，所以不能与现在的宽度相乘 (int)(c.Width * w);  
                                d.Height = (int)(ctrHeight0 * hScale);//  
                                ctrlNo_Child += 1;
                            }
                        }
                    }
                }
                else if (a is Panel)
                {
                    foreach (Control b in a.Controls)
                    {
                        ctrLeft0 = oldCtrl_Child[ctrlNo_Child].Left;
                        ctrTop0 = oldCtrl_Child[ctrlNo_Child].Top;
                        ctrWidth0 = oldCtrl_Child[ctrlNo_Child].Width;
                        ctrHeight0 = oldCtrl_Child[ctrlNo_Child].Height;
                        b.Left = (int)(ctrLeft0 * wScale);//新旧控件之间的线性比例。控件位置只相对于窗体，所以不能加 + wLeft1  
                        b.Top = (int)(ctrTop0 * hScale);//  
                        b.Width = (int)(ctrWidth0 * wScale);//只与最初的大小相关，所以不能与现在的宽度相乘 (int)(c.Width * w);  
                        b.Height = (int)(ctrHeight0 * hScale);//  
                        ctrlNo_Child += 1;
                        foreach (Control c in b.Controls)
                        {
                            ctrLeft0 = oldCtrl_Child[ctrlNo_Child].Left;
                            ctrTop0 = oldCtrl_Child[ctrlNo_Child].Top;
                            ctrWidth0 = oldCtrl_Child[ctrlNo_Child].Width;
                            ctrHeight0 = oldCtrl_Child[ctrlNo_Child].Height;
                            c.Left = (int)(ctrLeft0 * wScale);//新旧控件之间的线性比例。控件位置只相对于窗体，所以不能加 + wLeft1  
                            c.Top = (int)(ctrTop0 * hScale);//  
                            c.Width = (int)(ctrWidth0 * wScale);//只与最初的大小相关，所以不能与现在的宽度相乘 (int)(c.Width * w);  
                            c.Height = (int)(ctrHeight0 * hScale);//  
                            ctrlNo_Child += 1;
                        }
                    }
                }
                ctrLeft0 = oldCtrl[ctrlNo].Left;
                ctrTop0 = oldCtrl[ctrlNo].Top;
                ctrWidth0 = oldCtrl[ctrlNo].Width;
                ctrHeight0 = oldCtrl[ctrlNo].Height;                
                a.Left = (int)((ctrLeft0) * wScale);//新旧控件之间的线性比例。控件位置只相对于窗体，所以不能加 + wLeft1  
                a.Top = (int)((ctrTop0) * hScale);//  
                a.Width = (int)(ctrWidth0 * wScale);//只与最初的大小相关，所以不能与现在的宽度相乘 (int)(c.Width * w);  
                a.Height = (int)(ctrHeight0 * hScale);//  
                ctrlNo += 1;
            }
        }  
    }
}
