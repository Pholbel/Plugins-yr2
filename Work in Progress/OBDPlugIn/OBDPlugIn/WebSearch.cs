using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OBDPlugIn
{
    public class WebSearch
    {
        private Provider provider { get; set; }

        public WebSearch(Provider _provider)
        {
            this.provider = _provider;
        }

        public Result<IRestResponse> Search(string url, bool numbersOnly)
        {
            try
            {
                string license = provider.LicenseNumber;

                if (numbersOnly)
                {
                    Match m = Regex.Match(license, "[A-Z](?<NUMBERS>.*)", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        license = m.Groups["NUMBERS"].ToString();
                    }
                }

                RestClient client = new RestClient(url);
                RestRequest request = new RestRequest(Method.GET);
                request.AddQueryParameter("searchby", "licno");
                request.AddQueryParameter("searchfor", license);
                request.AddQueryParameter("stateselect", "none");
                request.AddQueryParameter("lictype", "All Licensee");
                request.AddQueryParameter("Submit", "Search");
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return Result<IRestResponse>.Success(response);
                }
                else
                {
                    return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
                }
            }
            catch (Exception ex)
            {
                return Result<IRestResponse>.Exception(ex);
            } 
        }


        public Result<IRestResponse> SearchPharm(string url)
        {
            try
            {
                RestClient client = new RestClient(url);
                RestRequest request = new RestRequest(Method.GET);
                request.AddQueryParameter("searchby", "licnumber");
                request.AddQueryParameter("searchfor", provider.LicenseNumber);
                request.AddQueryParameter("stateselect", "none");
                request.AddQueryParameter("SubmitPerson", "Search for a Person");
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return Result<IRestResponse>.Success(response);
                }
                else
                {
                    return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
                }
            }
            catch (Exception ex)
            {
                return Result<IRestResponse>.Exception(ex);
            } 
        }



    }
}
