using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IDFPRPlugIn
{
    public class WebSearch
    {

        private RegexOptions RegOpt = RegexOptions.IgnoreCase | RegexOptions.Singleline;
        private Provider provider { get; set; }

        //Prefix
        const string PREFIX = "ctl00$MainContentPlaceHolder$ucLicenseLookup$ctl03";

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
            string baseUrl = "https://ilesonline.idfpr.illinois.gov/DFPR/Lookup";

            //Set up security protocol
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //Set up client and request
            RestClient client = new RestClient($"{baseUrl}/LicenseLookup.aspx");
            client.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
            RestRequest request = new RestRequest(Method.GET);

            //Execute request
            if (!ExecuteRequest(client, request, ref allCookies, out IRestResponse response))
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);

            //Search By
            NewPostRequest(allCookies, out request);

            Dictionary<string, string> formData = new Dictionary<string, string>
            {
                { "ctl00$ScriptManager1", "ctl00$MainContentPlaceHolder$ucLicenseLookup$UpdtPanelGridLookup|ctl00$MainContentPlaceHolder$ucLicenseLookup$UpdtPanelGridLookup" },
                { $"{PREFIX}$ddStatus", "" },
                { $"{PREFIX}$tbCredentialNumber_Credential", provider.LicenseNumber },
                { $"{PREFIX}$tbDBA_Contact", "" },
                { $"{PREFIX}$tbDBAAlias_ContactAlias", "" },
                { $"{PREFIX}$tbFirstName_Contact", provider.FirstName },
                { $"{PREFIX}$tbLastName_Contact", provider.LastName },
                { $"{PREFIX}$tbCity_ContactAddress", "" },
                { $"{PREFIX}$ddStates", "" },
                { $"{PREFIX}$ddCounty", "" },
                { $"{PREFIX}$tbZipCode_ContactAddress", "" },
                { "ctl00$MainContentPlaceHolder$ucLicenseLookup$ResizeLicDetailPopupID_ClientState", "0,0" }
            };

            //Event arguments
            string viewState = Regex.Match(response.Content, "<input\\s+type=\"hidden\"\\s+name=\"__VIEWSTATE\"\\s+id=\"__VIEWSTATE\"\\s+value=\"(?<state>[+=/\\w]+)\"\\s*/>", RegOpt).Groups["state"].Value;
            string viewStateGen = Regex.Match(response.Content, "<input\\s+type=\"hidden\"\\s+name=\"__VIEWSTATEGENERATOR\"\\s+id=\"__VIEWSTATEGENERATOR\"\\s+value=\"(?<gen>[\\w]+)\"\\s*/>", RegOpt).Groups["gen"].Value;
            Dictionary<string, string> eventArgs = new Dictionary<string, string>
            {
                { "__EVENTTARGET", "ctl00$MainContentPlaceHolder$ucLicenseLookup$UpdtPanelGridLookup" },
                { "__EVENTARGUMENT", "5" },
                { "__VIEWSTATE", viewState },
                { "__VIEWSTATEGENERATOR", viewStateGen },
                { "__ASYNCPOST", "true" }
            };

            AddParameters(formData, eventArgs, ref request);

            if (!ExecuteRequest(client, request, ref allCookies, out response))
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSearchForm);

            MatchCollection results = Regex.Matches(response.Content, "<a\\s+id=\"[_\\w]+\"\\s+class=\"[-\\w ]+\"\\s+href=\"[:\\w]+\\(&#39;(?<value>[;\\w ]+)&#39;\\)\">Detail</a>", RegOpt);

            if (results.Count == 0)
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);

            if (results.Count > 1)
                return Result<IRestResponse>.Failure(ErrorMsg.MultipleProvidersFound);

            //To details page
            NewGetRequest(allCookies, out request);

            client.BaseUrl = new Uri($"{baseUrl}/licensedetail.aspx");

            request.AddQueryParameter("id", results[0].Groups["value"].Value);

            if (!ExecuteRequest(client, request, ref allCookies, out response))
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessDetailsPage);
            
            return Result<IRestResponse>.Success(response);
        }

        void AddParameters(Dictionary<string, string> formData, Dictionary<string, string> eventArgs, ref RestRequest request)
        {
            //Form data
            foreach (KeyValuePair<string, string> data in formData)
                request.AddParameter(data.Key, data.Value);

            //Event args
            foreach (KeyValuePair<string, string> arg in eventArgs)
                request.AddParameter(arg.Key, arg.Value);
        }

        void NewPostRequest(List<RestResponseCookie> cookies, out RestRequest request)
        {
            //Forming new post request with our parameters
            request = new RestRequest(Method.POST);

            //Headers
            request.AddHeader("X-Requested-With", "XMLHttpRequest");

            //Add cookies to our request
            foreach (RestResponseCookie c in cookies)
                request.AddCookie(c.Name, c.Value);
        }

        void NewGetRequest(List<RestResponseCookie> cookies, out RestRequest request)
        {
            //Forming new post request with our parameters
            request = new RestRequest(Method.GET);

            //Headers
            request.AddHeader("X-Requested-With", "XMLHttpRequest");

            //Add cookies to our request
            foreach (RestResponseCookie c in cookies)
                request.AddCookie(c.Name, c.Value);
        }

        bool ExecuteRequest(RestClient client, RestRequest request, ref List<RestResponseCookie> cookies, out IRestResponse response)
        {
            //Execute our request
            response = client.Execute(request);

            //Store new cookies
            cookies.AddRange(response.Cookies);

            return response.StatusCode == HttpStatusCode.OK;
        }
    }
}
