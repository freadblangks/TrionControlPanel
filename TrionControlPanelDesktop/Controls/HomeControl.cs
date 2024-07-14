﻿using TrionControlPanelDesktop.Data;
using TrionLibrary.Sys;

namespace TrionControlPanelDesktop.Controls
{
    public partial class HomeControl : UserControl
    {
        static double RamProcent;
        bool RamUsageHight;
        private static void FirstLoad()
        {

        }

        public HomeControl()
        {
            Dock = DockStyle.Fill;
            InitializeComponent();
        }
        private bool ServerStatusWorld()
        {
            if (User.UI.Form.CustWorldRunning ||
                User.UI.Form.ClassicWorldRunning ||
                User.UI.Form.TBCWorldRunning ||
                User.UI.Form.WotLKWorldRunning ||
                User.UI.Form.CataWorldRunning ||
                User.UI.Form.MOPWorldRunning)
            { return true; }
            else { return false; }
        }
        private bool ServerStatusLogon()
        {
            if (User.UI.Form.CustLogonRunning ||
                User.UI.Form.ClassicLogonRunning ||
                User.UI.Form.TBCLogonRunning ||
                User.UI.Form.WotLKLogonRunning ||
                User.UI.Form.CataLogonRunning ||
                User.UI.Form.MOPLogonRunning)
            { return true; }
            else { return false; }
        }
        private void ServerIconUI()
        {
            if (ServerStatusWorld()) 
            { PICWorldServerStatus.Image = Properties.Resources.cloud_online_50; }
            else { PICWorldServerStatus.Image = Properties.Resources.cloud_offline_50; }
            //
            if (ServerStatusLogon()) 
            { PICLogonServerStatus.Image = Properties.Resources.cloud_online_50; }
            else { PICLogonServerStatus.Image = Properties.Resources.cloud_offline_50; }
            //
            if (User.UI.Form.DBRunning) { PICMySqlServerStatus.Image = Properties.Resources.cloud_online_50; }
            else { PICMySqlServerStatus.Image = Properties.Resources.cloud_offline_50; }
        }
        private void HomeControl_Load(object sender, EventArgs e)
        {
            FirstLoad();
        }
        private void RamProcentage()
        {
            if (RamProcent > 80 && RamUsageHight == false)
            {
                Infos.Message = "Your Ram is in a critical availability phase! More than 80% are used!!";
                RamUsageHight = true;
            }
            if (RamProcent < 80)
            {
                RamUsageHight = false;
            }
        }
        static double CalculatePercentage(double whole, double part)
        {
            return (part / whole) * 100;
        }
        private void TimerWacher_Tick(object sender, EventArgs e)
        {
            ServerIconUI();
            TimerRam.Enabled = true;
            if (PCResorcePbarRAM.Value > 0) { User.UI.Form.StartUpLoading++; }
            try
            {
                Thread MachineRamThread = new(() =>
                {
                    User.UI.Resource.MachineTotalRam = Watcher.MachineTotalRam();
                    User.UI.Resource.MachineUsageRam = User.UI.Resource.MachineTotalRam - Watcher.CurentPcRamUsage();
                });
                MachineRamThread.Start();
                //
                RamProcent = CalculatePercentage(User.UI.Resource.MachineTotalRam, User.UI.Resource.MachineUsageRam);
                //
                User.UI.Resource.AuthTotalRam = (int)User.UI.Resource.MachineUsageRam;
                User.UI.Resource.WorldTotalRam = (int)User.UI.Resource.MachineUsageRam;
                //
                Thread ApplicationResourceUsage = new(() =>
                {
                    foreach (var WorldProcessid in User.System.WorldProcessesID)
                    {
                        User.UI.Resource.WorldCPUUsage = Watcher.ApplicationCpuUsage(WorldProcessid.ID);
                        User.UI.Resource.WorldUsageRam = Watcher.ApplicationRamUsage(WorldProcessid.ID);
                    }
                    foreach (var logonProcessesID in User.System.LogonProcessesID)
                    {
                        User.UI.Resource.AuthUsageRam = Watcher.ApplicationRamUsage(logonProcessesID.ID);
                        User.UI.Resource.AuthCPUUsage = Watcher.ApplicationCpuUsage(logonProcessesID.ID);
                    }
                });
                ApplicationResourceUsage.Start();

                Thread MachineCpuUtilizationThread = new(() =>
                {
                    User.UI.Resource.MachineCPUUsage = Watcher.MachineCpuUtilization();
                });
                MachineCpuUtilizationThread.Start();
                //
                LBLMysqlPort.Text = $"ProcessID: {string.Join(", ", User.System.DatabaseProcessID)}";
                LBLLogonPort.Text = $"ProcessID: {string.Join(", ", User.System.LogonProcessesID)}";
                LBLWordPort.Text = $"ProcessID: {string.Join(", ", User.System.WorldProcessesID)}";

                PCResorcePbarRAM.Maximum = User.UI.Resource.MachineTotalRam;
                PCResorcePbarRAM.Value = User.UI.Resource.MachineUsageRam;
                PCResorcePbarCPU.Value = User.UI.Resource.MachineCPUUsage;
                WorldPbarRAM.Maximum = User.UI.Resource.WorldTotalRam;
                WorldPbarRAM.Value = User.UI.Resource.WorldUsageRam;
                WorldPbarCPU.Value = User.UI.Resource.WorldCPUUsage;
                LoginPbarRAM.Maximum = User.UI.Resource.AuthTotalRam;
                LoginPbarRAM.Value = User.UI.Resource.AuthUsageRam;
                LoginPbarCPU.Value = User.UI.Resource.AuthCPUUsage;
            }
            catch
            {
            }
        }
        private void TimerRam_Tick(object sender, EventArgs e)
        {
            RamProcentage();
        }
        private void TimerStopWatch_Tick(object sender, EventArgs e)
        {
            if (ServerStatusWorld() == true && User.System.WorldProcessesID.Count > 0)
            {
                TimeSpan elapsedTime = DateTime.Now - User.System.WorldStartTime;
                LBLUpTimeWorld.Text = $"Up Time: {elapsedTime.Days}D : {elapsedTime.Hours}H : {elapsedTime.Minutes}M : {elapsedTime.Seconds}S";
            }
            if (User.UI.Form.DBRunning == true && User.System.DatabaseProcessID.Count > 0)
            {
                TimeSpan elapsedTime = DateTime.Now - User.System.DatabaseStartTime;
                LBLUpTimeDatabase.Text = $"Up Time: {elapsedTime.Days}D : {elapsedTime.Hours}H : {elapsedTime.Minutes}M : {elapsedTime.Seconds}S";
            }
            if (ServerStatusLogon() == true && User.System.LogonProcessesID.Count > 0)
            {
                TimeSpan elapsedTime = DateTime.Now - User.System.LogonStartTime;
                LBLUpTimeLogon.Text = $"Up Time: {elapsedTime.Days}D : {elapsedTime.Hours}H : {elapsedTime.Minutes}M : {elapsedTime.Seconds}S";
            }
        }
    }
}
