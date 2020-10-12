using ITSWebMgmt.Controllers;

namespace ITSWebMgmt.WebMgmtErrors
{
    public abstract class UserWebMgmtError : WebMgmtError
    {
        protected UserController user;

        public UserWebMgmtError(UserController user)
        {
            this.user = user;
        }
    }

    public class NotStandardOU : UserWebMgmtError
    {
        public NotStandardOU(UserController user) : base(user)
        {
            Heading = "User is in a non standard OU";
            Description = "This might not be a problem. User can be affected by non-stadard group policy. User can be a service user or admin account.";
            Severeness = Severity.Warning;
        }

        public override bool HaveError() => !user.UserIsInRightOU();
    }

    public class MissingAAUAttr : UserWebMgmtError
    {
        public MissingAAUAttr(UserController user) : base(user)
        {
            Heading = "User is missing AAU Attributes";
            Description = "The user is missing one or more of the AAU attributes. The user will not be able to login via login.aau.dk. Check CPR is correct in ADMdb.";
            Severeness = Severity.Error;
        }

        public override bool HaveError() => user.UserModel.AAUUserClassification == null || user.UserModel.AAUUserStatus == null || (user.UserModel.AAUStaffID == null && user.UserModel.AAUStudentID == null);
    }

    public class AccountLocked : UserWebMgmtError
    {
        public AccountLocked(UserController user) : base(user)
        {
            Heading = "Account locked";
            Description = "The user account is locked, maybe due to too many failed password attempts.";
            Severeness = Severity.Error;
        }

        public override bool HaveError()
        {
            const int UF_LOCKOUT = 0x0010;

            int userFlags = (int)user.UserModel.UserAccountControlComputed;

            return (userFlags & UF_LOCKOUT) == UF_LOCKOUT;
        }
    }
    public class ADFSLocked : UserWebMgmtError
    {
        public ADFSLocked(UserController user) : base(user)
        {
            Heading = "ADFS locked";
            Description = "The user account is ADFS locked, maybe due to too many failed password attempts.";
            Severeness = Severity.Error;
        }

        public override bool HaveError()
        {
            return user.UserModel.BasicInfoADFSLocked == true.ToString();
        }
    }

    public class UserLockedDiv : UserWebMgmtError
    {
        public UserLockedDiv(UserController user) : base(user)
        {
            Heading = "User account is locked";
            Description = "The user account is locked, used tasks unlock account to unlock it.";
            Severeness = Severity.Error;
        }

        public override bool HaveError() => user.UserModel.IsAccountLocked == true;
    }


    public class UserDisabled : UserWebMgmtError
    {
        public UserDisabled(UserController user) : base(user)
        {
            Heading = "User is diabled";
            Description = "The user is disabled in AD, user can't login. User is expired in AdmDB or disabled by a administrator, see <a href=\"onenote:https://docs.its.aau.dk/Documentation/Info%20til%20Service%20Desk/Disablet%20Users.one#Disabled%20users%20in%20AD&section-id={062F945F-AF8F-4E1C-8151-6C87AA1F134B}&page-id={86CE4A52-90A9-4A5C-A189-9402B9B6153B}&object-id={441C8DED-9C4E-4561-B184-186C63174D6D}&EB\">onenote</a>";
            Severeness = Severity.Error;
        }

        public override bool HaveError()
        {
            return user.UserModel.IsDisabled;
        }
    }
}
