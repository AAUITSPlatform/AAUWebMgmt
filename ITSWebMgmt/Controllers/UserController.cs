using ITSWebMgmt.Connectors;
using ITSWebMgmt.Helpers;
using ITSWebMgmt.Models;
using ITSWebMgmt.Models.Log;
using ITSWebMgmt.WebMgmtErrors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ITSWebMgmt.Controllers
{
    public class UserController : DynamicLoadingWebMgmtController
    {
        public IActionResult Index(string username)
        {
            UserModel = GetUserModel(username);

            if (username != null)
            {
                if (UserModel.UserFound)
                {
                    new Logger(_context).Log(LogEntryType.UserLookup, HttpContext.User.Identity.Name, UserModel.UserPrincipalName, true);
                }
                else
                {
                    new Logger(_context).Log(LogEntryType.UserLookup, HttpContext.User.Identity.Name, username + " (Not found)", true);
                }
            }

            return View(UserModel);
        }

        private readonly IMemoryCache _cache;
        public UserModel UserModel;

        public UserController(LogEntryContext context, IMemoryCache cache) : base(context)
        {
            _cache = cache;
        }

        private UserModel GetUserModel(string username)
        {
            if (username != null)
            {
                username = ADHelper.NormalizeUsername(username);

                if (!_cache.TryGetValue(username, out UserModel))
                {
                    username = username.Trim();
                    UserModel = new UserModel(username);

                    if (UserModel.UserFound)
                    {
                        try
                        {
                            UserModel.BasicInfoADFSLocked = new SplunkConnector(_cache).IsAccountADFSLocked(UserModel.UserPrincipalName);
                        }
                        catch (Exception e)
                        {
                            UserModel.BasicInfoADFSLocked = "Error: Could not connect to Splunk";
                            HandleError(e);
                        }
                        UserModel.InitBasicInfo();
                        LoadWarnings();
                        UserModel.InitCalendarAgenda();
                        UserModel.SetTabs();
                        var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5));
                        _cache.Set(username, UserModel, cacheEntryOptions);
                    }
                }
            }
            else
            {
                UserModel = new UserModel(null);
            }

            return UserModel;
        }

        public bool UserIsInRightOU()
        {
            string dn = UserModel.DistinguishedName;
            string[] dnarray = dn.Split(',');

            string[] ou = dnarray.Where(x => x.StartsWith("ou=", StringComparison.CurrentCultureIgnoreCase)).ToArray();

            int count = ou.Count();
            if (count < 2)
            {
                return false;
            }
            //Check root is people
            if (!(ou[count - 1]).Equals("ou=people", StringComparison.CurrentCultureIgnoreCase))
            {
                //Error user is not placed in people!!!!! Cant move the user (might not be a real user or admin or computer)
                return false;
            }
            string[] okplaces = new string[3] { "ou=staff", "ou=guests", "ou=students" };
            if (!okplaces.Contains(ou[count - 2], StringComparer.OrdinalIgnoreCase))
            {
                //Error user is not in out staff, people or student, what is gowing on here?
                return false;
            }
            if (count > 2)
            {
                return false;
            }
            return true;
        }

        public ActionResult FixUserOu([FromBody]string username)
        {
            UserModel = GetUserModel(username);
            if (UserIsInRightOU()) { return Error(); }

            //See if it can be fixed!
            string dn = UserModel.DistinguishedName;
            string[] dnarray = dn.Split(',');

            string[] ou = dnarray.Where(x => x.StartsWith("ou=", StringComparison.CurrentCultureIgnoreCase)).ToArray();

            int count = ou.Count();

            if (count < 2)
            {
                return Error("This cant be in people/{staff,student,guest}");
            }
            //Check root is people
            if (!(ou[count - 1]).Equals("ou=people", StringComparison.CurrentCultureIgnoreCase))
            {
                return Error("Error user is not placed in people!!!!! Cant move the user (might not be a real user or admin or computer)");
            }
            string[] okplaces = new string[3] { "ou=staff", "ou=guests", "ou=students" };
            if (!okplaces.Contains(ou[count - 2], StringComparer.OrdinalIgnoreCase))
            {
                return Error("Error user is not in out staff, people or student, what is gowing on here?");
            }
            if (count > 2)
            {
                //User is not placed in people/{staff,student,guest}, but in a sub ou, we need to do somthing!
                //from above check we know the path is people/{staff,student,guest}, lets generate new OU

                //Format ldap://DOMAIN/pathtoOU
                //return false; //XX Return false here?

                string[] ADPathsplit = UserModel.ADPath.ToLower().Replace("ldap://", "").Split('/');
                string protocol = "LDAP://";
                string domain = ADPathsplit[0];
                string[] dcpath = (ADPathsplit[0].Split(',')).Where<string>(s => s.StartsWith("dc=", StringComparison.CurrentCultureIgnoreCase)).ToArray<string>();

                string newOU = string.Format("{0},{1}", ou[count - 2], ou[count - 1]);
                string newPath = string.Format("{0}{1}/{2},{3}", protocol, string.Join(".", dcpath).Replace("dc=", ""), newOU, string.Join(",", dcpath));

                new Logger(_context).Log(LogEntryType.UserMoveOU, HttpContext.User.Identity.Name, new List<string>() {newPath, UserModel.ADPath });

                var newLocaltion = DirectoryEntryCreator.CreateNewDirectoryEntry(newPath);
                UserModel.ADCache.DE.MoveTo(newLocaltion);

                return Success();
            }
            //We don't need to do anything, user is placed in the right ou! (we think, can still be in wrong ou fx a guest changed to staff, we cant check that here) 
            return Success($"no need to change user {UserModel.ADPath} out, all is good");
        }

        public ActionResult UnlockUserAccount([FromBody]string username)
        {
            UserModel = GetUserModel(username);
            new Logger(_context).Log(LogEntryType.UnlockUserAccount, HttpContext.User.Identity.Name, UserModel.ADPath);

            try
            {
                UserModel.ADCache.DE.Properties["LockOutTime"].Value = 0; //unlock account
                UserModel.ADCache.DE.CommitChanges(); //may not be needed but adding it anyways
                UserModel.ADCache.DE.Close();
            }
            catch (UnauthorizedAccessException e)
            {
                return Error(e.Message);
            }
            catch (Exception e)
            {
                return Error(e.Message);
            }

            return Success();
        }

        public ActionResult ToggleUserprofile([FromBody]string username)
        {
            UserModel = GetUserModel(username);
            //XXX log what the new value of profile is :)
            new Logger(_context).Log(LogEntryType.ToggleUserProfile, HttpContext.User.Identity.Name, UserModel.ADPath);

            //string profilepath = (string)(ADCache.DE.Properties["profilePath"])[0];

            try
            {
                if (UserModel.ADCache.DE.Properties.Contains("profilepath"))
                {
                    UserModel.ADCache.DE.Properties["profilePath"].Clear();
                    UserModel.ADCache.DE.CommitChanges();
                }
                else
                {
                    string upn = UserModel.UserPrincipalName;
                    var tmp = upn.Split('@');

                    string path = string.Format("\\\\{0}\\profiles\\{1}", tmp[1], tmp[0]);

                    UserModel.ADCache.DE.Properties["profilePath"].Add(path);
                    UserModel.ADCache.DE.CommitChanges();
                }
            }
            catch (UnauthorizedAccessException e)
            {
                return Error(e.Message);
            }

            //Set value
            //DirectoryEntry de = result.GetDirectoryEntry();
            //de.Properties["TelephoneNumber"].Clear();
            //de.Properties["employeeNumber"].Value = "123456789";
            //de.CommitChanges();

            return Success();
        }

        [HttpPost]
        public void CreateNewIRSR(string userPrincipalName, string sCSMUserID)
        {
            Response.Redirect("/CreateWorkItem?userPrincipalName=" + userPrincipalName + "&userID=" + sCSMUserID);
        }

        //ingen camel-case fordi asp.net er træls
        public void CreateWin7UpgradeFormsForUserPCs(string userPrincipalName, string computerName, string sCSMUserID)
        {
            Response.Redirect("/CreateWorkItem/Win7Index?userPrincipalName=" + userPrincipalName + "&computerName=" + computerName + "&userID=" + sCSMUserID);
        }

        private void LoadWarnings()
        {
            List<WebMgmtError> errors = new List<WebMgmtError>
            {
                new UserDisabled(this),
                new UserLockedDiv(this),
                new AccountLocked(this),
                new MissingAAUAttr(this),
                new NotStandardOU(this),
                new ADFSLocked(this)
            };

            var errorList = new WebMgmtErrorList(errors);
            UserModel.ErrorList = errorList;

            if (!UserIsInRightOU())
            {
                UserModel.ShowFixUserOU = true;
            }
            //Password is expired and warning before expire (same timeline as windows displays warning)
        }

        public ActionResult GetUsersByName([FromBody]string name)
        {
            List<string> names = new List<string>();
            try
            {
                names = new PureConnector().GetUsersByName(name);
            }
            catch (Exception)
            {
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = true, names });
        }

        private bool CaseNumberOk(string caseNumber)
        {
            if (caseNumber.Length < 3)
            {
                return false;
            }

            string firstTwoLetters = caseNumber.Substring(0, 2).ToUpper();

            if (firstTwoLetters != "IR" && firstTwoLetters != "SR")
            {
                return false;
            }

            return true;
        }

        public ActionResult SetupOnedrive([FromBody]string data)
        {
            string[] temp = data.Split('|');
            UserModel = GetUserModel(temp[0]);

            if (temp[0].Length == 0 || temp[1].Length == 0 || temp[2].Length == 0)
            {
                return Error("Fields cannot be empty");
            }

            if(!CaseNumberOk(temp[2]))
            {
                return Error("Case number is on a wrong format");
            }

            if (UserModel.AAUUserClassification == "guest")
            {
                return Error("The user is a guest, and guests do not have an Office 365 licence and can therefore not be setup to use Onedrive");
            }

            WindowsComputerModel computerModel = new WindowsComputerModel(temp[1]);

            if (computerModel.ComputerFound)
            {
                ADHelper.AddMemberToGroup(UserModel.DistinguishedName, "LDAP://CN=GPO_User_DenyFolderRedirection,OU=Group Policies,OU=Groups,DC=aau,DC=dk");
                ADHelper.AddMemberToGroup(computerModel.DistinguishedName, "LDAP://CN=GPO_Computer_UseOnedriveStorage,OU=Group Policies,OU=Groups,DC=aau,DC=dk");

                new Logger(_context).Log(LogEntryType.Onedrive, HttpContext.User.Identity.Name, new List<string>() { UserModel.UserPrincipalName, computerModel.ComputerName, temp[2] });

                return Success("User and computer added to groups");
            }
            else
            {
                return Error("Computer not found");
            }
        }

        public ActionResult DisableADUser([FromBody]string data)
        {
            string[] temp = data.Split('|');
            UserModel = GetUserModel(temp[0]);

            if (temp[0].Length == 0 || temp[1].Length == 0 || temp[2].Length == 0)
            {
                return Error("Fields cannot be empty");
            }

            if (!CaseNumberOk(temp[2]))
            {
                return Error("Case number is on a wrong format");
            }

            if (ADHelper.DisableUser(UserModel.ADPath))
            {
                new Logger(_context).Log(LogEntryType.DisabledAdUser, HttpContext.User.Identity.Name, new List<string>() { UserModel.UserPrincipalName, temp[1], temp[2] });

                return Success("User disabled in AD");
            }
            else
            {
                return Error("User could not be disabled");
            }
        }

        public ActionResult EnableADUser([FromBody]string data)
        {
            string[] temp = data.Split('|');
            UserModel = GetUserModel(temp[0]);

            if (temp[0].Length == 0 || temp[1].Length == 0 || temp[2].Length == 0)
            {
                return Error("Fields cannot be empty");
            }

            if (!CaseNumberOk(temp[2]))
            {
                return Error("Case number is on a wrong format");
            }

            if (temp[1] != "true")
            {
                return Error("Do the required actions first");
            }

            if (ADHelper.EnableUser(UserModel.ADPath))
            {
                new Logger(_context).Log(LogEntryType.EnabledAdUser, HttpContext.User.Identity.Name, new List<string>() { UserModel.UserPrincipalName, temp[2] });

                return Success("User enabled in AD");
            }
            else
            {
                return Error("User could not be enabled");
            }
        }

        public override ActionResult LoadTab(string tabName, string name)
        {
            UserModel = GetUserModel(name);

            new Logger(_context).Log(LogEntryType.LoadedTabUser, HttpContext.User.Identity.Name, new List<string>() { tabName, UserModel.UserPrincipalName }, true);

            PartialGroupModel model = null;

            string viewName = tabName;
            switch (tabName)
            {
                case "basicinfo":
                    viewName = "BasicInfo";
                    UserModel.InitBasicInfo();
                    break;
                case "groups":
                    viewName = "Groups";
                    model = new PartialGroupModel(UserModel.ADCache, "memberOf");
                    break;
                case "tasks":
                    viewName = "Tasks";
                    break;
                case "warnings":
                    return PartialView("WebMgmtErrors/Index", UserModel.ErrorList);
                case "fileshares":
                    model = UserModel.InitFileshares();
                    return PartialView("ExchangeFileshare", model);
                case "calAgenda":
                    UserModel.InitCalendarAgenda();
                    return PartialView("Calendar", UserModel);
                case "exchange":
                    model = UserModel.InitExchange();
                    return PartialView("ExchangeFileshare", model);
                case "servicemanager":
                    viewName = "ServiceManager";
                    break;
                case "computerInformation":
                    UserModel.InitComputerInformation();
                    return PartialView("ComputerInfo", UserModel);
                case "win7to10":
                    viewName = "Win7to10";
                    UserModel.InitWin7to10();
                    break;
                case "print":
                    return PartialView("Print", new PrintConnector(UserModel.Guid.ToString()).GetData());
                case "rawdata":
                    return PartialView("Rawtable", UserModel.ADCache.GetAllProperties());
                case "netaaudk":
                    return PartialView("TableView", UserModel.InitNetaaudk());
            }

            return model != null ? PartialView(viewName, model) : PartialView(viewName, UserModel);
        }
    }
}
