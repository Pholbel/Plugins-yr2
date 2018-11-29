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

            // PARAMETERS AND COOKIES WE WILL GET WITH FIRST GET
            List<RestResponseCookie> allCookies = new List<RestResponseCookie>();
            string viewState = "", viewState1 ="", viewState2="", viewState3="", viewState4 = "", viewState5 = "", viewState6 = "", viewState7 = "", viewState8 = "", viewState9 = "", viewState10 = "";
            string viewStateGenerator = "";
            string eventValidation = "";
            string lastFocus = "";
            string eventTarget = "";
            string eventArgument = "";
            string viewStateFieldCount = "";
            string baseUrl = "https://www.pfr.maine.gov/ALMSOnline/ALMSQuery/";

            //GET PARAMETERS AND COOKIES
            RestClient client = new RestClient(baseUrl+"SearchIndividual.aspx");
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);

            allCookies.AddRange(response.Cookies);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                GetViewStates(ref viewStateFieldCount, ref viewState, ref viewState1, ref viewState2, ref viewState3, ref viewState4, ref viewState5, ref viewState6, ref viewState7, ref viewState8, ref viewState9, ref viewState10, ref viewStateGenerator, ref eventValidation, ref lastFocus, ref eventTarget, ref eventArgument, response);
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
            }

            //FORMING NEW POST WITH OUR PARAMS
            request = new RestRequest(Method.POST);

            string _param_prefix = "ctl00$ctl00$mainContent$mainContent$";

            request.AddParameter("__VIEWSTATEFIELDCOUNT", viewStateFieldCount);
            request.AddParameter("__EVENTTARGET", eventTarget);
            request.AddParameter("__EVENTARGUMENT", eventArgument);
            request.AddParameter("__LASTFOCUS", lastFocus);
            request.AddParameter("__VIEWSTATE", viewState);
            request.AddParameter("__VIEWSTATE1", viewState1);
            request.AddParameter("__VIEWSTATE2", viewState2);
            request.AddParameter("__VIEWSTATE3", viewState3);
            request.AddParameter("__VIEWSTATE4", viewState4);
            request.AddParameter("__VIEWSTATE5", viewState5);
            request.AddParameter("__VIEWSTATE6", viewState6);
            request.AddParameter("__VIEWSTATE7", viewState7);
            request.AddParameter("__VIEWSTATE8", viewState8);
            request.AddParameter("__VIEWSTATE9", viewState9);
            request.AddParameter("__VIEWSTATE10", viewState10);
            request.AddParameter("__VIEWSTATEGENERATOR", viewStateGenerator);
            request.AddParameter("__EVENTVALIDATION", eventValidation);
            request.AddParameter("__VIEWSTATEENCRYPTED", "");
            request.AddParameter(_param_prefix + "scDepartment", "");
            request.AddParameter(_param_prefix + "scAgency", "");
            request.AddParameter(_param_prefix + "scRegulator", "");
            request.AddParameter(_param_prefix + "scLastName", provider.LastName);
            request.AddParameter(_param_prefix + "scFirstName", provider.FirstName);
            request.AddParameter(_param_prefix + "scLicenseNo", provider.LicenseNumber);
            request.AddParameter(_param_prefix + "iShowAdditionalOptions", "false");
            request.AddParameter(_param_prefix + "iAdditionalOptionsSaved", "false");
            request.AddParameter(_param_prefix + "scCountry", "");
            request.AddParameter(_param_prefix + "scCity", "");
            request.AddParameter(_param_prefix + "scState", "");
            request.AddParameter(_param_prefix + "scCounty", "");
            request.AddParameter(_param_prefix + "scZip", "");
            request.AddParameter(_param_prefix + "btnSearch", "Search");

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
                GetViewStates(ref viewStateFieldCount, ref viewState, ref viewState1, ref viewState2, ref viewState3, ref viewState4, ref viewState5, ref viewState6, ref viewState7, ref viewState8, ref viewState9, ref viewState10, ref viewStateGenerator, ref eventValidation, ref lastFocus, ref eventTarget, ref eventArgument, response);
            }
            else { return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite); }

            //CHECK IF WE HAVE MULTIPLE PROVIDERS

            MatchCollection providerList = Regex.Matches(response.Content, @"ShowDetail.*(?<=TOKEN.)\w*");
         

            if (providerList.Count == 0)
            {
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);
            }
            else if (providerList.Count == 1)
            {
                string detailQuery = Regex.Match(response.Content, @"ShowDetail.*(?<=TOKEN.)\w*").ToString();
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
            }

        }

        private void GetViewStates(ref string viewStateFieldCount, ref string viewState, ref string viewState1, ref string viewState2, ref string viewState3, ref string viewState4, ref string viewState5, ref string viewState6, ref string viewState7, ref string viewState8, ref string viewState9, ref string viewState10, ref string viewStateGenerator, ref string eventValidation, ref string lastFocus, ref string eventTarget, ref string eventArgument, IRestResponse response)
        {
            Match m = Regex.Match(response.Content, "id=\"__VIEWSTATE\" value=\"(?<VIEW>.*?)\"", RegOpt);
            if (m.Success)
            {
                viewState = m.Groups["VIEW"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__VIEWSTATE1\" value=\"(?<VIEW1>.*?)\"", RegOpt);
            if (m.Success)
            {
                viewState1 = m.Groups["VIEW1"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__VIEWSTATE2\" value=\"(?<VIEW2>.*?)\"", RegOpt);
            if (m.Success)
            {
                viewState2 = m.Groups["VIEW2"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__VIEWSTATE3\" value=\"(?<VIEW3>.*?)\"", RegOpt);
            if (m.Success)
            {
                viewState3 = m.Groups["VIEW3"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__VIEWSTATE4\" value=\"(?<VIEW4>.*?)\"", RegOpt);
            if (m.Success)
            {
                viewState4 = m.Groups["VIEW4"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__VIEWSTATE5\" value=\"(?<VIEW5>.*?)\"", RegOpt);
            if (m.Success)
            {
                viewState5 = m.Groups["VIEW5"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__VIEWSTATE6\" value=\"(?<VIEW6>.*?)\"", RegOpt);
            if (m.Success)
            {
                viewState6 = m.Groups["VIEW6"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__VIEWSTATE7\" value=\"(?<VIEW7>.*?)\"", RegOpt);
            if (m.Success)
            {
                viewState7 = m.Groups["VIEW7"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__VIEWSTATE8\" value=\"(?<VIEW8>.*?)\"", RegOpt);
            if (m.Success)
            {
                viewState8 = m.Groups["VIEW8"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__VIEWSTATE9\" value=\"(?<VIEW9>.*?)\"", RegOpt);
            if (m.Success)
            {
                viewState9 = m.Groups["VIEW9"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__VIEWSTATE10\" value=\"(?<VIEW10>.*?)\"", RegOpt);
            if (m.Success)
            {
                viewState10 = m.Groups["VIEW10"].ToString();
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
            m = Regex.Match(response.Content, "id=\"__VIEWSTATEFIELDCOUNT\" value=\"(?<VIEWSTATEFIELDCOUNT>.*?)\"", RegOpt);
            if (m.Success)
            {
                viewStateFieldCount = m.Groups["VIEWSTATEFIELDCOUNT"].ToString();
            }
        }
    }
}
