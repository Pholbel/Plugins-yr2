using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MDPRplugin
{
    public class WebSearch
    {
        private RegexOptions RegOpt = RegexOptions.IgnoreCase | RegexOptions.Singleline;
        private Provider provider { get; set; }

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
            string viewState = "";
            string viewStateGenerator = "";
            string eventValidation = "";
            string lastFocus = "";
            string eventTarget = "";
            string eventArgument = "";
            string id = "";
            string baseUrl = "https://renew.pr.mo.gov/licensee-search.asp";

            //GET PARAMETERS AND COOKIES
            RestClient client = new RestClient(baseUrl);
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            
            allCookies.AddRange(response.Cookies);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                GetViewStates(ref viewState, ref viewStateGenerator, ref eventValidation, ref lastFocus, ref eventTarget, ref eventArgument, response);
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
            }

            client = new RestClient("https://renew.pr.mo.gov/licensee-search-results.asp?passview=1");
            request = new RestRequest(Method.POST);

            request.AddParameter("message", "Submited Succesfully");
            request.AddParameter("select_county", "ALL");
            request.AddParameter("select_profession", "Professional Counselor");
            request.AddParameter("select_search", "Number");
            request.AddParameter("select_criteria", provider.LicenseNumber);

            foreach (var c in allCookies)
            {
                request.AddCookie(c.Name, c.Value);
            }

            response = client.Execute(request);

            Match match = Regex.Match(response.Content, "passkey=(?<ID>.*?)'", RegOpt);
            id = match.Groups["ID"].ToString();

            client = new RestClient("https://renew.pr.mo.gov/licensee-search-detail.asp?passkey=" + id);

            request = new RestRequest(Method.GET);
            response = client.Execute(request);
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return Result<IRestResponse>.Success(response);
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSearchResultsPage);
            }
        }

        private void GetViewStates(ref string viewState, ref string viewStateGenerator, ref string eventValidation, ref string lastFocus, ref string eventTarget, ref string eventArgument, IRestResponse response)
        {
            Match m = Regex.Match(response.Content, "id=\"__VIEWSTATE\" value=\"(?<VIEW>.*?)\"", RegOpt);
            if (m.Success)
            {
                viewState = m.Groups["VIEW"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__VIEWSTATEGENERATOR\" value=\"(?<VIEWGEN>.*?)\"", RegOpt);
            if (m.Success)
            {
                viewStateGenerator = m.Groups["VIEWGEN"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__EVENTVALIDATION\" value=\"(?<EVENT>.*?)\"", RegOpt);
            if (m.Success)
            {
                eventValidation = m.Groups["EVENT"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__LASTFOCUS\" value=\"(?<EVENT>.*?)\"", RegOpt);
            if (m.Success)
            {
                lastFocus = m.Groups["EVENT"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__EVENTTARGET\" value=\"(?<EVENTTAR>.*?)\"", RegOpt);
            if (m.Success)
            {
                eventTarget = m.Groups["EVENTTAR"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__EVENTARGUMENT\" value=\"(?<EVENTARG>.*?)\"", RegOpt);
            if (m.Success)
            {
                eventArgument = m.Groups["EVENTARG"].ToString();
            }
        }
    }
}
