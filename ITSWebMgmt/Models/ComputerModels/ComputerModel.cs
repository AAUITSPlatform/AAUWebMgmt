using ITSWebMgmt.WebMgmtErrors;
using System.Collections.Generic;

namespace ITSWebMgmt.Models
{
    public class ComputerModel
    {
        public List<TabModel> Tabs = new List<TabModel>();

        public MacComputerModel Mac;
        public WindowsComputerModel Windows;

        //Display
        public bool IsWindows { get; set; }
        public bool IsInAD { get; set; }
        public string ComputerName { get; set; } = "AAU115359";
        public string ErrorCountMessage { get; set; }
        public string ErrorMessages { get; set; }
        public string ResultError { get; set; }
        public WebMgmtErrorList ErrorList { get; set; }
        public virtual bool ComputerFound { get; set; }


        public ComputerModel(string computerName)
        {
            if (computerName != null)
            {
                ComputerName = computerName;
            }
        }

        public void SetTabs()
        {
            if (IsWindows)
            {
                Tabs.Add(new TabModel("groups", "Groups"));
                Tabs.Add(new TabModel("sccminfo", "SCCM info"));
                Tabs.Add(new TabModel("sccmcollections", "SCCM Collections"));
                Tabs.Add(new TabModel("sccmInventory", "Software info"));
                Tabs.Add(new TabModel("sccmHW", "Hardware info"));
                Tabs.Add(new TabModel("sccmAV", "Antivirus"));
                Tabs.Add(new TabModel("purchase", "Purchase info (INDB)"));
                Tabs.Add(new TabModel("rawdata", "Raw data (AD)"));
                Tabs.Add(new TabModel("rawdatasccm", "Raw data (SCCM)"));
                Tabs.Add(new TabModel("tasks", "Tasks"));
                Tabs.Add(new TabModel("warnings", "Warnings"));
            }
            else
            {
                Tabs.Add(new TabModel("macgroups", "Groups"));
                Tabs.Add(new TabModel("macHW", "Hardware info"));
                Tabs.Add(new TabModel("macDisk", "Disk info"));
                Tabs.Add(new TabModel("macSW", "Software info"));
                Tabs.Add(new TabModel("macnetwork", "Network info"));
                Tabs.Add(new TabModel("macloaclaccounts", "Local accounts"));
                Tabs.Add(new TabModel("purchase", "Purchase info (INDB)"));
                Tabs.Add(new TabModel("warnings", "Warnings"));
                if (IsInAD)
                {
                    Tabs.Add(new TabModel("groups", "Groups (AD)"));
                    Tabs.Add(new TabModel("rawdata", "Raw data (AD)"));
                }
            }
        }
    }
}
