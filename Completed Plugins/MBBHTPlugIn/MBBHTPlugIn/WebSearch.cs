using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MBBHTPlugIn
{
    public class WebSearch
    {

        private RegexOptions RegOpt = RegexOptions.IgnoreCase | RegexOptions.Singleline;
        private Provider provider { get; set; }

        //Parameter prefix
        private const string PREFIX = "_ctl7:";

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
            string baseUrl = "https://bht.hlb.state.mn.us/DesktopModules/ServiceForm.aspx?svid=37&mid=178";

            //Set up security protocol
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //Set up client and request
            RestClient client = new RestClient(baseUrl);
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            
            //Store new cookies
            allCookies.AddRange(response.Cookies);

            //Check that we accessed the site
            if (response.StatusCode != HttpStatusCode.OK)
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);

            //Forming new post request with our parameters
            request = new RestRequest(Method.POST);

            //We perform the search using license numbers if available, otherwise full names
            string licNo = provider.LicenseNumber;
            string type = GetLicenseType(ref licNo);

            //Event headers
            string event_target = "", event_argument = "", event_validation = "", view_state = "", view_state_generator = "", last_focus = "";

            if (type != "")
            {
                request.AddParameter(PREFIX + "ddlbLicenseType", type);
                request.AddParameter(PREFIX + "txtLicenseNumber", licNo);
                request.AddParameter(PREFIX + "rbtnSearch", "1");
            } else
            {
                //Switch to name mode
                request.AddParameter(PREFIX + "txtLicenseNumber", "");
                request.AddParameter(PREFIX + "ddlbLicenseType", "CD");
                request.AddParameter(PREFIX + "rbtnSearch", "2");
                GetViewStates(ref event_target, ref event_argument, ref event_validation, ref view_state, ref view_state_generator, ref last_focus, response);
                SetViewStates("_ctl7$rbtnSearch$1", event_argument, event_validation, view_state, view_state_generator, last_focus, ref request);
                //Add cookies to our request
                foreach (RestResponseCookie c in allCookies)
                    request.AddCookie(c.Name, c.Value);
                response = client.Execute(request);

                //Store new cookies
                allCookies.AddRange(response.Cookies);

                //Check that we accessed the site
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSearchForm);

                //Forming new post request with our parameters
                request = new RestRequest(Method.POST);

                //Get new parameters
                request.AddParameter(PREFIX + "txtLastName", provider.LastName);
                request.AddParameter(PREFIX + "txtFirstName", provider.FirstName);
                request.AddParameter(PREFIX + "rbtnSearch", "2");
            }

            GetViewStates(ref event_target, ref event_argument, ref event_validation, ref view_state, ref view_state_generator, ref last_focus, response);
            SetViewStates("_ctl7$cmdSearch", event_argument, event_validation, view_state, view_state_generator, last_focus, ref request);

            //Add cookies to our request
            foreach (RestResponseCookie c in allCookies)
                request.AddCookie(c.Name, c.Value);

            client.BaseUrl = new Uri(baseUrl + "&step=1");

            response = client.Execute(request);

            //Store new cookies
            allCookies.AddRange(response.Cookies);

            //Check that we accessed the site
            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Redirect)
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);

            //If we searched by license number, site automatically redirects us to details page
            if (type != "")
                return Result<IRestResponse>.Success(response);

            MatchCollection providerList = Regex.Matches(response.Content, "_ctl7_grdSearchResults__ctl[\\d]_Hyperlink1", RegOpt);

            //Check if we have multiple providers
            if (providerList.Count > 1)
                return Result<IRestResponse>.Failure(ErrorMsg.MultipleProvidersFound);

            //Check if we have no providers
            if (providerList.Count == 0)
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);

            //Navigate to the details page
            Match m = Regex.Match(response.Content, "&amp;(?<id>id=[\\d]+)&amp;(?<type>lictype=[\\w]+)\"><");
            client.BaseUrl = new Uri($"{baseUrl}&step=2&{m.Groups["id"].Value}&{m.Groups["type"].Value}");

            //Forming new get request with our parameters
            request = new RestRequest(Method.GET);

            //Add cookies to our request
            foreach (RestResponseCookie c in allCookies)
                request.AddCookie(c.Name, c.Value);

            response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessDetailsPage);

            return Result<IRestResponse>.Success(response);
        }

        string GetLicenseType(ref string licNo)
        {
            Match m = Regex.Match(licNo, "(?<type>[A-Za-z]*)[-]?(?<num>[\\d]+)", RegOpt);
            string type = m.Groups["type"].Value;
            licNo = m.Groups["num"].Value;

            if (type == "")
                type = provider.GetData("drtitle");

            switch (type.ToLower())
            {
                case "professional counselor":
                case "licensed professional counselor":
                case "lpc":
                    type = "PC";
                    break;
                case "professional clinical counselor":
                case "licensed professional clinical counselor":
                case "lpcc":
                    type = "CC";
                    break;
                case "alcohol and drug counselor":
                case "licensed alcohol and drug counselor":
                case "ladc":
                    type = "CD";
                    break;
                default:
                    type = "";
                    break;
            }

            return type;
        }

        void GetViewStates(ref string target, ref string argument, ref string validation, ref string state, ref string generator, ref string focus, IRestResponse response)
        {
            target = Regex.Match(response.Content, "id=\"__EVENTTARGET\"\\s*value=\"([\\w\\+/=]*)", RegOpt).Groups[1].Value;
            argument = Regex.Match(response.Content, "id=\"__EVENTARGUMENT\"\\s*value=\"([\\w\\+/=]*)", RegOpt).Groups[1].Value;
            validation = Regex.Match(response.Content, "id=\"__EVENTVALIDATION\"\\s*value=\"([\\w\\+/=]*)", RegOpt).Groups[1].Value;
            generator = Regex.Match(response.Content, "id=\"__VIEWSTATEGENERATOR\"\\s*value=\"([\\w]*)", RegOpt).Groups[1].Value;
            state = Regex.Match(response.Content, "id=\"__VIEWSTATE\"\\s*value=\"([\\w\\+/=]*)", RegOpt).Groups[1].Value;
            focus = "";
        }

        void SetViewStates(string target, string argument, string validation, string state, string generator, string focus, ref RestRequest request)
        {
            request.AddParameter("__EVENTTARGET", target);
            request.AddParameter("__EVENTARGUMENT", argument);
            request.AddParameter("__EVENTVALIDATION", validation);
            request.AddParameter("__VIEWSTATE", state);
            request.AddParameter("__VIEWSTATEGENERATOR", generator);
            request.AddParameter("__LASTFOCUS", focus);
        }
    }
}
