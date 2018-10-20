using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WICVPlugIn
{
    public class WebSearch //
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
            /*
            string viewState = "";
            string viewStateGenerator = "";
            string eventValidation = "";
            string lastFocus = "";
            string eventTarget = "";
            string eventArgument = "";
            */
            string baseUrl = "https://app.wi.gov";

            //GET PARAMETERS AND COOKIES
            RestClient client = new RestClient(baseUrl + "/licensesearch");
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            
            allCookies.AddRange(response.Cookies);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                // GetViewStates(ref viewState, ref viewStateGenerator, ref eventValidation, ref lastFocus, ref eventTarget, ref eventArgument, response);
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
            }

            //FORMING NEW POST WITH OUR PARAMS
            request = new RestRequest(Method.POST);

            string formDataHeader = "IndividualCredentialSearchViewModel.IndividualCredentialModel.IceIndividualLicense.";
            request.AddParameter(formDataHeader + "LastName", provider.LastName);
            request.AddParameter(formDataHeader + "FirstName", provider.FirstName);
            request.AddParameter(formDataHeader + "LicenseNumber", provider.LicenseNumber);
            request.AddParameter(formDataHeader + "CredTypeID", -1);
            request.AddParameter("Command", "IndividualSearch");

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
                // GetViewStates(ref viewState, ref viewStateGenerator, ref eventValidation, ref lastFocus, ref eventTarget, ref eventArgument, response);
            }
            else { return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite); }

            //CHECK IF WE HAVE MULTIPLE PROVIDERS

            MatchCollection providerList = Regex.Matches(response.Content, ">" + provider.LastName.ToUpper() + ".*</a>", RegOpt);

            if (providerList.Count == 0)
            {
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);
            }
            else if (providerList.Count == 1)
            {
                Match fields = Regex.Match(response.Content, "<a href=\"(?<QUERY>.*?)\">" + provider.FirstName.ToUpper());
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
