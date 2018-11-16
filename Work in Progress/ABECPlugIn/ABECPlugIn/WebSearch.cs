using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ABECPlugIn
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
            string EVENTTARGET = "";
            string EVENTARGUMENT = "";
            string LASTFOCUS = "";
            string VIEWSTATE = "";
            string VIEWSTATEGENERATOR = "";
            string EVENTVALIDATION = "";
            string baseUrl = "http://www.abec.state.al.us/licensee.aspx";

            //GET PARAMETERS AND COOKIES
            RestClient client = new RestClient(baseUrl);
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            
            allCookies.AddRange(response.Cookies);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                GetViewStates(ref EVENTTARGET, ref EVENTARGUMENT, ref LASTFOCUS, ref VIEWSTATE, ref VIEWSTATEGENERATOR, ref EVENTVALIDATION, response);
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
            }

            //FORMING NEW POST WITH OUR PARAMS
            request = new RestRequest(Method.POST);

            request.AddParameter("__EVENTTARGET", EVENTTARGET);
            request.AddParameter("__EVENTARGUMENT", EVENTARGUMENT);
            request.AddParameter("__LASTFOCUS", LASTFOCUS);
            request.AddParameter("__VIEWSTATE", VIEWSTATE);
            request.AddParameter("__VIEWSTATEGENERATOR", VIEWSTATEGENERATOR);
            request.AddParameter("__EVENTVALIDATION", EVENTVALIDATION);
            request.AddParameter("ctl00$ContentPlaceHolder1$rdlALCLPC", "1"); // 1 for ALC, 2 for LPC
            request.AddParameter("ctl00$ContentPlaceHolder1$txtbxName", provider.FirstName + " " + provider.LastName);
            request.AddParameter("ctl00$ContentPlaceHolder1$txtbxLicenseNumber", provider.LicenseNumber);
            request.AddParameter("ctl00$ContentPlaceHolder1$btnSubmit", "Submit");

            foreach (var c in allCookies)
            {
                request.AddCookie(c.Name, c.Value);
            }

            response = client.Execute(request);

            //LAND HO!

            //REFILL THE COOKIE JAR
            allCookies.AddRange(response.Cookies);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                GetViewStates(ref EVENTTARGET, ref EVENTARGUMENT, ref LASTFOCUS, ref VIEWSTATE, ref VIEWSTATEGENERATOR, ref EVENTVALIDATION, response);
            }
            else { return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite); }

            //CHECK IF WE HAVE MULTIPLE PROVIDERS

            MatchCollection providerList = Regex.Matches(response.Content, ">" + provider.LastName + "</a>", RegOpt);

            if (providerList.Count == 0)
            {
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);
            }
            else if (providerList.Count == 1)
            {
                string baseSubUrl = "psychologist.php\\?id=";
                string searchRegex =  baseSubUrl + "(?<QUERY>.*?)\">" + provider.LastName;
                Match fields = Regex.Match(response.Content, searchRegex, RegOpt);
                string detailQuery = fields.Groups["QUERY"].ToString();
                detailQuery = Regex.Replace(detailQuery, "\"", "", RegOpt);
                client = new RestClient(baseUrl + baseSubUrl + detailQuery);
                request = new RestRequest(Method.GET);

                foreach (var c in allCookies)
                {
                    request.AddCookie(c.Name, c.Value);
                }

                response = client.Execute(request);

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
                return Result<IRestResponse>.Failure(ErrorMsg.MultipleProvidersFound);
            }
            
        }

        private void GetViewStates(ref string EVENTTARGET, ref string EVENTARGUMENT, ref string LASTFOCUS, ref string VIEWSTATE, ref string VIEWSTATEGENERATOR, ref string EVENTVALIDATION, IRestResponse response)
        {
            Match m = Regex.Match(response.Content, "id=\"__EVENTTARGET\" value=\"(?<EVENTTARGET>.*?)\"", RegOpt);
            if (m.Success)
            {
                EVENTTARGET = m.Groups["EVENTTARGET"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__EVENTARGUMENT\" value=\"(?<EVENTARGUMENT>.*?)\"", RegOpt);
            if (m.Success)
            {
                EVENTARGUMENT = m.Groups["EVENTARGUMENT"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__LASTFOCUS\" value=\"(?<LASTFOCUS>.*?)\"", RegOpt);
            if (m.Success)
            {
                LASTFOCUS = m.Groups["LASTFOCUS"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__VIEWSTATE\" value=\"(?<VIEWSTATE>.*?)\"", RegOpt);
            if (m.Success)
            {
                VIEWSTATE = m.Groups["VIEWSTATE"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__VIEWSTATEGENERATOR\" value=\"(?<VIEWSTATEGENERATOR>.*?)\"", RegOpt);
            if (m.Success)
            {
                VIEWSTATEGENERATOR = m.Groups["VIEWSTATEGENERATOR"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__EVENTVALIDATION\" value=\"(?<EVENTVALIDATION>.*?)\"", RegOpt);
            if (m.Success)
            {
                EVENTVALIDATION = m.Groups["EVENTVALIDATION"].ToString();
            }
        }
    }
}
