using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LBPCPlugIn
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
            string baseUrl = "https://www.lpcboard.org/";
            string orgName = provider.GetData("orgName").ToUpper();

            
                if (orgName.Contains("COUNSELORS"))
                {
                    if (!provider.GetData("drtitle").Equals(string.Empty))
                    {
                        return CSearch(baseUrl + "licensee-search-results");
                    }
                    else
                    {
                        return Result<IRestResponse>.Failure("invalid or missing field 'drtitle'");
                    }
                }   
                else if (orgName.Contains("DISCIPLINARY"))
                {
                    return DSearch(baseUrl + "?action=licensee.snip_disciplines.snip&sortBy=date&order=DESC");
                }
                else
                {
                    return Result<IRestResponse>.Failure("invalid field for 'orgName'");
                }
            

        }


        private Result<IRestResponse> CSearch(string targetUrl)
        {

            string type = "C";
            StringBuilder builder = new StringBuilder();
            builder.Append("?fn=");
            builder.Append(provider.FirstName);
            builder.Append("&ln=");
            builder.Append(provider.LastName);
            builder.Append("&c=&p=");
            builder.Append(provider.GetData("drtitle"));
            string query = builder.ToString();

            RestClient client = new RestClient(targetUrl + query);
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return Result<IRestResponse>.Success(response, type);
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
            }
        }

        private Result<IRestResponse> DSearch(string targetUrl)
        {

            string type = "D";
            RestClient client = new RestClient(targetUrl);
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return Result<IRestResponse>.Success(response, type);
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
            }

        }

    }
}
