using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DPFRPlugIn
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
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //COOKIES WE WILL GET WITH FIRST GET
            List<RestResponseCookie> allCookies = new List<RestResponseCookie>();
            string baseUrl = "https://www.kansas.gov/DPFR-verifications/search/";

            //GET PARAMETERS AND COOKIES
            RestClient client = new RestClient(baseUrl);
            RestRequest request = new RestRequest(Method.GET);
            request.AddParameter("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            IRestResponse response = client.Execute(request);

            //GET TOKEN
            string csrfToken = Regex.Match(response.Content, "(?<=TOKEN..value=.).*(?=..id)").ToString();

            allCookies.AddRange(response.Cookies);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
            }

            //FORMING NEW POST WITH OUR PARAMS
            client = new RestClient(baseUrl + "search");
            request = new RestRequest(Method.POST);


            request.AddParameter("SYNCHRONIZER_TOKEN", csrfToken);
            request.AddParameter("SYNCHRONIZER_URI", "/DPFR-verifications/search/");
            request.AddParameter("licenseNumber", provider.LicenseNumber);
            request.AddParameter("_eventId_submit", "Issue Search");
            
            foreach (var c in allCookies)
            {
                request.AddCookie(c.Name, c.Value);
            }

            response = client.Execute(request);

            //WE MADE IT

            //REFILL THE COOKIE JAR
            allCookies.AddRange(response.Cookies);
            if (response.StatusCode != HttpStatusCode.OK)
            {
             return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite); }

            //CHECK IF WE HAVE MULTIPLE PROVIDERS

            MatchCollection providerList = Regex.Matches(response.Content, "License Method");
     

            if (providerList.Count == 0)
            {
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);
            }
            else if (providerList.Count == 1)
            {
                    return Result<IRestResponse>.Success(response);
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.MultipleProvidersFound);
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
