﻿using TrionControlPanelDesktop.Data;
using TrionLibrary.Sys;

namespace TrionControlPanelDesktop.Controls
{
    public partial class HomeControl : UserControl
    {
        static double RamProcent;
        bool RamUsageHight;
        int CurrentWorldsOpen = 0;
        int CurrentLogonsOpen = 0;
        public HomeControl()
        {
            Dock = DockStyle.Fill;
            InitializeComponent();
        }

        private void ServerIconUI()
        {
            if (Main.ServerStatusWorld())
            { PICWorldServerStatus.Image = Properties.Resources.cloud_online_50; }
            else { PICWorldServerStatus.Image = Properties.Resources.cloud_offline_50; }
            //
            if (Main.ServerStatusLogon())
            { PICLogonServerStatus.Image = Properties.Resources.cloud_online_50; }
            else { PICLogonServerStatus.Image = Properties.Resources.cloud_offline_50; }
            //
            if (User.UI.Form.DBRunning && User.UI.Form.DBStarted ) { PICMySqlServerStatus.Image = Properties.Resources.cloud_online_50; }
            else { PICMySqlServerStatus.Image = Properties.Resources.cloud_offline_50; }
        }
        private void HomeControl_Load(object sender, EventArgs e)
        {
        }
        private void RamProcentage()
        {
            if (RamProcent > 80 && RamUsageHight == false)
            {
                Infos.Message = "Your Ram is in a critical availability phase! More than 80% are used.";
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
                    if (User.UI.Resource.CurrentWorldID > 0)
                    {
                        User.UI.Resource.WorldUsageRam = Watcher.ApplicationRamUsage(User.UI.Resource.CurrentWorldID);
                        User.UI.Resource.WorldCPUUsage = Watcher.ApplicationCpuUsage(User.UI.Resource.CurrentWorldID);
                    }
                    if (User.UI.Resource.CurrentAuthID > 0)
                    {
                        User.UI.Resource.AuthUsageRam = Watcher.ApplicationRamUsage(User.UI.Resource.CurrentAuthID);
                        User.UI.Resource.AuthCPUUsage = Watcher.ApplicationCpuUsage(User.UI.Resource.CurrentAuthID);
                    }
                });
                ApplicationResourceUsage.Start();
                Thread MachineCpuUtilizationThread = new(() =>
                {
                    User.UI.Resource.MachineCPUUsage =  Watcher.MachineCpuUtilization();
                });
                MachineCpuUtilizationThread.Start();
                Thread IsServerRunning = new(() =>
                {
                    if (User.System.LogonProcessesID.Count > 0)
                    {
                        Main.IsLogonRunning(User.System.LogonProcessesID);
                    }
                    if(User.System.WorldProcessesID.Count > 0)
                    {
                        Main.IsWorldRunning(User.System.WorldProcessesID);
                    }
                    if(User.System.DatabaseProcessID.Count > 0)
                    {
                        Main.IsDatabaseRunning(User.System.DatabaseProcessID);
                    }
                });
                IsServerRunning.Start();
                LBLMysqlPort.Text = $"Process ID: {string.Join(", ", User.System.DatabaseProcessID.Select(p => p.ID))}";
                LBLLogonPort.Text = $"Process ID: {string.Join(", ", User.System.LogonProcessesID.Select(p => p.ID))}";
                LBLWordPort.Text = $"Process ID: {string.Join(", ", User.System.WorldProcessesID.Select(p => p.ID))}";

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
            catch (Exception ex)
            {
                Infos.Message = ex.Message;
            }
            if (User.System.WorldProcessesID.Count >= 1)
            {
                LBLWorldsOpen.Text = $"{CurrentWorldsOpen + 1}/{User.System.WorldProcessesID.Count}";
                PNLWorldCount.Visible = true;
                if (CurrentWorldsOpen == User.System.WorldProcessesID.Count && User.System.WorldProcessesID.Count > 2)
                {
                    BTNWorldFW.Visible = false;
                    BTNWorldBC.Visible = true;
                }
                else if (CurrentWorldsOpen == 0 && User.System.WorldProcessesID.Count > 2)
                {
                    BTNWorldFW.Visible = true;
                    BTNWorldBC.Visible = false;
                }
                else if(User.System.WorldProcessesID.Count == 1)
                {
                    BTNWorldFW.Visible = false;
                    BTNWorldBC.Visible = false;
                }
                else
                {
                    BTNWorldFW.Visible = true;
                    BTNWorldBC.Visible = true;
                }
            }
            else
            {
                PNLWorldCount.Visible = false;
                BTNWorldFW.Visible = false;
                BTNWorldBC.Visible = false;
            }
            if (User.System.LogonProcessesID.Count >= 1)
            {
                LBLLogonOpen.Text = $"{CurrentLogonsOpen + 1}/{User.System.LogonProcessesID.Count}";
                PNLLoginCount.Visible = true;
                if (CurrentWorldsOpen == User.System.LogonProcessesID.Count && User.System.LogonProcessesID.Count > 2)
                {
                    BTNLoginFW.Visible = false;
                    BTNLoginBC.Visible = true;
                }
                else if (CurrentWorldsOpen == 0 && User.System.LogonProcessesID.Count > 2)
                {
                    BTNLoginFW.Visible = true;
                    BTNLoginBC.Visible = false;
                }
                else if (User.System.LogonProcessesID.Count == 1)
                {
                    BTNLoginFW.Visible = false;
                    BTNLoginBC.Visible = false;
                }
                else
                {
                    BTNLoginFW.Visible = true;
                    BTNLoginBC.Visible = true;
                }
            }
            else
            {
                PNLLoginCount.Visible = false;
                BTNLoginFW.Visible = false;
                BTNLoginBC.Visible = false;
            }
        }
        private void TimerRam_Tick(object sender, EventArgs e)
        {
            RamProcentage();
        }
        private void TimerStopWatch_Tick(object sender, EventArgs e)
        {
            if (Main.ServerStatusWorld() == true && User.System.WorldProcessesID.Count > 0)
            {
                TimeSpan elapsedTime = DateTime.Now - User.System.WorldStartTime;
                LBLUpTimeWorld.Text = $"Uptime: {elapsedTime.Days}D : {elapsedTime.Hours}H : {elapsedTime.Minutes}M : {elapsedTime.Seconds}S";
            }
            else
            {
                LBLUpTimeWorld.Text = $"Uptime: 0D : 0H : 0M : 0S";
            }
            if (User.UI.Form.DBStarted == true && User.System.DatabaseProcessID.Count > 0)
            {
                TimeSpan elapsedTime = DateTime.Now - User.System.DatabaseStartTime;
                LBLUpTimeDatabase.Text = $"Uptime: {elapsedTime.Days}D : {elapsedTime.Hours}H : {elapsedTime.Minutes}M : {elapsedTime.Seconds}S";
            }
            else
            {
                LBLUpTimeDatabase.Text = $"Uptime: 0D : 0H : 0M : 0S";
            }
            if (Main.ServerStatusLogon() == true && User.System.LogonProcessesID.Count > 0)
            {
                TimeSpan elapsedTime = DateTime.Now - User.System.LogonStartTime;
                LBLUpTimeLogon.Text = $"Uptime: {elapsedTime.Days}D : {elapsedTime.Hours}H : {elapsedTime.Minutes}M : {elapsedTime.Seconds}S";
            }
            else
            {
                LBLUpTimeLogon.Text = $"Uptime: 0D : 0H : 0M : 0S";
            }
        }
        private void BTNWorldBC_Click(object sender, EventArgs e)
        {
            if (CurrentWorldsOpen + 1 <= User.System.WorldProcessesID.Count && CurrentWorldsOpen != 0)
            {
                CurrentWorldsOpen++;
                User.UI.Resource.CurrentWorldID = User.System.WorldProcessesID[CurrentWorldsOpen].ID;
            }
        }

        private void BTNWorldFW_Click(object sender, EventArgs e)
        {
            if (CurrentWorldsOpen + 1 < User.System.WorldProcessesID.Count && CurrentWorldsOpen + 1 != User.System.WorldProcessesID.Count)
            {
                CurrentWorldsOpen++;
                User.UI.Resource.CurrentWorldID = User.System.WorldProcessesID[CurrentWorldsOpen].ID;
            }
        }
    }
}
