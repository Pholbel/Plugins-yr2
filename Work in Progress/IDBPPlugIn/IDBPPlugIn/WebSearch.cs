using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IDBPPlugIn
{
    public class WebSearch
    {
        private RegexOptions RegOpt = RegexOptions.IgnoreCase;
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
            RestRequest request = new RestRequest(Method.POST);
            string baseUrl = "https://www.azbbhe.us/";
            string searchQuery = "DaEngine.asp";
            RestClient client = new RestClient(baseUrl + searchQuery);

            // ADD PARAMETERS
            request.AddParameter("LicSearch", "");
            request.AddParameter("TypeSearch", "LicenseNo");
            request.AddParameter("DaInBox", provider.LicenseNumber);
            request.AddParameter("B1", "Submit");
            
            foreach (var c in allCookies)
            {
                request.AddCookie(c.Name, c.Value);
            }

            IRestResponse response = client.Execute(request);


            //REFILL THE COOKIE JAR
            allCookies.AddRange(response.Cookies);

            Match detailLink = Regex.Match(response.Content, @"ProDetail.*(?<==.*\d)", RegOpt);
            
            if (detailLink.Success)
            {
               
                client = new RestClient(baseUrl + detailLink);
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
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);
            }
            
        }

    }
}
