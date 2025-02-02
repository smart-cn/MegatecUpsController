﻿using log4net;
using System;
using System.Reflection;
using System.Text;
using System.Threading;
using UsbLibrary;

namespace MegatecUpsController
{
    static class UsbOps
    {

        private static readonly ILog applog = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly ILog eventlog = LogManager.GetLogger("eventlog");

        public static UsbHidPort usb = new UsbHidPort();
        private static readonly Timer timerUSB;
        private static string rawDataDecoded = "";

        private static readonly byte[] comm_status = Encoding.ASCII.GetBytes("0QS\r00000"); //буфер 9 байт, первый проглатывается, далее команда megatec-протокола, потом символ возврата строки, остальное нулями
        private static readonly byte[] comm_beeper = Encoding.ASCII.GetBytes("0Q\r000000");

        static UsbOps()
        {
            usb.OnSpecifiedDeviceArrived += new EventHandler(Usb_OnSpecifiedDeviceArrived);
            usb.OnSpecifiedDeviceRemoved += new EventHandler(Usb_OnSpecifiedDeviceRemoved);
            usb.OnDataSend += new EventHandler(Usb_OnDataSend);
            usb.OnDataRecieved += new DataRecievedEventHandler(Usb_OnDataRecieved);

            TimerCallback tm = new TimerCallback(TimerActionDataRequest);
            timerUSB = new Timer(tm, null, 0, 1000);
        }

        public static bool SetupUsbDevice(int vid, int pid)
        {
            if (usb.Ready())
            {
                usb.Close();
            }

            usb.VendorId = vid;
            usb.ProductId = pid;
            usb.Open(true);

            if (usb.SpecifiedDevice != null)
            {
                UpsData.ConnectStatus = true;
                return true;
            }
            else
            {
                UpsData.ConnectStatus = false;
                return false;
            }
        }

        public static void StopUsbTimer()
        {
            if (timerUSB != null)
            {
                timerUSB.Dispose();
            }                
        }

        private static void TimerActionDataRequest(object obj)
        {
            if (usb.SpecifiedDevice != null)
            {
                usb.SpecifiedDevice.SendData(comm_status);
            }
        }

        public static void SwitchUpsBeeper()
        {
            if (usb.SpecifiedDevice != null)
            {
                usb.SpecifiedDevice.SendData(comm_beeper);
            }
        }

        private static void Usb_OnSpecifiedDeviceArrived(object sender, EventArgs e)
        {
            eventlog.Info("ИБП подключён");
            UpsData.ConnectStatus = true;
        }

        private static void Usb_OnSpecifiedDeviceRemoved(object sender, EventArgs e)
        {
            eventlog.Info("Потеряно соединение с ИБП");
            UpsData.ConnectStatus = false;
        }

        private static void Usb_OnDataSend(object sender, EventArgs e)
        {
            //нечего тут логировать, мы каждую секунду шлём данные
        }

        private static void Usb_OnDataRecieved(object sender, DataRecievedEventArgs args)
        {
            foreach (byte myData in args.data)
            {
                if (myData != 0x00) //иногда попадаются нулевые байты - игнорируем, засоряют только
                {
                    char c = Convert.ToChar(myData);
                    rawDataDecoded += c.ToString(); //набиваем буферную строку
                }

                if (myData == 0x0D) //если пришёл символ возврата строки
                {
                    UpsData.UpdateData(rawDataDecoded); //значит это конец буферной строки, отправляем её на обработку
                    rawDataDecoded = ""; //и очищаем для приёма новой порции данных
                }
            }
        }

    }
}
