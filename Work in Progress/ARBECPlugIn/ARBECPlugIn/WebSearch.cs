using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ARBECPlugIn
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
            string MM_query = "form1";
            string zip = "";
            string city = "";
            string bName = "";
            string LName = provider.LastName;
            string license = provider.LicenseNumber;
            string baseUrl = "http://search.statesolutions.us";

            //GET PARAMETERS AND COOKIES
            RestClient client = new RestClient(baseUrl+ "/?A=ABEC&AID=RS");
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            
            allCookies.AddRange(response.Cookies);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
            }

            //FORMING NEW POST WITH OUR PARAMS
            request = new RestRequest(Method.POST);

            request.AddParameter("MM_query", MM_query);
            request.AddParameter("zip", zip);
            request.AddParameter("city", city);
            request.AddParameter("bName", bName);
            request.AddParameter("LName", LName);
            request.AddParameter("license", license);
            
            foreach (var c in allCookies)
            {
                request.AddCookie(c.Name, c.Value);
            }

            response = client.Execute(request);

            //LAND HO!

            //REFILL THE COOKIE JAR
            allCookies.AddRange(response.Cookies);
            if (response.StatusCode != HttpStatusCode.OK)
            {
             return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite); }

            //CHECK IF WE HAVE MULTIPLE PROVIDERS

            MatchCollection providerList = Regex.Matches(response.Content, "mDate", RegOpt);

            if (providerList.Count == 0)
            {
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);
            }
            else if (providerList.Count == 1)
            {
                Match match = Regex.Match(response.Content, @"(?<=href..).*/Licensee.*(?=..MORE)");

                string detailLink = match.ToString();
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
                return Result<IRestResponse>.Failure(ErrorMsg.MultipleProvidersFound);
            }          
        }
    }
}
