using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MSCVPlugIn
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
            // FIRST ASSCESS THE SITE TO COLLECT COOKIES
            List<RestResponseCookie> allCookies = new List<RestResponseCookie>();
            RestClient client = new RestClient("https://www.lpc.ms.gov/secure/licensesearch.asp");
            RestRequest request = new RestRequest(Method.POST);
            IRestResponse response = client.Execute(request);
            allCookies.AddRange(response.Cookies);

            // SECOND REQUEST THE SEARCH RESULT PAGE
            string baseUrl = "https://www.lpc.ms.gov/secure/licensesearchresults.asp";
            client = new RestClient(baseUrl);
            request = new RestRequest(Method.POST);

            //FORMING NEW POST WITH OUR PARAMS

            // DETERMIN SELECT "AND" OR "OR" BASED ON INPUT
            string join1a;
            if(provider.LicenseNumber != "" && provider.LastName != "")
            {
                // IF INPUT HAS BOTH LIS# AND LAST NAME USE "AND"
                join1a = "AND";
            }
            else
            {
                // OTHERWISE USE "OR"
                join1a = "OR";
            }
            // FILLING FORM DATA
            request.AddParameter("licnbr", provider.LicenseNumber);
            request.AddParameter("join1a", join1a);
            request.AddParameter("lname", provider.LastName);
            request.AddParameter("join1b", "OR");
            request.AddParameter("city", "");
            request.AddParameter("join1c", "OR");
            request.AddParameter("country", "");
            request.AddParameter("join1d", "OR");
            request.AddParameter("Submit", "SEARCH");

            foreach (var c in allCookies)
            {
                request.AddCookie(c.Name, c.Value);
            }

            // REQUEST PAGE. AFTER NEXT LINE response.content SHOULD BE THE SEARCH RESULT PAGE
            response = client.Execute(request);

            allCookies.AddRange(response.Cookies);

            if (!(response.StatusCode == HttpStatusCode.OK))
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
            }

            // A THIRD REQUEST TO GET TO DETAIL PAGE

            // GETTING LINK TO DETAIL PAGE
            MatchCollection details_link_matches = Regex.Matches(response.Content, "licensesearchdetails.asp.*?\"");
            // CHECK IF THERE ARE MULTIPLE RESULTS
            if(details_link_matches.Count > 1)
            {
                return Result<IRestResponse>.Failure(ErrorMsg.MultipleProvidersFound);
            }
            // CHECK IF THERE IS NO RESULT
            if(details_link_matches.Count == 0)
            {
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);
            }
            string details_link = details_link_matches[0].ToString();
            details_link = details_link.Substring(0, details_link.Length - 1);
            RestClient client2 = new RestClient("https://www.lpc.ms.gov/secure/" + details_link);
            RestRequest request2 = new RestRequest(Method.GET);
            IRestResponse response2 = client2.Execute(request2);
            //LAND HO!
            //REFILL THE COOKIE JAR
            allCookies.AddRange(response.Cookies);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return Result<IRestResponse>.Success(response2);
            }
            else { return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSearchResultsPage); }
            
        }

    }
}
