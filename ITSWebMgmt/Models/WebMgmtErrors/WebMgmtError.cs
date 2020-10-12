using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ITSWebMgmt.WebMgmtErrors
{
    public enum Severity { Error, Warning, Info}

    public abstract class WebMgmtError
    {
        public string Heading { get; set; }
        public string Description { get; set; }
        public abstract bool HaveError();
        public Severity Severeness { get; set; }
        public List<Button> Buttons { get; set; } = new List<Button>();
    }

    public class Button
    {
        public Button(string ButtonDescription, string FunctionName)
        {
            this.ButtonDescription = ButtonDescription;
            this.FunctionName = FunctionName;
        }
        public string ButtonDescription { get; set; }
        [Key] //Hvorfor skal dette være en key? Godt spørgsmål, men skidtet crasher hvis det ikke er  ¯\_(ツ)_/¯
        public string FunctionName { get; set; }
    }
}
