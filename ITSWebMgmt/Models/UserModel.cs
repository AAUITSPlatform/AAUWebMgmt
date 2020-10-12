using ITSWebMgmt.Caches;
using ITSWebMgmt.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using ITSWebMgmt.Helpers;
using System.Web;
using Microsoft.Exchange.WebServices.Data;
using ITSWebMgmt.Connectors;
using static ITSWebMgmt.Connectors.NetaaudkConnector;
using ITSWebMgmt.WebMgmtErrors;

namespace ITSWebMgmt.Models
{
    public class UserModel : WebMgmtModel<UserADCache>
    {
        public List<TabModel> Tabs = new List<TabModel>();
        public string Guid { get => new Guid((byte[])(ADCache.GetProperty("objectGUID"))).ToString(); }
        public string UserPrincipalName { get => ADCache.GetProperty("userPrincipalName"); }
        public string DisplayName { get => ADCache.GetProperty("displayName"); }
        public string[] ProxyAddresses
        {
            get
            {
                var temp = ADCache.GetProperty("proxyAddresses");
                return temp.GetType().Equals(typeof(string)) ? (new string[] { temp }) : temp;
            }
        }
        public int UserAccountControlComputed { get => ADCache.GetProperty("msDS-User-Account-Control-Computed"); }
        public int UserAccountControl { get => ADCache.GetProperty("userAccountControl"); }
        public string UserPasswordExpiryTimeComputed { get => ADCache.GetProperty("msDS-UserPasswordExpiryTimeComputed"); }
        public string GivenName { get => ADCache.GetProperty("givenName"); }
        public string SN { get => ADCache.GetProperty("sn"); }
        public string AAUStaffID { get => ADCache.GetProperty("aauStaffID").ToString(); }
        public string AAUStudentID { get => ADCache.GetProperty("aauStudentID").ToString(); }
        public object Profilepath { get => ADCache.GetProperty("profilepath"); }
        public string AAUUserClassification { get => ADCache.GetProperty("aauUserClassification"); }
        public string AAUUserStatus { get => ADCache.GetProperty("aauUserStatus").ToString(); }
        public string ScriptPath { get => ADCache.GetProperty("scriptPath"); }
        public bool IsAccountLocked { get => ADCache.GetProperty("IsAccountLocked"); }
        public int BadPwdCount { get => ADCache.GetProperty("badPwdCount"); }
        public string AAUAAUID { get => ADCache.GetProperty("aauAAUID"); }
        public string AAUUUID { get => ADCache.GetProperty("aauUUID"); }
        public string TelephoneNumber { get => ADCache.GetProperty("telephoneNumber"); set => ADCache.SaveProperty("telephoneNumber", value); }
        public string LastLogon { get => ADCache.GetProperty("lastLogon"); }
        public string Manager { get => ADCache.GetProperty("manager"); }
        public string DistinguishedName { get => ADCache.GetProperty("distinguishedName"); }
        public ManagementObjectCollection GetUserMachineRelationshipFromUserName(string userName) => SCCMCache.GetUserMachineRelationshipFromUserName(userName);
        public List<WindowsComputerModel> GetManagedWindowsComputers() {
            List<WindowsComputerModel> managedComputerList = new List<WindowsComputerModel>();

            if (UserPrincipalName != "")
            {
                string[] upnsplit = UserPrincipalName.Split('@');
                string domain = upnsplit[1].Split('.')[0];

                string formattedName = string.Format("{0}\\\\{1}", domain, upnsplit[0]);

                foreach (ManagementObject o in GetUserMachineRelationshipFromUserName(formattedName))
                {
                    string machineName = o.Properties["ResourceName"].Value.ToString();
                    WindowsComputerModel model = new WindowsComputerModel(machineName);
                    if (!model.ComputerFound)
                    {
                        model = new WindowsComputerModel(model.ComputerName);
                    }

                    if (model.BaseModel.IsInAD)
                    {
                        managedComputerList.Add(model);
                    }   
                }
            }
            
            return managedComputerList;
        }

        public bool UserFound = false;

