using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WVBOMPlugIn
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
            string baseUrl = "https://www.wvbdosteo.org/verify/";

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

            //We perform the search using license numbers and last names, if available
            if (provider.LastName != "")
                request.AddParameter("lName", provider.LastName);

            string providerType = LicenseType();

            //The site will not let us perform a search without a provider type
            if (providerType == null)
                return Result<IRestResponse>.Failure(ErrorMsg.Custom("Unable to determine license type. Please input a provider title."));

            request.AddParameter("licType", providerType);
            request.AddParameter("licNo", provider.LicenseNumber);

            request.AddParameter("do", "submit");
            request.AddParameter("action", "submit");
            request.AddParameter("search", "Search");

            //Add cookies to our request
            foreach (RestResponseCookie c in allCookies)
                request.AddCookie(c.Name, c.Value);

            response = client.Execute(request);

            //Store new cookies
            allCookies.AddRange(response.Cookies);

            //Have we hit the request limit?
            if (Regex.Match(response.Content, "5 requests are permitted per 1 minute.", RegOpt).Success)
                return Result<IRestResponse>.Failure(ErrorMsg.Custom("This site caps at 5 requests a minute. Please try again later."));

            //Check that we accessed the site
            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Redirect)
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);

            MatchCollection providerList = Regex.Matches(response.Content, "javascript:details", RegOpt);

            //Check if we have multiple providers
            if (providerList.Count > 1)
                return Result<IRestResponse>.Failure(ErrorMsg.MultipleProvidersFound);

            //Check if we have no providers
            if (providerList.Count == 0)
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);

            //Navigate to the details page
            client = new RestClient(baseUrl + "details.asp");

            //Forming new post request with our parameters
            request = new RestRequest(Method.POST);

            //Calls redirect form
            request.AddParameter("t", "0");

            //Add cookies to our request
            foreach (RestResponseCookie c in allCookies)
                request.AddCookie(c.Name, c.Value);

            response = client.Execute(request);

            return Result<IRestResponse>.Success(response);
        }

        private string LicenseType()
        {
            Match licensePrefix = Regex.Match(provider.LicenseNumber, "ED", RegOpt);

            if (licensePrefix.Success)
                return "Residents";

            string title = provider.GetData("drtitle");

            if (title != "")
            {
                if (title == "D.O.")
                    return "BoardXP";

                if (title == "PA-C")
                    return "Physician Assistants";
            }

            return null;
        }
    }
}
