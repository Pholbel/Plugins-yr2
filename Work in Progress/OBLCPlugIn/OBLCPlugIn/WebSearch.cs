using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OBLCPlugIn
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
            string baseUrl = "https://hrlb.oregon.gov/oblpct/licenseelookup/";

            //Set up security protocol
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //Set up client and request
            RestClient client = new RestClient(baseUrl + "index.asp");
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            
            //Store new cookies
            allCookies.AddRange(response.Cookies);

            //Check that we accessed the site
            if (response.StatusCode != HttpStatusCode.OK)
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);

            //Forming new get request with our parameters
            request = new RestRequest(Method.GET);

            string searchUrl = "searchdir.asp?";

            //We perform the search using license numbers if available, last names otherwise
            //Website does not allow both
            if (provider.LicenseNumber != "")
            {
                string licenseType = LicenseType(provider.LicenseNumber);

                if (licenseType == "")
                    return Result<IRestResponse>.Failure(ErrorMsg.InvalidLicense);

                searchUrl += "searchby=" + licenseType;

                Match licenseNum = Regex.Match(provider.LicenseNumber, "\\d+");

                searchUrl += "&searchfor=" + licenseNum.Value;
            }
            else
            {
                searchUrl += "searchby=lastName";
                searchUrl += "&searchfor=" + provider.LastName;
            }

            searchUrl += "&stateselect=none&Submit=Search";
            client = new RestClient(baseUrl + searchUrl);

            //Add cookies to our request
            foreach (RestResponseCookie c in allCookies)
                request.AddCookie(c.Name, c.Value);

            response = client.Execute(request);

            //Store new cookies
            allCookies.AddRange(response.Cookies);

            //Check that we accessed the site
            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Redirect)
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);

            MatchCollection providerList = Regex.Matches(response.Content, "(<tr valign='Middle'>)", RegOpt);

            //Check if we have multiple providers
            if (providerList.Count > 1)
                return Result<IRestResponse>.Failure(ErrorMsg.MultipleProvidersFound);

            //Check if we have no providers
            if (Regex.Match(response.Content, "No Records Found.", RegOpt).Success)
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);

            //We have been redirected to the details page
            return Result<IRestResponse>.Success(response);
        }

        private string LicenseType(string license)
        {
            Match licensePrefix = Regex.Match(license, "([ctr]){1}\\d+", RegOpt);

            switch (licensePrefix.Groups[1].Value)
            {
                case "C":
                    return "lpcnum";
                case "T":
                    return "lmftnum";
                case "R":
                    return "regnum";
                default:
                    return "";
            }
        }
    }
}