        public string[] GetUserInfo()
        {
            return new string[]
            {
                BasicInfoDepartmentPDS,
                BasicInfoOfficePDS,
                BasicInfoADFSLocked,
                BasicInfoLocked,
                BasicInfoPasswordExpireDate,
                BasicInfoPasswordExpired,
                BadPwdCount.ToString(),
                UserPrincipalName,
                AAUAAUID,
                AAUUUID,
                AAUUserStatus,
                AAUStaffID,
                AAUStudentID,
                AAUUserClassification,
                TelephoneNumber,
                LastLogon
            };
        }

        public string BasicInfoDepartmentPDS { get; set; }
        public string BasicInfoOfficePDS { get; set; }
        public string BasicInfoADFSLocked { get; set; }
        public string BasicInfoLocked { get; set; }
        public string BasicInfoPasswordExpireDate { get; set; }
        public string BasicInfoPasswordExpired { get; set; }
        public TableModel BasicInfoTable { get; set; }
        public string BasicInfoRomaing
        {
            get
            {
                return (Profilepath != null).ToString();
            }
        }
        public GetUserAvailabilityResults CalInfo { get; set; }
        public string CalAgendaStatus { get; set; }
        public ServiceManagerModel ServiceManager { get; set; }
        public string ErrorMessages { get; set; }
        public string ResultError { get; set; }
        public WebMgmtErrorList ErrorList { get; set; }
        public string UserName { get; set; } = "mhsv16@its.aau.dk";
        public string ErrorCountMessage { get; set; }
        public string SCSMUserID { get; set; }
        public TableModel Windows7to10 { get; set; }
        public TableModel WindowsComputerTable { get; set; }
        public TableModel MacComputerTable { get; set; }
        public bool ShowFixUserOU { get; set; } = false;
        public bool ShowLoginScript { get; set; } = false;
        public string UsesOnedrive { get; set; } = "False";
        public List<NetaaudkModel> Netaaudk { get; set; }
        public bool IsDisabled
        {
            get
            {
                const int ufAccountDisable = 0x0002;
                return (UserAccountControl & ufAccountDisable) == ufAccountDisable;
            }
        }

        public UserModel(string ADPath, string test)
        {
            Init(ADPath, false);
        }

        public UserModel(string username, bool loadDataInbackground = true)
        {
            string ADPath = null;
            if (username != null)
            {
                UserName = username;
                ADPath = ADHelper.GetADPath(username);
            }
            if (ADPath != null)
            {
                Init(ADPath, loadDataInbackground);
            }
            else
            {
                if (username != null)
                {
                    ResultError = $"User ({username}) Not found";
                }
            }
        }

        public void Init(string ADPath, bool loadDataInbackground = true)
        {
            ADCache = new UserADCache(ADPath);
            SCCMCache = new SCCMCache();
            UserFound = true;
            if (loadDataInbackground)
            {
                LoadDataInbackground();
            }
        }

        public void SetTabs()
        {
            Tabs.Add(new TabModel("servicemanager", "Service Manager", true));
            Tabs.Add(new TabModel("calAgenda", "Calendar, Currently: " + CalAgendaStatus));
            Tabs.Add(new TabModel("computerInformation", "Computer Information"));
            Tabs.Add(new TabModel("win7to10", "Windows 7 to 10 upgrade", true));
            Tabs.Add(new TabModel("netaaudk", "Net.aau.dk"));
            Tabs.Add(new TabModel("groups", "Groups"));
            Tabs.Add(new TabModel("fileshares", "Fileshares"));
            Tabs.Add(new TabModel("exchange", "Exchange Resources"));
            Tabs.Add(new TabModel("print", "Print"));
            Tabs.Add(new TabModel("rawdata", "Raw Data"));
            Tabs.Add(new TabModel("tasks", "Tasks"));
            Tabs.Add(new TabModel("warnings", "Warnings"));
        }

