﻿using ITSWebMgmt.Helpers;
using ITSWebMgmt.Models.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSWebMgmt.Controllers
{
    public class TestController : WebMgmtController
    {
        public TestController(LogEntryContext context) : base(context) { }

        public void Index()
        {
            throw new NotImplementedException();
        }
    }
}