using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace FlorPlugIn
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
            List<RestResponseCookie> allCookies = new List<RestResponseCookie>();
            string baseUrl = "https://appsmqa.doh.state.fl.us/MQASearchServices/HealthCareProviders";

            RestClient client = new RestClient(baseUrl);
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            
            allCookies.AddRange(response.Cookies);

            request = new RestRequest(Method.POST);

            string _param_prefix = "SearchDto.";

            request.AddParameter(_param_prefix + "Board", "");
            request.AddParameter(_param_prefix + "Profession", "");
            request.AddParameter(_param_prefix + "LicenseNumber", provider.LicenseNumber);
            request.AddParameter(_param_prefix + "BusinessName", "");
            request.AddParameter(_param_prefix + "LastName", provider.LastName);
            request.AddParameter(_param_prefix + "FirstName", "");
            request.AddParameter(_param_prefix + "City", "");
            request.AddParameter(_param_prefix + "County", "");
            request.AddParameter(_param_prefix + "ZipCode", "");
            request.AddParameter(_param_prefix + "LicenseStatus", "ALL");

            foreach (var c in allCookies)
            {
                request.AddCookie(c.Name, c.Value);
            }

            response = client.Execute(request);
            allCookies.AddRange(response.Cookies);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                //if there are multiple results, find the one that matches the given license number
                MatchCollection matches = Regex.Matches(response.Content, "<a href=\"/MQASearchServices/(?<LINK>.*?)\">(?<LICENSE>.*?)<", RegOpt);

                if (matches.Count > 0 && Regex.IsMatch(response.Content, "search results total..[1-9]", RegexOptions.IgnoreCase))
                {
                    foreach (Match m in matches)
                    {
                        if (m.Groups["LICENSE"].ToString() == provider.LicenseNumber)
                        {
                            client = new RestClient(string.Format("https://appsmqa.doh.state.fl.us/MQASearchServices/{0}", HttpUtility.HtmlDecode(m.Groups["LINK"].ToString())));
                            request = new RestRequest(Method.POST);
                            foreach (var c in allCookies)
                            {
                                request.AddCookie(c.Name, c.Value);
                            }
                            response = client.Execute(request);

                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                return Result<IRestResponse>.Success(response);
                            }
                            else
                            {
                                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSearchResultsPage);
                            }
                        }
                    }
                    return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);
                }
                else if (Regex.IsMatch(response.Content, "no records found", RegOpt))
                {
                    return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);
                }
                else
                {
                    return Result<IRestResponse>.Success(response);
                }
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSearchResultsPage);
            }
        }
    }
}