        #region loading data
        /*
         * Remember the disclaimer in the documentation about some guy that wrote the legacy code in the weirdest possible manner?
         * This is one of those cases where it applies. How anyone thought it would be a good idea to aquire data, trim and prepare it,
         * as well as actually serving it in the same function is beyond my understanding. Why make clearly defined functions and fields,
         * when you can make super compact code that is impossible to extend? If anything good is to be said about the following pieces of code,
         * it would probably be, that it is a prime example of what NOT to do when making a well-typed program.
         * 
         * Should any of you brave souls that get the duty of maintaining this abomination feel the need to change this, please do.
         * It should be wiped off any harddrive that has seen the ugly underbelly of the software business. If you are not up to the task,
         * I don't blame you. It makes me ill just looking at it too.
             */
        public void InitBasicInfo()
        {
            //lblbasicInfoOfficePDS
            if (AAUStaffID != null)
            {
                string empID = AAUStaffID;

                try
                {
                    var pds = new PureConnector(empID);
                    BasicInfoDepartmentPDS = pds.Department;
                    BasicInfoOfficePDS = pds.OfficeAddress;
                }
                catch (Exception)
                {
                    BasicInfoDepartmentPDS = "Error: Failed to connect to Pure";
                    BasicInfoOfficePDS = "Error: Failed to connect to Pure";
                }
            }

            //Password info
            BasicInfoLocked = "False";
            const int UF_LOCKOUT = 0x0010;
            if ((UserAccountControlComputed & UF_LOCKOUT) == UF_LOCKOUT)
            {
                BasicInfoLocked = "True";
            }

            if (UserPasswordExpiryTimeComputed == "")
            {
                BasicInfoPasswordExpireDate = "Never";
                BasicInfoPasswordExpired = "False";
            }
            else
            {
                BasicInfoPasswordExpireDate = UserPasswordExpiryTimeComputed;
                if (UserPasswordExpiryTimeComputed != null)
                {
                    BasicInfoPasswordExpired = DateTime.Parse(UserPasswordExpiryTimeComputed) > DateTime.Now ? "False" : "True";
                }
            }

            //Other fileds
            var attrDisplayName = "Department (Pure), Office (Pure), ADFS locked, Account locked, Password Expire Date, Password expired, Bad password count, UserName, AAU-ID, AAU-UUID, UserStatus, StaffID, StudentID, UserClassification, Telephone, LastLogon (approx.)";
            var attrArry = GetUserInfo();
            var dispArry = attrDisplayName.Split(',');

            List<string[]> rows = new List<string[]>();

            for (int i = 0; i < attrArry.Length; i++)
            {
                string name = dispArry[i].Trim();
                string val = attrArry[i];

                rows.Add(new string[] { name, val });
            }

            //OneDrive
            UsesOnedrive = OneDriveHelper.DoesUserUseOneDrive(this);
            rows.Add(new string[] { "Uses OneDrive?", UsesOnedrive });

            BasicInfoTable = new TableModel(null, rows);
        }
        public List<string> GetUserMails(){
            List<string> emails = new List<string>();
            foreach (string s in ProxyAddresses)
            {
                if (IsAnEmail(s))
                {
                    emails.Add(s.ToLower().Replace("smtp:", ""));
                }
            }
            return emails;
        }
        private bool IsAnEmail(string s)
        {
            return s.StartsWith("SMTP:", StringComparison.CurrentCultureIgnoreCase);
        }

