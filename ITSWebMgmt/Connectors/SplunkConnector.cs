﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ITSWebMgmt.Connectors
{
    public class SplunkConnector
    {
        public static string Auth { private get; set; }
        public SplunkConnector()
        {
            string user = Startup.Configuration["cred:ad:username"];
            string pass = Startup.Configuration["cred:ad:password"];
            var plainTextBytes = Encoding.UTF8.GetBytes(user + ":" + pass);
            string base64encodedusernpass = Convert.ToBase64String(plainTextBytes);
            Auth = "Basic " + base64encodedusernpass;

            var temp = getData().Content.ReadAsStringAsync().Result; // This is done with a regex, becuase i could not find a NDJSON parser
            Regex regex = new Regex(@"sec_id"":""(?<email>[^ ]*) "".*""nBad_Password_Count"":""(?<count>[^ ]*) ");
            var entries = temp.Substring(0, temp.Length - 2).Split('\n');
            List<string> lockedAccouts = new List<string>();
            foreach (var entry in entries)
            {
                Match match = regex.Match(entry);
                if (int.Parse(match.Groups["count"].Value) >= 5)
                {
                    lockedAccouts.Add(match.Groups["email"].Value);
                }
            }

            Console.WriteLine(temp);
        }

        private HttpResponseMessage getData()
        {
            string url = "https://splunk.aau.dk:8089/services/search/jobs/export?output_mode=json";
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response;

            using (var request = new HttpRequestMessage(new HttpMethod("POST"), url))
            {
                request.Headers.TryAddWithoutValidation("Authorization", Auth);

                request.Content = new StringContent($"search={Uri.EscapeDataString(@"|loadjob savedsearch=SVC_SplunkREPORTS@srv.aau.dk:aau:aau_adfs_locked_out_d-1d")}", Encoding.UTF8, "application/x-www-form-urlencoded");

                response = client.SendAsync(request).Result;
            }
            client.Dispose();

            return response;
        }
    }
}