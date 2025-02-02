﻿using log4net;
using MegatecUpsController.Properties;
using System;
using System.Globalization;
using System.Reflection;
using static MegatecUpsController.Structures;

namespace MegatecUpsController
{
    static class UpsData
    {
        // Megatec data
        public static float InputVoltage { get; private set; }
        public static float InputVoltageLastFault { get; private set; }
        public static float OutputVoltage { get; private set; }
        public static int LoadPercent { get; private set; }
        public static float Hz { get; private set; }
        public static float BatteryVoltage { get; private set; }
        public static float Temperature { get; private set; }
        public static string BinaryStatus { get; private set; }

        // Decoded binary status data
        public static bool IsUtilityFail { get; private set; } //true - питание от батарейки, false - от розетки
        public static bool IsBatteryLow { get; private set; }
        public static bool IsActiveAVR { get; private set; }
        public static bool IsUpsFailed { get; private set; }
        public static bool IsStandby { get; private set; }
        public static bool IsTestInProgress { get; private set; }
        public static bool IsShutdownActive { get; private set; }
        public static bool IsBeeperOn { get; private set; }

        // Calculated data
        public static int BatteryPercent { get; private set; }
        public static float CurVA { get; private set; } // KUURRRWWA!!!
        public static float CurWatt { get; private set; }
        public static float CurAmper { get; private set; }

        //History data
        public static SizedQueue<double> InputVoltageHistory = new SizedQueue<double>(60);
        public static SizedQueue<double> OutputVoltageHistory = new SizedQueue<double>(60);

        //Settings data
        public static int UpsAction { private get; set; } //0 - при низком заряде, 1 - по напряжению
        public static int ShutdownAction { private get; set; } //0 - завершение работы, 1 - гибернация
        public static int SecondsTillShutdownAction { private get; set; }
        public static float ShutdownVoltage { private get; set; }
        public static float BatteryVoltageMax { private get; set; }
        public static float BatteryVoltageMin { private get; set; }
        public static float BatteryVoltageMaxOnLoad { private get; set; }
        public static float UpsVA { private get; set; }

        // Common data
        public static string RawInputData { get; private set; }
        public static DateTime LastUpdated { get; private set; }
        public static bool ConnectStatus { get; set; }

        private static readonly ILog applog = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly ILog eventlog = LogManager.GetLogger("eventlog");
        private static bool isGoingToHibernation = false;
        private static bool isCheckedOnce = false; // для выполнения действия 1 раз при первом получении данных от ИБП

        static UpsData()
        {
            LoadSettings();
        }

        private static void LoadSettings()
        {
            UpsAction = Settings.Default.upsAction;
            ShutdownAction = Settings.Default.shutdownAction;
            ShutdownVoltage = float.Parse(Settings.Default.shutdownVoltage, CultureInfo.InvariantCulture.NumberFormat);
            SecondsTillShutdownAction = Convert.ToInt32(Settings.Default.shutdownActionTimeout);
            BatteryVoltageMax = float.Parse(Settings.Default.batteryVoltage_max, CultureInfo.InvariantCulture.NumberFormat);
            BatteryVoltageMin = float.Parse(Settings.Default.batteryVoltage_min, CultureInfo.InvariantCulture.NumberFormat);
            BatteryVoltageMaxOnLoad = float.Parse(Settings.Default.batteryVoltage_maxOnLoad, CultureInfo.InvariantCulture.NumberFormat);
            UpsVA = float.Parse(Settings.Default.upsVA, CultureInfo.InvariantCulture.NumberFormat);

            for (int i = 0; i < 60; i++) //заполнение у графика оси Y (напряжения) нулями
            {
                InputVoltageHistory.Enqueue(0);
                OutputVoltageHistory.Enqueue(0);
            }
        }

