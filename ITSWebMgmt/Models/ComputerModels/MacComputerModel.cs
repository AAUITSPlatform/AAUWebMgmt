using ITSWebMgmt.Connectors;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace ITSWebMgmt.Models
{
    public class MacComputerModel
    {
        public ComputerModel BaseModel { get; set; }
        public JamfConnector Jamf { get; set; } = new JamfConnector();
        public bool ComputerFound { get; set; } = false;
        public TableModel BasicInfoTable { get; set; }
        public TableModel Applications { get; set; }
        public TableModel Plugins { get; set; }
        public TableModel HardwareTable { get; set; }
        public TableModel NetworkTable { get; set; }
        public TableModel LocalAccountsTable { get; set; }
        public TableModel DiskTable { get; set; }
        public TableModel GroupsTable { get; set; }
        public List<string> Groups { get; set; }
        public int FreeSpace { get; set; }
        public string ComputerName { get; set; }
        public int Id { get; set; }

        public MacComputerModel(ComputerModel baseModel)
        {
            BaseModel = baseModel;

            int id = Jamf.GetComputerIdByName(BaseModel.ComputerName);

            if (id != -1) //Computer found
            {
                ComputerFound = true;
                InitViews(id);
            }

            BaseModel.ComputerFound = ComputerFound;
        }

        public MacComputerModel(string computerName)
        {
            int id = Jamf.GetComputerIdByName(computerName);

            if (id != -1) //Computer found
            {
                InitViews(id);
            }
        }
        public MacComputerModel(int id)
        {
            InitViews(id);
        }

        public void InitViews(int id)
        {
            Id = id;
            var jsonString = Jamf.GetAllComputerInformationAsJSONString(id);
            JObject jsonVal = JObject.Parse(jsonString) as JObject;
            ComputerName = jsonVal.SelectToken("computer.general.name").ToString();

            SetHardware(jsonVal);
            SetSoftware(jsonVal);
            SetBasic(jsonVal);
            SetNetwork(jsonVal);
            SetGroups(jsonVal);
            SetLocalAccounts(jsonVal);
            SetDisk(jsonVal);
        }

        private void SetHardware(JObject jsonVal)
        {
            HardwareTable = CreateRawTableFromJamf(jsonVal, "computer.hardware", new List<string>() { "filevault2_users", "storage", "mapped_printers" }, "Hardware info", true);
            //TODO Add disk info til hardware info
            //TODO consider printers
        }

        private void SetSoftware(JObject jsonVal)
        {
            Applications = CreateTableFromJamf(jsonVal, "computer.software.applications", new List<string>() { "name", "version" }, new string[] { "Name", "Version" }, null);
            Plugins = CreateTableFromJamf(jsonVal, "computer.software.plugins", new List<string>() { "name", "version" }, new string[] { "Name", "Version" }, null);
        }

        private void SetNetwork(JObject jsonVal)
        {
            NetworkTable = CreateRawTableFromJamf(jsonVal, "computer.general", new List<string>() { "mac_address", "alt_mac_address", "ip_address", "last_reported_ip" }, "Network info");
        }

        private void SetDisk(JObject jsonVal)
        {
            dynamic token = jsonVal.SelectToken("computer.hardware.storage");
            List<string[]> rows = new List<string[]>();

            List<string> attributeNames = new List<string>() { "disk", "model", "revision" };
            List<string> partitionAttributeNames = new List<string>() { "name", "size", "percentage_full", "filevault_status" };
            List<string> diskInfo = new List<string>();

            foreach (dynamic info in token)
            {
                List<string> rowEntries = new List<string>();
                foreach (var name in attributeNames)
                {
                    diskInfo.Add(info.SelectToken(name).Value.ToString());
                }
                dynamic partitions = info.SelectToken("partitions");
                if (partitions != null)
                {
                    foreach (var partition in partitions)
                    {
                        foreach (var name in partitionAttributeNames)
                        {
                            rowEntries.Add(partition.SelectToken(name).Value.ToString());
                        }

                        if (rowEntries[0] == "Macintosh HD (Boot Partition)")
                        {
                            FreeSpace = (int.Parse(rowEntries[1]) * (100 - int.Parse(rowEntries[2]))) / 102400;
                        }

                        var list = new List<string>(diskInfo);
                        list.AddRange(rowEntries);
                        rows.Add(list.ToArray());
                        rowEntries.Clear();
                    }
                    diskInfo.Clear();
                }
                else
                {
                    var list = new List<string>(diskInfo)
                    {
                        "Partion info not found"
                    };
                    rows.Add(list.ToArray());
                }
            }

            DiskTable = new TableModel(new string[] { "Disk", "Model", "Revision", "Partition name", "Size (MB)", "Percentage full", "Filevault status" }, rows, "Disk info");
        }

        private void SetBasic(JObject jsonVal)
        {
            dynamic token = jsonVal.SelectToken("computer.location");

            List<string[]> rows = new List<string[]>
            {
                new string[] { "Computer type", "Mac" }
            };

            List<string> names = new List<string>() { "email_address"};

            foreach (dynamic info in token)
            {
                if (names.Contains(info.Name))
                {
                    rows.Add(new string[] { info.Name.Replace('_', ' '), info.Value.Value.ToString() });
                }
            }

            token = jsonVal.SelectToken("computer.extension_attributes");
            names = new List<string>() { /*"AAU Computer Type",*/ "AAU-1x Username", "auto-update2", "Battery Health Status" };

            foreach (dynamic info in token)
            {
                dynamic name = info.SelectToken("name");
                if (names.Contains(name.Value))
                {
                    dynamic value = info.SelectToken("value");
                    rows.Add(new string[] { name.Value, value.Value });
                }
            }

            dynamic jamfVersion = jsonVal.SelectToken("computer.general.jamf_version");
            rows.Add(new string[] { "Jamf version", jamfVersion.Value });

            BasicInfoTable = new TableModel(new string[] { "Property name", "Value" }, rows, "Basic info");
        }

        private void SetLocalAccounts(JObject jsonVal)
        {
            LocalAccountsTable = CreateTableFromJamf(jsonVal, "computer.groups_accounts.local_accounts", new List<string>() { "name", "realname", "uid", "home", "home_size", "administrator", "filevault_enabled" }, new string[] { "Name", "Real name", "uid", "Home directory", "Home size", "Administrator", "Filevault enabled" }, "Local accounts");
        }

        private void SetGroups(JObject jsonVal)
        {
            JArray groups = jsonVal.SelectToken("computer.groups_accounts.computer_group_memberships").ToObject<JArray>();
            Groups = groups.Select(x => x.ToString()).ToList();
            GroupsTable = new TableModel(new string[] {"Groups"}, groups.Select(x => new string[] { x.ToString() }).ToList());
        }

        public TableModel CreateRawTableFromJamf(JObject jsonVal, string tokenName, List<string> names, string title, bool skipNames = false)
        {
            dynamic token = jsonVal.SelectToken(tokenName);
            List<string[]> rows = new List<string[]>();

            foreach (dynamic info in token)
            {
                if (names.Contains(info.Name) != skipNames)
                {
                    rows.Add(new string[] { info.Name.Replace('_', ' '), info.Value.Value.ToString() });
                }
            }

            return new TableModel(new string[] { "Property name", "Value" }, rows, title);
        }

        public TableModel CreateTableFromJamf(JObject jsonVal, string tokenName, List<string> attributeNames, string[] headings, string title)
        {
            dynamic token = jsonVal.SelectToken(tokenName);
            List<string[]> rows = new List<string[]>();

            foreach (dynamic info in token)
            {
                List<string> rowEntries = new List<string>();
                foreach (var name in attributeNames)
                {
                    rowEntries.Add(info.SelectToken(name).Value.ToString());
                }
                rows.Add(rowEntries.ToArray());
            }

            return new TableModel(headings, rows, title);
        }
    }
}
