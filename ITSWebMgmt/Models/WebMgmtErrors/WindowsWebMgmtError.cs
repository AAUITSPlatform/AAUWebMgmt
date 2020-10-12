using ITSWebMgmt.Controllers;
using ITSWebMgmt.Helpers;
using ITSWebMgmt.Models;
using System;
using System.Linq;

namespace ITSWebMgmt.WebMgmtErrors
{
    public abstract class ComputerWebMgmtError : WebMgmtError
    {
        protected ComputerController computer;

        public ComputerWebMgmtError(ComputerController computer)
        {
            this.computer = computer;
        }
    }
    public class MissingDataFromSCCM : ComputerWebMgmtError
    {
        public MissingDataFromSCCM(ComputerController computer) : base(computer)
        {
            Heading = "Computer missing data from SCCM";
            Description = "Some infomation cannot be shown due to missing information from SCCM";
            Severeness = Severity.Warning;
        }

        public override bool HaveError()
        {
            if (computer.ComputerModel.Windows.SCCMCache.ResourceID == "")
            {
                return true;
            }

            return computer.ComputerModel.Windows.LogicalDisk.Count == 0;
        }
    }

    public class DriveAlmostFull : ComputerWebMgmtError
    {
        public DriveAlmostFull(ComputerController computer) : base(computer)
        {
            Heading = "Less than 5 GB space avilable";
            Description = "Having an almost full drive might cause troubles";
            Severeness = Severity.Warning;
        }

        public override bool HaveError()
        {
            if (computer.ComputerModel.Windows.SCCMCache.ResourceID == "")
            {
                return false;
            }

            if (computer.ComputerModel.Windows.LogicalDisk.Count != 0)
            {
                int space = computer.ComputerModel.Windows.LogicalDisk.GetPropertyInGB("FreeSpace");
                if (space == 0) return false;
                return space <= 5;
            }

            return false;
        }
    }

    public class ManagerAndComputerNotInSameDomain : ComputerWebMgmtError
    {
        public ManagerAndComputerNotInSameDomain(ComputerController computer) : base(computer)
        {
            Heading = "The computer and the user in \"managed by\" are in different domains";
            Description = "This may or may not be an error, but user cannot have local administrator rights until this is corrected";
            Severeness = Severity.Warning;
        }

        public override bool HaveError()
        {
            ComputerModel compModel = computer.ComputerModel;
            if (compModel.IsWindows)
            {
                WindowsComputerModel winModel = compModel.Windows;
                if (!compModel.Windows.ManagedBy.ManagedByDomainAndName.Contains('@'))
                {
                    return false;
                }
                string managerDomain = compModel.Windows.ManagedBy.ManagedByDomainAndName.Split('@')[1];
                //We are only interested in the bit after @, as that is the actual domain.
                //I am sure there is a prettier way of doing this, but this works too.
                //In case you don't know what is happening, I am using @ as a delimiter for the mail of the manager,
                //Which results in two indexes in the resulting array. As the domain is in the second index, we index by 1 to get it.
                return !winModel.Domain.Equals(managerDomain);
            }
            else
            {
                return false;
            }
        }
    }

    public class MissingPCConfig : ComputerWebMgmtError
    {
        public MissingPCConfig(ComputerController computer) : base(computer)
        {
            Heading = "The PC is missing config";
            Description = "The computer is not in the Administrativ10 PC or AAU10 PC group. Please add it to one of them. It can take over 30 secounds before you get responce, that says if it succesfully was added, please be patient.";
            Severeness = Severity.Error;
            this.Buttons.Add(new Button("Add computer to AAU10 PC", "AddToAAU10"));
            this.Buttons.Add(new Button("Add computer to Administrativ10 PC", "AddToAdministrativ10"));
        }

        public override bool HaveError()
        {
            return (computer.ComputerModel.Windows.ConfigPC == "Unknown" && computer.ComputerModel.Windows.WhenCreated > DateTime.Parse("2019-01-01"));
        }
    }

    public class MissingPCADGroup : ComputerWebMgmtError
    {
        public MissingPCADGroup(ComputerController computer) : base(computer)
        {
            Heading = "The PC is missing in an ad group";
            Description = "The computer is neither in the AD group cm12_config_AAU10 or cm12_config_Administrativ10, please add it to one of them";
            Severeness = Severity.Error;
            this.Buttons.Add(new Button("Add computer to cm12_config_AAU10", "AddToADAAU10"));
            this.Buttons.Add(new Button("Add computer to cm12_config_Administrativ10", "AddToADAdministrativ10"));
        }

        public override bool HaveError()
        {
            var groups = computer.ComputerModel.Windows.ADCache.GetGroups("memberOf");
            return (!groups.Any(x => x.Contains("cm12_config_AAU10") || x.Contains("cm12_config_Administrativ10"))
                && computer.ComputerModel.Windows.WhenCreated > DateTime.Parse("2019-01-01"));
        }
    }

    public class IsWindows7 : ComputerWebMgmtError
    {
        public IsWindows7(ComputerController computer) : base(computer)
        {
            Heading = "The computer uses Windows 7";
            Description = "Windows 7 is not longer support. The computer have to be reinstalled with Windows 10";
            Severeness = Severity.Error;
        }

        public override bool HaveError()
        {
            return (computer.ComputerModel.Windows.ConfigPC == "AAU7 PC" || computer.ComputerModel.Windows.ConfigPC == "Administrativ7 PC");
        }
    }

    public class NotStandardComputerOU : ComputerWebMgmtError
    {
        public NotStandardComputerOU(ComputerController computer) : base(computer)
        {
            Heading = "Computer is in a wrong OU";
            Description = "The computer is getting wrong GPO settings. Fix by using task \"Move computer to OU Clients.\" ";
            Severeness = Severity.Error;
        }

        public override bool HaveError() => !computer.ComputerIsInRightOU(computer.ComputerModel.Windows.DistinguishedName);
    }

    public class PasswordExpired : ComputerWebMgmtError
    {
        public PasswordExpired(ComputerController computer) : base(computer)
        {
            Heading = "Computer admin password is expired";
            Description = "The computer admin password is expired";
            Severeness = Severity.Warning;
        }

        public override bool HaveError()
        {
            var time = computer.ComputerModel.Windows.AdminPasswordExpirationTime;
            if (time != null && time != "")
            {
                try
                {
                    return DateTime.Parse(time) < DateTime.Now;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }
    }

    public class MissingJavaLicense : ComputerWebMgmtError
    {
        public MissingJavaLicense(ComputerController computer) : base(computer)
        {
            Heading = "Computer missing Java licence";
            Description = "Computer must be a Administrativ10 PC to have a Java licence";
            Severeness = Severity.Error;
        }

        public override bool HaveError()
        {
            return computer.ComputerModel.Windows.ConfigPC != "Administrativ10 PC" && computer.ComputerModel.Windows.HasJava();
        }
    }

    public class HaveVirus : ComputerWebMgmtError
    {
        public HaveVirus(ComputerController computer) : base(computer)
        {
            Heading = "Virus found on computer";
            Description = "A decription on of the virus can be seen in the antivirus tab";
            Severeness = Severity.Warning;
        }

        public override bool HaveError()
        {
            return computer.ComputerModel.Windows.SCCMAV.ErrorMessage != "Antivirus information not found";
        }
    }
}