        public static void UpdateData(string RawData)
        {
            try
            {
                RawInputData = RawData;
                RawInputData = RawInputData.Remove(0, RawInputData.IndexOf("(") + 1);
                string[] arrayOfData = RawInputData.Split(' ');
                try
                {
                    InputVoltage = float.Parse(arrayOfData[0], CultureInfo.InvariantCulture.NumberFormat);
                }
                catch { };
                try
                {
                    InputVoltageLastFault = float.Parse(arrayOfData[1], CultureInfo.InvariantCulture.NumberFormat);
                }
                catch { };
                try
                {
                    OutputVoltage = float.Parse(arrayOfData[2], CultureInfo.InvariantCulture.NumberFormat);
                }
                catch { };
                try
                {
                    LoadPercent = Convert.ToInt32(arrayOfData[3]);
                }
                catch { };
                try
                {
                    Hz = float.Parse(arrayOfData[4], CultureInfo.InvariantCulture.NumberFormat);
                }
                catch { };
                try
                {
                    BatteryVoltage = float.Parse(arrayOfData[5], CultureInfo.InvariantCulture.NumberFormat);
                }
                catch { };
                try
                {
                    Temperature = float.Parse(arrayOfData[6], CultureInfo.InvariantCulture.NumberFormat);
                }
                catch { };
                try
                {
                    BinaryStatus = arrayOfData[7];
                }
                catch { };

                if (IsActiveAVR != BinaryStatus[2].Equals('1')) //если старый статус AVR не равен новому
                {
                    if (IsActiveAVR)
                    {
                        eventlog.Info("AVR выключен");
                    }
                    else
                    {
                        eventlog.Info("AVR активирован");
                    }
                }

                if (IsUtilityFail != BinaryStatus[0].Equals('1'))
                {
                    if (IsUtilityFail)
                    {
                        eventlog.Info("Внешнее питание восстановлено");
                    }
                    else
                    {
                        eventlog.Info("ВНЕШНЕЕ ПИТАНИЕ ОТКЛЮЧЕНО! ПЕРЕХОД НА ПИТАНИЕ ОТ БАТАРЕИ!");
                    }
                }

                IsUtilityFail = BinaryStatus[0].Equals('1');
                IsBatteryLow = BinaryStatus[1].Equals('1');
                IsActiveAVR = BinaryStatus[2].Equals('1');
                IsUpsFailed = BinaryStatus[3].Equals('1');
                IsStandby = BinaryStatus[4].Equals('1');
                IsTestInProgress = BinaryStatus[5].Equals('1');
                IsShutdownActive = BinaryStatus[6].Equals('1');
                IsBeeperOn = BinaryStatus[7].Equals('1');

                if (IsUtilityFail)
                {
                    BatteryPercent = Convert.ToInt32(100 - (100 / (BatteryVoltageMaxOnLoad - BatteryVoltageMin) * (BatteryVoltageMaxOnLoad - BatteryVoltage))); //при переходе на батарейку, её напряжение проседает, если это не учитывать то процент заряда рывком упадёт до 80%
                }
                else
                {
                    BatteryPercent = Convert.ToInt32(100 - (100 / (BatteryVoltageMax - BatteryVoltageMin) * (BatteryVoltageMax - BatteryVoltage)));
                }

                CurVA = LoadPercent * UpsVA / 100;
                CurWatt = Convert.ToSingle(CurVA * 0.6); //будем считать что cosφ (коэффициент активной мощности) у компов равен 0.6
                if (OutputVoltage > 0)
                {
                    CurAmper = (float)Math.Round(CurWatt / OutputVoltage, 1);
                }
                else
                {
                    CurAmper = 0;
                }

                InputVoltageHistory.Enqueue(InputVoltage);
                OutputVoltageHistory.Enqueue(OutputVoltage);

                LastUpdated = DateTime.Now;
            }
            catch (Exception e)
            {
                applog.Error("Error parsing UPS incoming data ->" + RawInputData + "<-:" + e.Message);
            }

            if (!isCheckedOnce)
            {
                CheckOnce();
                isCheckedOnce = true;
            }

            if (IsUtilityFail)
            {
                CheckShutdownAction();
            }
            else
            {
                if (isGoingToHibernation) //описание ниже в CheckShutdownAction()
                {
                    isGoingToHibernation = false;
                }

                if (SecondsTillShutdownAction == 0)
                {
                    SecondsTillShutdownAction = Convert.ToInt32(Settings.Default.shutdownActionTimeout);
                }
            }
        }

        private static void CheckOnce()
        {
            // после выключения ИБП забывает что бипер был выключен
            // при подключении ИБП настройки бипера приводятся в соответствие с выбором в программе
            if (Settings.Default.isBeeperOn != IsBeeperOn)
            {
                Settings.Default.isBeeperOn = !IsBeeperOn;
                Settings.Default.Save();
                UsbOps.SwitchUpsBeeper();
            }
        }

        private static void CheckShutdownAction()
        {
            if (UpsAction == 0)
            {
                if (IsBatteryLow)
                {
                    TickDelayAndDoShutdownAction();
                }
            }
            else if (UpsAction == 1)
            {
                if (BatteryVoltage <= ShutdownVoltage)
                {
                    TickDelayAndDoShutdownAction();
                }
            }
        }

        private static void TickDelayAndDoShutdownAction()
        {
            if (SecondsTillShutdownAction == 0)
            {
                if (ShutdownAction == 0)
                {
                    PowerOps.ShutdownComputer();
                }
                else if (ShutdownAction == 1)
                {
                    // Если мы уже отправили один раз комп в гибернацию, то по возвращению он уйдёт в неё снова. 
                    // Так что проверяем, если уже уходил (isGoingToHibernation = true), то не отправляем его туда снова.
                    // isGoingToHibernation перезарядится в false при возвращении из гибернации и получения от ИБП статуса работы от розетки
                    if (!isGoingToHibernation)
                    {
                        isGoingToHibernation = true;
                        PowerOps.HibernateComputer();
                    }
                }

            }
            else
            {
                SecondsTillShutdownAction--;
            }
        }
    }
}