        public void InitCalendarAgenda()
        {
            CalAgendaStatus = "Free";

            try
            {
                CalInfo = GetFreeBusyResultsAsync(this).Result;

                DateTime now = DateTime.Now;
                foreach (AttendeeAvailability availability in CalInfo.AttendeesAvailability)
                {
                    foreach (CalendarEvent calendarItem in availability.CalendarEvents)
                    {
                        if (calendarItem.FreeBusyStatus != LegacyFreeBusyStatus.Free)
                        {
                            if (now > calendarItem.StartTime && calendarItem.EndTime > now)
                            {
                                CalAgendaStatus = calendarItem.FreeBusyStatus.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                CalAgendaStatus = "Unknown";
            }
        }

        public static async Task<GetUserAvailabilityResults> GetFreeBusyResultsAsync(UserModel UserModel)
        {
            ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2010_SP2)
            {
                UseDefaultCredentials = true, // Use domain account for connecting 
                Url = new Uri("https://mail.aau.dk/EWS/exchange.asmx")
            };

            List<AttendeeInfo> attendees = new List<AttendeeInfo>();

            string address = UserModel.UserPrincipalName;
            if (UserModel.UserPrincipalName == "")
            {
                address = UserModel.UserName;
            }

            attendees.Add(new AttendeeInfo()
            {
                SmtpAddress = address,
                AttendeeType = MeetingAttendeeType.Organizer
            });

            // Specify availability options.
            AvailabilityOptions myOptions = new AvailabilityOptions
            {
                MeetingDuration = 30,
                RequestedFreeBusyView = FreeBusyViewType.FreeBusy
            };

            // Return a set of free/busy times.
            DateTime dayBegin = DateTime.Now.Date;
            var window = new TimeWindow(dayBegin, dayBegin.AddDays(1));
            return await service.GetUserAvailability(attendees, window, AvailabilityData.FreeBusy, myOptions);
        }

        public void InitComputerInformation()
        {
            try
            {
                var windowsComputers = GetManagedWindowsComputers();

                if (windowsComputers.Count != 0)
                {
                    List<string[]> rows = new List<string[]>();
                    List<string[]> linkRows = new List<string[]>();

                    foreach (WindowsComputerModel m in windowsComputers)
                    {
                        string OnedriveWarning = "";
                        try
                        {
                            if (UsesOnedrive.Contains("True") && !OneDriveHelper.ComputerUsesOneDrive(m.ADCache))
                            {
                                OnedriveWarning = " (NOT USING ONEDRIVE!)";
                            }
                        }
                        catch (System.Runtime.InteropServices.COMException)
                        {
                            // Does get an unknown error (0x80005000) when computer is found in SCCM, but not in AD
                        }

                        var fjernsupportLink = "https://support.its.aau.dk/api/client_script?type=rep&operation=generate&action=start_pinned_client_session&client.hostname=" + m.ComputerName;
                        rows.Add(new string[] { m.ComputerName + OnedriveWarning, "Start" });
                        linkRows.Add(new string[] { "/Computer?computername=" + m.ComputerName, fjernsupportLink });
                    }

                    WindowsComputerTable = new TableModel(new string[] { "Computer name", "AAU Fjernsupport" }, rows, new int[] { 0, 1 }, linkRows, "Windows computers");
                }
                else
                {
                    WindowsComputerTable = new TableModel("User do not have any Windows computer", "Windows computers");
                }

                JamfConnector jamf = new JamfConnector();
                List<string> macComputers = new List<string>();
                foreach (var email in GetUserMails())
                {
                    macComputers.AddRange(jamf.GetComputerNamesForUserWith1X(email));
                }

                if (macComputers.Count != 0)
                {
                    List<string[]> rows = new List<string[]>();
                    List<string[]> linkRows = new List<string[]>();

                    foreach (string computername in macComputers)
                    {
                        rows.Add(new string[] { computername });
                        linkRows.Add(new string[] { "/Computer?computername=" + computername });
                    }

                    MacComputerTable = new TableModel(new string[] { "Computer name" }, rows, new int[] { 0 }, linkRows, "Mac computers");
                }
                else
                {
                    MacComputerTable = new TableModel("User do not have any Mac computer", "Mac computers");
                }
            }
            catch (UnauthorizedAccessException)
            {
                WindowsComputerTable = null;
                MacComputerTable = null;
            }
        }

        public PartialGroupModel InitExchange()
        {
            PartialGroupModel model = new PartialGroupModel(ADCache, "memberOf");

            var members = model.GroupAllList;
            if (members.Count == 0)
            {
                members = model.GroupList;
            }

            List<string[]> rows = new List<string[]>();
            List<string[]> linkRows = new List<string[]>();

            //Select Exchange groups and convert to list of ExchangeMailboxGroup
            var exchangeMailboxGroupList = members.Where<string>(group => (group.StartsWith("CN=MBX_"))).Select(x => new ExchangeMailboxGroupModel(x));

            foreach (ExchangeMailboxGroupModel e in exchangeMailboxGroupList)
            {
                var type = e.Type;
                var domain = e.Domain;
                var link = string.Format("/Group?grouppath={0}", HttpUtility.UrlEncode("LDAP://" + e.RawValue));
                var access = e.Access;
                rows.Add(new string[] { type, domain, e.Name, access });
                linkRows.Add(new string[] { link });
            }

            model.FilteredTable = new TableModel(new string[] { "Type", "Domain", "Name", "Access" }, rows, new int[] { 2 }, linkRows);

            return model;
        }

        public PartialGroupModel InitFileshares()
        {
            PartialGroupModel model = new PartialGroupModel(ADCache, "memberOf");
            var members = model.GroupAllList;

            if (members.Count == 0)
            {
                members = model.GroupList;
            }
            List<string[]> rows = new List<string[]>();
            List<string[]> linkRows = new List<string[]>();

            //Filter fileshare groups and convert to Fileshare Objects
            var fileshareList = members.Where((string value) =>
            {
                return GroupController.IsFileShare(value);
            }).Select(x => new FileshareModel(x));

            foreach (FileshareModel f in fileshareList)
            {
                var nameWithLink = string.Format("/Group?grouppath={0}", HttpUtility.UrlEncode("LDAP://" + f.Fileshareraw));
                rows.Add(new string[] { f.Type, f.Domain, f.Name, f.Access });
                linkRows.Add(new string[] { nameWithLink});
            }

            model.FilteredTable = new TableModel(new string[] { "Type", "Domain", "Name", "Access" }, rows, new int[] { 2 }, linkRows);

            return model;
        }

        public void InitWin7to10()
        {
            // Should be deleted after Windows 7 is gone
            // No reason to refactor
            bool haveWindows7 = false;
            List<string[]> rows = new List<string[]>();

            foreach (WindowsComputerModel m in GetManagedWindowsComputers())
            {
                m.SetConfig();
                string upgradeButton = "";
                if (m.ConfigPC.Equals("AAU7 PC") || m.ConfigPC.Equals("Administrativ7 PC"))
                {
                    var name = "<a href=\"/Computer?computername=" + m.ComputerName + "\">" + m.ComputerName + "</a><br/>";
                    upgradeButton = "<input type=\"button\" value=\"Create Win7 to 10 SR\" onclick=\"submitform('" + m.ComputerName + "');\"/>";
                    rows.Add(new string[] { name, upgradeButton });
                    haveWindows7 = true;
                }
            }

            if (haveWindows7)
            {
                var scsm = new SCSMConnector();
                _ = scsm.GetUUID(UserPrincipalName, GetUserMails()).Result;
                SCSMUserID = scsm.userID;

                Windows7to10 = new TableModel(new string[] { "Computername", "Windows 7 to 10 upgrade" }, rows);
            }
            else
            {
                Windows7to10 = new TableModel("User do not have any Windows 7 PCs");
            }
        }

        public TableModel InitNetaaudk()
        {
            Netaaudk = new NetaaudkConnector().GetData(UserPrincipalName);

            if (Netaaudk.Count != 0)
            {
                List<string[]> rows = new List<string[]>();

                foreach (var item in Netaaudk)
                {
                    string created_at = DateTimeConverter.Convert(item.created_at);
                    string first_used = item.first_use == null ? "Never used" : DateTimeConverter.Convert(item.first_use);
                    string last_used = item.last_used == null ? "Never used" : DateTimeConverter.Convert(item.last_used);
                    rows.Add(new string[] {created_at, first_used, last_used, item.mac_address, item.name, item.devicetype, item.id});
                }

                return new TableModel(new string[] { "Created at", "First use", "last used", "Mac address", "Device name", "Device type", "ID" }, rows, "Net.aau.dk");
            }
            else
            {
                return new TableModel("User have not used Net.aau.dk", "Net.aau.dk");
            }
        }

        #endregion loading data
    }
}
