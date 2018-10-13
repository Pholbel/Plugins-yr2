using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IDCVPlugIn
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
            string baseUrl = "https://secure.ibol.idaho.gov/eIBOLPublic/";

            //GET PARAMETERS AND COOKIES
            RestClient client = new RestClient(baseUrl + "LPRBrowser.aspx");
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

            //FORMING NEW POST WITH OUR PARAMS
            request = new RestRequest(Method.POST);

            string _param_prefix = "ctl00$CPH1$txtsrc";

            request.AddParameter("ctl00$ToolkitScriptManager1", "ctl00$CPH1$UpdatePnl0|ctl00$CPH1$btnGoFind");
            request.AddParameter("ctl00_ToolkitScriptManager1_HiddenField", ";;AjaxControlToolkit, Version=3.5.40412.0, Culture=neutral, PublicKeyToken=28f01b0e84b6d53e:en-US:065e08c0-e2d1-42ff-9483-e5c14441b311:475a4ef5:addc6819:5546a2b:d2e10b12:effe2a26:37e2e5c9:5a682656:c7029a2:e9e598a9:3ac3e789:751cdd15:dfad98a5:1d3ed089:497ef277:a43b07eb:3cf12cf1;");
            request.AddParameter("__EVENTTARGET", eventTarget);
            request.AddParameter("__EVENTARGUMENT", eventArgument);
            request.AddParameter("__LASTFOCUS", lastFocus);
            request.AddParameter("__VIEWSTATE", viewState);
            request.AddParameter("__VIEWSTATEGENERATOR", viewStateGenerator);
            request.AddParameter("__EVENTVALIDATION", eventValidation);
            request.AddParameter(_param_prefix + "Profession", "COU");
            request.AddParameter(_param_prefix + "LicenseType", "");
            request.AddParameter(_param_prefix + "LicenseNo", provider.LicenseNumber);
            request.AddParameter(_param_prefix + "ApplicantLastName", provider.LastName);
            request.AddParameter(_param_prefix + "ApplicantFirstName", provider.FirstName);
            request.AddParameter(_param_prefix + "OriginalLicenseDate", "");
            request.AddParameter(_param_prefix + "FacilityName", "");
            request.AddParameter(_param_prefix + "OwnerName", "");
            request.AddParameter(_param_prefix + "City", "");
            request.AddParameter(_param_prefix + "PostalCode", "");
            request.AddParameter("__ASYNCPOST: true", "true");
            request.AddParameter("ctl00$CPH1$btnGoFind", "Start Search");
            foreach (var c in allCookies)
            {
                request.AddCookie(c.Name, c.Value);
            }

            response = client.Execute(request);
            //REFILL THE COOKIE JAR
            allCookies.AddRange(response.Cookies);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                GetViewStates(ref viewState, ref viewStateGenerator, ref eventValidation, ref lastFocus, ref eventTarget, ref eventArgument, response);
            }
            else { return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite); }

            // A SECOND REQUEST TO GET TO DETAIL PAGE
            string details_link = Regex.Matches(response.Content, "LicensePublicRecord.aspx.*(?!')")[0].ToString();
            details_link = details_link.Substring(0, details_link.Length - 3);
            RestClient client2 = new RestClient(baseUrl + details_link);
            RestRequest request2 = new RestRequest(Method.GET);
            IRestResponse response2 = client2.Execute(request);
            //LAND HO!
            //REFILL THE COOKIE JAR
            allCookies.AddRange(response.Cookies);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return Result<IRestResponse>.Success(response2);
                //GetViewStates(ref viewState, ref viewStateGenerator, ref eventValidation, ref lastFocus, ref eventTarget, ref eventArgument, response);
            }
            else { return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSearchResultsPage); }

            //CHECK IF WE HAVE MULTIPLE PROVIDERS
            /*
            MatchCollection providerList = Regex.Matches(response.Content, "(?<=cert.*\">).*(?=</a>)", RegOpt);
            HashSet<string> providerHash = new HashSet<string>();

            foreach (var p in providerList)
            {
                providerHash.Add(p.ToString());
            }

            if (providerHash.Count == 0)
            {
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);
            }
            else if (providerHash.Count == 1)
            {
                Match fields = Regex.Match(response.Content, "(?<QUERY>\"DetailPage.aspx?.*?\\\")", RegOpt);
                string detailQuery = fields.Groups["QUERY"].ToString();
                detailQuery = Regex.Replace(detailQuery, "\"", "", RegOpt);
                client = new RestClient(baseUrl + detailQuery);
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
            }*/
            
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
