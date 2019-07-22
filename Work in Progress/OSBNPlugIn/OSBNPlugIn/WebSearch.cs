using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace OSBNPlugIn
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

            // DECLARATIONS
            string keyValue = "{0}={1}&";
            string keyValueLast = keyValue.Substring(0, keyValue.Length - 1);
            string baseUrl = "https://osbn.oregon.gov/OSBNVerification/";
            StringBuilder searchUrl = new StringBuilder();
            searchUrl.Append(baseUrl + "SearchResults.aspx?");
            string searchtype;


            // CHECK THAT EITHER LICENSE NUMBER OR LAST NAME EXISTS
            if (provider.LastName == string.Empty && provider.LicenseNumber == string.Empty)
            {
                return Result<IRestResponse>.Failure(ErrorMsg.InvalidLicenseAndLastName);
            }


            // DETERMINE SEARCHTYPE
            if (provider.LicenseNumber == string.Empty)
            {
                searchtype = "name";
            }
            else
            {
                searchtype = "license";
            }


            searchUrl.AppendFormat(keyValue, "searchtype", searchtype);
            if (searchtype == "license")
            {
                searchUrl.AppendFormat(keyValueLast, "license", provider.LicenseNumber);
            }
            else
            {
                searchUrl.AppendFormat(keyValue, "lastname", provider.LastName);
                searchUrl.AppendFormat(keyValue, "firstname", provider.FirstName);
                searchUrl.AppendFormat(keyValue, "city", provider.GetData("city"));
                searchUrl.AppendFormat(keyValueLast, "zip", provider.GetData("req_zip"));
            }


            // GET FOR RESULTS
            RestClient client = new RestClient(searchUrl.ToString());
            RestRequest request = new RestRequest(Method.GET);
            request.AddHeader("Accept", "text/html;");
            IRestResponse response = client.Execute(request);
            
            if (response.StatusCode == HttpStatusCode.OK)
            {

                // CHECK FOR MULTIPLE PROVIDERS
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(response.Content);
                HtmlNode table = doc.GetElementbyId("ctl00_MainContent_gvSearchResult");
                if (table.ChildNodes.Count > 4)
                {
                    return Result<IRestResponse>.Failure(ErrorMsg.MultipleProvidersFound);
                }

                // SUCCESSFUL SEARCH, NOW GO TO DETAILS PAGE
                string detailUrl = Regex.Match(table.InnerHtml, "Details[\\s.\\w?=-]*", RegOpt).ToString();
                RestClient detailClient = new RestClient(baseUrl + detailUrl);
                RestRequest detailRequest = new RestRequest(Method.GET);
                IRestResponse detailResponse = detailClient.Execute(detailRequest);
                return Result<IRestResponse>.Success(detailResponse);
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
            }

            
        }

    }
}
