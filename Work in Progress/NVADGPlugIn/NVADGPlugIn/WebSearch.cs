using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NVADGPlugIn
{
    public class WebSearch
    {

        private RegexOptions RegOpt = RegexOptions.IgnoreCase | RegexOptions.Singleline;
        private Provider provider { get; set; }

        //Parameter prefix
        private const string PREFIX = "ctl00$ContentPlaceHolder1$";

        public WebSearch(Provider _provider)
        {
            this.provider = _provider;
        }

        public Result<IRestResponse> Execute()
        {
            try
            {
                return Search();
            }
            catch (Exception ex)
            {
                return Result<IRestResponse>.Exception(ex);
            }
        }

        private Result<IRestResponse> Search()
        {

            // PARAMETERS AND COOKIES WE WILL GET WITH FIRST GET
            List<RestResponseCookie> allCookies = new List<RestResponseCookie>();
            string baseUrl = "https://nvadg.glsuite.us/GLSuiteWeb/Clients/NVADG/Public/Verification/";

            //Set up security protocol
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //Set up client and request
            RestClient client = new RestClient(baseUrl+"Search.aspx");
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            
            //Store new cookies
            allCookies.AddRange(response.Cookies);

            //Check that we accessed the site
            if (response.StatusCode != HttpStatusCode.OK)
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);

            //Forming new post request with our parameters
            request = new RestRequest(Method.POST);

            //We perform the search using license numbers and/or full names, if available
            if (provider.LastName != "")
                request.AddParameter(PREFIX + "txtLastName", provider.LastName);

            if (provider.FirstName != "")
                request.AddParameter(PREFIX + "txtFirstName", provider.FirstName);

            request.AddParameter(PREFIX + "txtLicNum", provider.LicenseNumber);

            request.AddParameter(PREFIX + "ddlLicType", 0);

            //request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            //request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            //Event headers
            string event_target = "", event_argument = "", event_validation = "", view_state = "", view_state_encrypted = "", view_state_generator =  "";
            GetViewStates(ref event_target, ref event_argument, ref event_validation, ref view_state, ref view_state_encrypted, ref view_state_generator, response);

            request.AddParameter("__EVENTTARGET", PREFIX + "btnSearch");
            request.AddParameter("__EVENTARGUMENT", event_argument);
            request.AddParameter("__EVENTVALIDATION", event_validation);
            request.AddParameter("__VIEWSTATE", view_state);
            request.AddParameter("__VIEWSTATEENCRYPTED", view_state_encrypted);
            request.AddParameter("__VIEWSTATEGENERATOR", view_state_generator);

            //Add cookies to our request
            foreach (RestResponseCookie c in allCookies)
                request.AddCookie(c.Name, c.Value);

            response = client.Execute(request);

            //Store new cookies
            allCookies.AddRange(response.Cookies);

            //Check that we accessed the site
            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Redirect)
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);

            MatchCollection providerList = Regex.Matches(response.Content, "javascript:details", RegOpt);

            //Check if we have multiple providers
            if (providerList.Count > 1)
                return Result<IRestResponse>.Failure(ErrorMsg.MultipleProvidersFound);

            //Check if we have no providers
            if (providerList.Count == 0)
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);

            //Navigate to the details page
            client = new RestClient(baseUrl + "details.asp");

            //Forming new post request with our parameters
            request = new RestRequest(Method.POST);

            //Calls redirect form
            request.AddParameter("t", "0");

            //Add cookies to our request
            foreach (RestResponseCookie c in allCookies)
                request.AddCookie(c.Name, c.Value);

            response = client.Execute(request);

            return Result<IRestResponse>.Success(response);
        }

        void GetViewStates(ref string target, ref string argument, ref string validation, ref string state, ref string encrypted, ref string generator, IRestResponse response)
        {
            target = Regex.Match(response.Content, "id=\"__EVENTTARGET\"\\s*value=\"([\\w\\+/=]*)").Groups[1].Value;
            argument = Regex.Match(response.Content, "id=\"__EVENTARGUMENT\"\\s*value=\"([\\w\\+/=]*)").Groups[1].Value;
            validation = Regex.Match(response.Content, "id=\"__EVENTVALIDATION\"\\s*value=\"([\\w\\+/=]*)").Groups[1].Value;
            state = Regex.Match(response.Content, "id=\"__VIEWSTATE\"\\s*value=\"([\\w\\+/=]*)").Groups[1].Value;
            encrypted = Regex.Match(response.Content, "id=\"__VIEWSTATEENCRYPTED\"\\s*value=\"([\\w]*)").Groups[1].Value;
            generator = Regex.Match(response.Content, "id=\"__VIEWSTATEGENERATOR\"\\s*value=\"([\\w]*)").Groups[1].Value;
        }
    }
}
