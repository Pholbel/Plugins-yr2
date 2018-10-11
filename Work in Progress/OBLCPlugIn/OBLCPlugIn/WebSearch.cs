using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IBPLPlugIn
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
            RestClient client = new RestClient(baseUrl + "/publicsearch.jsp");
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            
            //Store new cookies
            allCookies.AddRange(response.Cookies);

            //Check that we accessed the site
            if (response.StatusCode != HttpStatusCode.OK)
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);

            //FORMING NEW POST WITH OUR PARAMS
            request = new RestRequest(Method.POST);

            //We perform the search using license numbers and last names
            request.AddParameter("licenseNumber", provider.LicenseNumber);
            request.AddParameter("lastName", provider.LastName);

            //Add cookies to our request
            foreach (RestResponseCookie c in allCookies)
                request.AddCookie(c.Name, c.Value);

            response = client.Execute(request);

            //Store new cookies
            allCookies.AddRange(response.Cookies);

            //Check that we accessed the site
            if (response.StatusCode != HttpStatusCode.OK)
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);

            MatchCollection providerList = Regex.Matches(response.Content, "(<tr class=\"FormtableData\">)", RegOpt);

            //Check if we have multiple providers
            if (providerList.Count == 0)
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);
            else if (providerList.Count > 1)
                return Result<IRestResponse>.Failure(ErrorMsg.MultipleProvidersFound);

            //Get the directory the details for this provider are in
            Match fields = Regex.Match(response.Content, "id=\"licenseCheckBox_1\"\\svalue=\"(\\d+)\"", RegOpt);
            string folderRsn = fields.Groups[1].ToString();
            client = new RestClient(baseUrl + "/publicsearch_detail.jsp?folderRsn=" + folderRsn);
            request = new RestRequest(Method.POST);

            //Add cookies to our request
            foreach (RestResponseCookie c in allCookies)
                request.AddCookie(c.Name, c.Value);

            response = client.Execute(request);

            //Get new cookies
            allCookies.AddRange(response.Cookies);

            //Check result
            if (response.StatusCode == HttpStatusCode.OK)
                return Result<IRestResponse>.Success(response);
            else
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSearchResultsPage);
        }
    }
}
