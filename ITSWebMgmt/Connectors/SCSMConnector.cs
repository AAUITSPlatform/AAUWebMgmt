using ITSWebMgmt.Helpers;
using ITSWebMgmt.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ITSWebMgmt.Connectors
{
    //Before you venture into this file, make sure you have been blessed by a priest first. It has been created in the fires of hell and should just be nuked back to oblivion, but we still need it. So good luck.
    public class SCSMConnector
    {
        private const string webserviceURL = "https://service.aau.dk";

        public string userID = "";


        private static readonly string idForConvertedToSR = "d283d1f2-5660-d28e-f0a3-225f621394a9";

        private string authkey;

        public SCSMConnector()
        {
            authkey = getAuthKey().Result;
        }

        protected async Task<string> getAuthKey()
        {

            WebRequest request = WebRequest.Create(webserviceURL + "/api/V3/Authorization/GetToken");
            request.Method = "POST";
            request.ContentType = "text/json";
            
            string domain = Startup.Configuration["cred:scsm:domain"];
            string username = Startup.Configuration["cred:scsm:username"];
            string secret = Startup.Configuration["cred:scsm:password"];

            if (domain == null || username == null || secret == null)
            {
                return null;
            }

            string json = "{\"Username\": \"" + domain + "\\\\" + username + "\",\"Password\": \"" + secret + "\",\"LanguageCode\": \"ENU\"}";

            var requestStream = new StreamWriter(request.GetRequestStream());
            requestStream.Write(json);
            requestStream.Flush();
            requestStream.Close();

            var response = await request.GetResponseAsync();
            var responseSteam = response.GetResponseStream();

            var streamReader = new StreamReader(responseSteam);

            var responseText = streamReader.ReadToEnd();

            return responseText.Replace("\"", "");
        }

        protected ServiceManagerModel CreateServiceManager(string userId)
        {
            if (userId == null)
            {
                return new ServiceManagerModel(null, null);
            }
            return new ServiceManagerModel(userId, lookupWorkItemsByUUID(userId).Result);
        }

        //returns json string for uuid
        protected async Task<List<Case>> lookupWorkItemsByUUID(string uuid)
        {

            WebRequest request = WebRequest.Create(webserviceURL + "/api/V3/WorkItem/GetGridWorkItemsMyRequests?userid=" + uuid + "&showInactiveItems=true");
            request.Method = "Get";
            request.Headers.Add("Authorization", "Token " + authkey);
            request.ContentType = "application/json; text/json";

            var response = await request.GetResponseAsync();
            var responseSteam = response.GetResponseStream();

            var streamReader = new StreamReader(responseSteam);

            var responseText = streamReader.ReadToEnd();

            //Make a breakpoint here to see what the response actually is.
            //This is probably easier than looking at the API documentation tbh.
            List<Case> caseList = JsonConvert.DeserializeObject<List<Case>>(responseText);

            return caseList;

        }

        //Takes a upn and retuns the users uuid
        protected async Task<string> getUserUUIDByUPN(string upn)
        {
            //Get username from UPN


            WebRequest request = WebRequest.Create(webserviceURL + "/api/V3/User/GetUserList?fetchAll=false&userFilter=" + upn);
            request.Method = "Get";
            request.ContentType = "text/json";
            request.ContentType = "application/json; charset=utf-8";


            request.Headers.Add("Authorization", "Token " + authkey);

            var response = await request.GetResponseAsync();
            var responseSteam = response.GetResponseStream();

            var streamReader = new StreamReader(responseSteam);

            var responseText = streamReader.ReadToEnd();

            dynamic json = JsonConvert.DeserializeObject<dynamic>(responseText);


            //TODO: Don't await for each item, make all requests and await, then look over data
            foreach (dynamic obj in json)
            {
                if (upn.Equals((string)obj["Email"], StringComparison.CurrentCultureIgnoreCase))
                {
                    userID = (string)obj["Id"];
                    return userID;
                }
            }
            return null;
        }

        public async Task<ServiceManagerModel> getServiceManager(string upn)
        {
            string uuid = await getUUID(upn);
            
            return CreateServiceManager(uuid);
        }

        public async Task<string> getUUID(string upn)
        {
            return await getUserUUIDByUPN(upn);
        }
    }
}