using ITSWebMgmt.Controllers;
using ITSWebMgmt.WebMgmtErrors;

namespace ITSWebMgmt.Models
{
    public abstract class MacWebMgmtError : WebMgmtError
    {
        public ComputerController computer;
   
        public int Id { get; set; }
        public string GroupName { get; set; }
        public string CaseLink { get; set; }
        public bool Active { get; set; } = true;
        public MacWebMgmtError(ComputerController computer)
        {
            this.computer = computer;
        }
    }

    public class NotAAUMac : MacWebMgmtError
    {
        public NotAAUMac(ComputerController computer) : base(computer)
        {
            Heading = "This is not an AAU-Mac";
            Description = "The computer is not an AAU-Mac";
            Severeness = Severity.Error;
            GroupName = "AAU Mac";
        }

        public override bool HaveError()
        {
            return !computer.ComputerModel.Mac.Groups.Contains(GroupName);
        }
    }

    public class MissingGroup : MacWebMgmtError
    {
        public MissingGroup() : base(null) { }
        public MissingGroup(ComputerController computer) : base(computer)
        {
        }

        public override bool HaveError()
        {
            return computer.ComputerModel.Mac.Groups.Contains(GroupName);
        }
    }
}
