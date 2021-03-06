﻿using ITSWebMgmt.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSWebMgmt.WebMgmtErrors
{
    public class WebMgmtErrorList
    {
        private readonly List<WebMgmtError> errors;
        private readonly int[] ErrorCount = { 0, 0, 0 };
        public string ErrorMessages;

        public WebMgmtErrorList(List<WebMgmtError> errors)
        {
            this.errors = errors;
        }

        public async Task<bool> HaveErrorAsync(WebMgmtError error)
        {
            return error.HaveError() || await error.HaveErrorAsync();
        }

        public async Task ProcessErrorsAsync()
        {
            var error_tasks = new List<Task<bool>>();
            foreach (WebMgmtError error in errors)
            {
                error_tasks.Add(HaveErrorAsync(error));
            }

            var haveErrors = await Task.WhenAll(error_tasks.ToArray());

            foreach (var error in errors.Zip(haveErrors, (e, he) => new { Error = e, HaveError = he }))
            {
                if (error.HaveError)
                {
                    ErrorCount[(int)error.Error.Severeness]++;
                    ErrorMessages += GenerateMessage(error.Error);
                }
            }
            if (ErrorMessages == null)
            {
                ErrorMessages = "No warnings found";
            }
        }

        private string GenerateMessage(WebMgmtError error)
        {
            string messageType = "";

            switch (error.Severeness)
            {
                case Severity.Error:
                    messageType = "negative";
                    break;
                case Severity.Warning:
                    messageType = "warning";
                    break;
                case Severity.Info:
                    messageType = "info";
                    break;
            }

            if (error is MissingGroup)
            {
                var macError = error as MissingGroup;
                //TODO handle these errors special
                return $"<div class=\"ui {messageType} message\" runat= \"server\">" +
                    $"<div class=\"header\">{macError.Heading}</div>" +
                    $"<p>{macError.Description}</p><br/>" +
                    $"<a href=\"{macError.CaseLink}\">Case<a/>" +
                    $"</div>";
            }

            return $"<div class=\"ui {messageType} message\" runat= \"server\">" +
                    $"<div class=\"header\">{error.Heading}</div>" +
                    $"<p>{error.Description}</p>" +
                    $"</div>";
        }

        public string GetErrorCountMessage()
        {
            string messageType = "";
            string heading = "";

            if (ErrorCount[(int)Severity.Error] > 0)
            {
                messageType = "negative";
                heading = "Errors";
            }
            else if (ErrorCount[(int)Severity.Warning] > 0)
            {
                messageType = "warning";
                heading = "Warnings";
            }
            else if (ErrorCount[(int)Severity.Info] > 0)
            {
                messageType = "info";
                heading = "Infos";
            }

            return messageType == "" ? "" : $"<div class=\"ui {messageType} message\" runat= \"server\">" +
                    $"<div class=\"header\">{heading} found</div>" +
                    $"<p>Found {ErrorCount[(int)Severity.Error]} errors, {ErrorCount[(int)Severity.Warning]} warnings, and {ErrorCount[(int)Severity.Info]} infos.</p>" +
                    $"</div>";
        }
    }
}