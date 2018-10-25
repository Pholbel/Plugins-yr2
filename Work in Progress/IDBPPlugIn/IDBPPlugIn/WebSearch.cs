using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IDBPPlugIn
{
    public class WebSearch
    {
        private RegexOptions RegOpt = RegexOptions.IgnoreCase;
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
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string viewState = "";
            string viewStateGenerator = "";
            string eventValidation = "";
            string lastFocus = "";
            string eventTarget = "";
            string eventArgument = "";
            List<RestResponseCookie> allCookies = new List<RestResponseCookie>();
            RestRequest request1 = new RestRequest(Method.GET);
            RestRequest request2 = new RestRequest(Method.POST);
            string baseUrl = "https://idbop.mylicense.com/verification/Search.aspx";
            RestClient client = new RestClient(baseUrl);


            //FIRST REQUEST TO GET COOKIE AND VIEW STATE
            request1.AddParameter("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            IRestResponse response = client.Execute(request1);
            allCookies.AddRange(response.Cookies);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                GetViewStates(ref viewState, ref viewStateGenerator, ref eventValidation, ref lastFocus, ref eventTarget, ref eventArgument, response);
            }
            else { return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite); }



            // ADD PARAMETERS AND ADD COOKIE FOR SECOND REQUEST
            foreach (var c in allCookies)
            {
                request2.AddCookie(c.Name, c.Value);
            }
            request2.AddParameter("__EVENTTARGET", eventTarget);
            request2.AddParameter("__EVENTARGUMENT", eventArgument);
            request2.AddParameter("__LASTFOCUS", lastFocus);
            request2.AddParameter("__VIEWSTATE", viewState);
            request2.AddParameter("__VIEWSTATEGENERATOR", viewStateGenerator);
            request2.AddParameter("__EVENTVALIDATION", eventValidation);
            request2.AddParameter("t_web_lookup__license_no", provider.LicenseNumber);
            request2.AddParameter("sch_button", "Search");


            //SECOND REQUEST TO GET TO SEARCH RESULTS
            IRestResponse response2 = client.Execute(request2);

            Match detailLink = Regex.Match(response.Content, @"ProDetail.*(?<==.*\d)", RegOpt);
            



            if (detailLink.Success)
            {
               
                client = new RestClient(baseUrl + detailLink);
                request2 = new RestRequest(Method.GET);

                foreach (var c in allCookies)
                {
                    request2.AddCookie(c.Name, c.Value);
                }

                response = client.Execute(request2);

                allCookies.AddRange(response.Cookies);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return Result<IRestResponse>.Success(response);
                }
                else
                {
                    return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSearchResultsPage);
                }
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);
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
