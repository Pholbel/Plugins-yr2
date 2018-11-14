using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VRCVPlugIn
{
    public class WebSearch
    {

        private RegexOptions RegOpt = RegexOptions.IgnoreCase | RegexOptions.Singleline;
        private Provider provider { get; set; }

        //Parameter prefix
        private const string PREFIX = "ctl00$MainContentPlaceHolder$ucLicenseLookup$";

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
            string baseUrl = "https://apps.colorado.gov/dora/licensing/Lookup";

            //Set up security protocol
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //Set up client and request
            RestClient client = new RestClient(baseUrl);
            client.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
            RestRequest request = new RestRequest("LicenseLookup.aspx", Method.GET);
            IRestResponse response = client.Execute(request);
            
            //Store new cookies
            allCookies.AddRange(response.Cookies);

            //Check that we accessed the site
            if (response.StatusCode != HttpStatusCode.OK)
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);

            //Forming new post request with our parameters
            request = new RestRequest("LicenseLookup.aspx", Method.POST);

            //Headers
            request.AddHeader("X-MicrosoftAjax", "Delta=true");
            request.AddHeader("X-Requested-With", "XMLHttpRequest");
            //request.AddHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");

            //Add cookies to our request
            foreach (RestResponseCookie c in allCookies)
                request.AddCookie(c.Name, c.Value);

            //Event headers
            string event_target = "", event_argument = "", view_state = "", view_state_generator = "", async = "";
            event_target = "ctl00$MainContentPlaceHolder$ucLicenseLookup$UpdtPanelGridLookup";
            event_argument = "1";
            async = "true";

            GetViewStates(ref view_state, ref view_state_generator, response);
            SetViewStates(event_target, event_argument, view_state, view_state_generator, async, ref request);

            //We perform the search using license numbers and/or names
            string licNo = provider.LicenseNumber;

            //Licenses can have sub types
            Tuple<string, string> type = GetLicenseType(ref licNo);

            /* BEGIN ADD PARAMETERS */
            request.AddParameter("ctl00$ScriptManager1", "ctl00$MainContentPlaceHolder$ucLicenseLookup$UpdtPanelGridLookup|ctl00$MainContentPlaceHolder$ucLicenseLookup$UpdtPanelGridLookup");

            //Search details
            request.AddParameter(PREFIX + "ctl03$ddCredPrefix", type.Item1);
            request.AddParameter(PREFIX + "ctl03$tbLicenseNumber", licNo);
            request.AddParameter(PREFIX + "ctl03$ddSubCategory", type.Item2);
            request.AddParameter(PREFIX + "ctl03$tbFirstName_Contact", provider.FirstName);
            request.AddParameter(PREFIX + "ctl03$tbLastName_Contact", provider.LastName);

            request.AddParameter(PREFIX + "ctl03$tbDBA_Contact", "");
            request.AddParameter(PREFIX + "ctl03$tbCity_ContactAddress", "");
            request.AddParameter(PREFIX + "ctl03$ddStates", "");
            request.AddParameter(PREFIX + "ctl03$tbZipCode_ContactAddress", "");

            request.AddParameter(PREFIX + "ResizeLicDetailPopupID_ClientState", "0,0");
            request.AddParameter("ctl00$OutsidePlaceHolder$ucLicenseDetailPopup$ResizeLicDetailPopupID_ClientState", "0,0");
            /* END ADD PARAMETERS */

            //Site uses a modal window with an async request
            response = client.Execute(request);

            //Store new cookies
            allCookies.AddRange(response.Cookies);

            //Check that we accessed the site
            if ((response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Redirect)||
                Regex.Match(response.Content, "ErrorPage.aspx").Success)
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);

            MatchCollection providerList = Regex.Matches(response.Content, "DisplayLicenceDetail\\(&#39;(?<Detail>[;\\w ]+)&#39;\\)");

            //Check if we have multiple providers
            if (providerList.Count > 1)
                return Result<IRestResponse>.Failure(ErrorMsg.MultipleProvidersFound);

            //Check if we have no providers
            if (providerList.Count == 0)
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);

            //Navigate to the details page
            request = new RestRequest("licensedetail.aspx", Method.GET);

            //Add cookies to our request
            foreach (RestResponseCookie c in allCookies)
                request.AddCookie(c.Name, c.Value);

            request.AddQueryParameter("id", providerList[0].Groups["Detail"].Value);

            response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessDetailsPage);

            return Result<IRestResponse>.Success(response);
        }

        Tuple<string, string> GetLicenseType(ref string licNo)
        {
            Match m = Regex.Match(licNo, "(?<type>[-A-Za-z]*?)[-.]?(?<num>[\\d]+)[-.]?(?<subtype>[-A-Za-z]*)", RegOpt);
            string type = m.Groups["type"].Value;
            string subtype = m.Groups["subtype"].Value;
            licNo = m.Groups["num"].Value;

            return new Tuple<string, string>(type, subtype);
        }

        void GetViewStates(ref string state, ref string generator, IRestResponse response)
        {
            generator = Regex.Match(response.Content, "id=\"__VIEWSTATEGENERATOR\"\\s*value=\"([\\w]*)", RegOpt).Groups[1].Value;
            state = Regex.Match(response.Content, "id=\"__VIEWSTATE\"\\s*value=\"([\\w\\+/=]*)", RegOpt).Groups[1].Value;
        }

        void SetViewStates(string target, string argument, string state, string generator, string async, ref RestRequest request)
        {
            request.AddParameter("__EVENTTARGET", target);
            request.AddParameter("__EVENTARGUMENT", argument);
            request.AddParameter("__VIEWSTATE", state);
            request.AddParameter("__VIEWSTATEGENERATOR", generator);
            request.AddParameter("__ASYNCPOST", async);
        }
    }
}
