using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITSWebMgmt.WebMgmtErrors;
using Microsoft.AspNetCore.Mvc;

namespace ITSWebMgmt.Controllers
{
    public class WebMgmtErrorListController : Controller
    {
        public IActionResult Index(List<WebMgmtError> errors)
        {
            WebMgmtErrorList model = new WebMgmtErrorList(errors);
            return View(model);
        }
    }
}