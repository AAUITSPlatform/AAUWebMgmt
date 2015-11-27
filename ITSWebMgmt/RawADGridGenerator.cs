﻿using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Web;

namespace ITSWebMgmt
{
    public class RawADGridGenerator
    {

        public string buildRawSegment(DirectoryEntry result)
        {
            //builder.Append((result.Properties["aauStaffID"][0])).ToString();
            var builder = new StringBuilder();

            builder.Append("<table><tr><th>k</th><th>v</th></tr>");

            foreach (string k in result.Properties.PropertyNames)
            {
                //Don't display admin password in raw data
                if (k.Equals("ms-Mcs-AdmPwd"))
                {
                    continue;
                }

                builder.Append("<tr>");
                builder.Append("<td>" + k + "</td>");

                var a = result.Properties[k];
                if (a != null && a.Count > 0)
                {
                    if (a.Count == 1)
                    {
                        var v = a[0];
                        builder.Append("<td>" + v + "</td></tr>");
                    }
                    else
                    {

                        var v = a[0];
                        builder.Append("<td>" + v + "</td>");
                        for (int i = 1; i < a.Count; i++)
                        {
                            v = a[i];
                            builder.Append("<tr><td></td><td>" + v + "</td></tr>");
                        }

                    }

                }
                else
                {
                    builder.Append("<td></td></tr>");
                }

            }

            builder.Append("</table>");

            return builder.ToString();
        }


    }
}