using ITSWebMgmt.Caches;
using ITSWebMgmt.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ITSWebMgmt.WebMgmtErrors;

namespace ITSWebMgmt.Models
{
    public class UserModel : WebMgmtModel<UserADcache>
    {
        public UserController user;
        public string Guid { get => new Guid((byte[])(ADcache.getProperty("objectGUID"))).ToString(); }
        public string UserPrincipalName { get => ADcache.getProperty("userPrincipalName"); }
        public string DisplayName { get => ADcache.getProperty("displayName"); }
        public string[] ProxyAddresses
        {
            get
            {
                var temp = ADcache.getProperty("proxyAddresses");
                return temp.GetType().Equals(typeof(string)) ? (new string[] { temp }) : temp;
            }
        }
        public int UserAccountControlComputed { get => ADcache.getProperty("msDS-User-Account-Control-Computed"); }
        public int UserAccountControl { get => ADcache.getProperty("userAccountControl"); }
        public string UserPasswordExpiryTimeComputed { get => ADcache.getProperty("msDS-UserPasswordExpiryTimeComputed"); }
        public string GivenName { get => ADcache.getProperty("givenName"); }
        public string SN { get => ADcache.getProperty("sn"); }
        public string AAUStaffID { get => ADcache.getProperty("aauStaffID").ToString(); }
        public string AAUStudentID { get => ADcache.getProperty("aauStudentID").ToString(); }
        public object Profilepath { get => ADcache.getProperty("profilepath"); }
        public string AAUUserClassification { get => ADcache.getProperty("aauUserClassification"); }
        public string AAUUserStatus { get => ADcache.getProperty("aauUserStatus").ToString(); }
        public string ScriptPath { get => ADcache.getProperty("scriptPath"); }
        public bool IsAccountLocked { get => ADcache.getProperty("IsAccountLocked"); }
        public string AAUAAUID { get => ADcache.getProperty("aauAAUID"); }
        public string AAUUUID { get => ADcache.getProperty("aauUUID"); }
        public string TelephoneNumber { get => ADcache.getProperty("telephoneNumber"); set => ADcache.saveProperty("telephoneNumber", value); }
        public string LastLogon { get => ADcache.getProperty("lastLogon"); }
        public string DistinguishedName { get => ADcache.getProperty("distinguishedName"); }
        public ManagementObjectCollection getUserMachineRelationshipFromUserName(string userName) => SCCMcache.getUserMachineRelationshipFromUserName(userName);

        public string[] getUserInfo()
        {
            return new string[]
            {
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

        public string InfoAdmDBExpireDate;
        public string BasicInfoDepartmentPDS;
        public string BasicInfoOfficePDS;
        public string BasicInfoPasswordExpired;
        public string BasicInfoPasswordExpireDate;
        public string BasicInfoTable;
        public string BasicInfoRomaing;
        public string Print;
        public string CalAgenda;
        public string ServiceManager;
        public string ComputerInformation;
        public string Loginscript;
        public string Rawdata;
        public string ErrorMessages;
        public string ResultError;
        public string UserName = "mhsv16@its.aau.dk";
        public string ErrorCountMessage;
        public bool ShowResultDiv = false;
        public bool ShowErrorDiv = false;
        public bool ShowFixUserOU = false;
        public bool ShowLoginScript = false;

        public UserModel(UserController controller, string username, string adpath)
        {
            user = controller;
            user.UserModel = this;
            if (username != null)
            {
                UserName = username;
            }
            if (adpath != null)
            {
                ADcache = new UserADcache(adpath);
                SCCMcache = new SCCMcache();
                ShowResultDiv = true;
                ShowErrorDiv = false;
                LoadWarnings();
                LoadDataInbackground();
            }
            else
            {
                ResultError = "User Not found";
                ShowResultDiv = false;
                ShowErrorDiv = true;
            }
        }

        private void LoadWarnings()
        {
            List<WebMgmtError> errors = new List<WebMgmtError>
            {
                new UserDisabled(user),
                new UserLockedDiv(user),
                new PasswordExpired(user),
                new MissingAAUAttr(user),
                new NotStandardOU(user)
            };

            var errorList = new WebMgmtErrorList(errors);
            ErrorCountMessage = errorList.getErrorCountMessage();
            ErrorMessages = errorList.ErrorMessages;

            if (user.userIsInRightOU())
            {
                ShowFixUserOU = false;
            }
            //Password is expired and warning before expire (same timeline as windows displays warning)
        }
    }
}
