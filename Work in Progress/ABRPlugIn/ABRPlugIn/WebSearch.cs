using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ABRPlugIn
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
            // check for last name
            if (provider.LastName.Equals(string.Empty))
            {
                return Result<IRestResponse>.Failure("Missing Field: Last Name");
            }

            // form query url
            string baseUrl = "https://www.theabr.org/myabr/find-a-radiologist";
            string _param_ = string.Format("?fn={0}&ln={1}&st={2}", provider.FirstName, provider.LastName, provider.GetData("state"));

            // initial GET request 
            RestClient client = new RestClient(baseUrl+_param_);
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            response = client.Execute(request);

            // check for multiple providers
            MatchCollection practiceLocationsList = Regex.Matches(response.Content, "practice locations", RegOpt);
            if (practiceLocationsList.Count > 1)
            {
                return Result<IRestResponse>.Failure("Error: Multiple Providers found");
            }

            // success
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return Result<IRestResponse>.Success(response);
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
            }
        }
    }
}
