using ITSWebMgmt.Models;
using System;
using System.Collections.Generic;

namespace ITSWebMgmt.WebMgmtErrors
{
    public class WebMgmtErrorList
    {
        public int[] ErrorCount { get; private set; } = { 0, 0, 0 };
        public string ErrorMessages;
        public List<WebMgmtError> PossibleErrors { get; private set; }
        //Blev træt af at skulle tjekke for HaveError i mine løkker, så nu er CurrentErrors en ting
        public List<WebMgmtError> CurrentErrors {
            get
            {
                return PossibleErrors.FindAll(x => x.HaveError());
            }
        }
        public bool CurrentlyHasErrors { 
            get
            {
                return CurrentErrors.Count != 0;
            }
        }
        public Severity MostImportantCurrentSeverity
        {
            get
            {
                foreach(Severity s in Enum.GetValues(typeof(Severity)))
                { //Iterer over hver værdi i enummen
                    if (getSeverityCount(s) > 0)
                    { //Hvis den givne enumværdi har en fejl, så returner den. Husk at løkken kører fra error til warning til info. 
                        return s;
                    }
                }
                return Severity.Info; //Dette burde ikke ske, men i tilfælde af at det gør, så vælg den mindst kritiske.
            }
        }

        public WebMgmtErrorList(List<WebMgmtError> errors)
        {
            this.PossibleErrors = errors;
        }
        public int getSeverityCount(Severity severityToCount)
        {
            int count = 0;
            foreach (WebMgmtError error in CurrentErrors)
            {
                if (error.Severeness.Equals(severityToCount))
                {
                    count++;
                }
            }
            return count;
        }
    }
}