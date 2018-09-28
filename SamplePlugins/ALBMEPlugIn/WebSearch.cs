using Newtonsoft.Json;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ALBMEPlugIn
{
    public class WebSearch
    {
        private Provider provider { get; set; }
        private RestClient client { get; set; }
        private RestRequest request { get; set; }
        private IRestResponse response { get; set; }
        private List<RestResponseCookie> cookieJar { get; set; }

        public WebSearch(Provider _provider)
        {
            this.provider = _provider;
            this.cookieJar = new List<RestResponseCookie>();
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
            RequestObject reqObj = new RequestObject()
            {
                fname = provider.FirstName,
                lname = provider.LastName,
                lnumber = Regex.Match(provider.LicenseNumber, @"(\d+)(?!.*\d)").Value,
                lictype = LicenseTypeLookup(provider.LicenseNumber),
                page = 1,
                pageSize = 20,
                sortby = string.Empty,
                sortexp = string.Empty,
                county = "-1",
                sdata = new List<object>()
            };

            client = new RestClient("https://abme.igovsolution.com/online/JS_grd/Grid.svc/GetIndv_license");
            //client = new RestClient("https://abme.igovsolution.com/online/Lookups/Individual_Lookup.aspx");
            request = new RestRequest(Method.POST);
            request.AddHeader("Accept", "application/json, text/javascript, */*; q=0.01");
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", JsonConvert.SerializeObject(reqObj), ParameterType.RequestBody);
            response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                ResponseObject root = JsonConvert.DeserializeObject<ResponseObject>(response.Content);

                ResponseDetail responseObj = (root != null) ? JsonConvert.DeserializeObject<ResponseDetail>(root.d) : null;

                List<ProviderObject> providerList = (responseObj != null) ? JsonConvert.DeserializeObject<List<ProviderObject>>(responseObj.Response) : null;

                if (providerList != null)
                {
                    if (providerList.Count == 1)
                    {
                        client = new RestClient(String.Format("https://abme.igovsolution.com/online/ABME_Prints/Print_MD_DO_Laspx.aspx?appid={0}", providerList[0].App_ID));
                        request = new RestRequest(Method.GET);
                        response = client.Execute(request);

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            return Result<IRestResponse>.Success(response);
                        }
                    }
                    else if (providerList.Count == 0)
                    {
                        return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);
                    }
                    else
                    {
                        return Result<IRestResponse>.Failure(ErrorMsg.MultipleProvidersFound);
                    }
                }
            }

            return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSearchResultsPage);
        }


        public string LicenseTypeLookup(string license)
        {
            string parsedTitle = Regex.Replace(license, "[^a-zA-Z]", "", RegexOptions.None);
            parsedTitle = !String.IsNullOrEmpty(parsedTitle) ? parsedTitle : provider.GetData("drtitle");

            switch (parsedTitle.ToUpper())
            {
                case "MD":
                    return "1";
                case "DO":
                    return "2";
                case "L":
                    return "3";
                case "PA":
                    return "4";
                case "AA":
                    return "5";
                case "TA":
                    return "6";
                case "SP":
                    return "7";
                case "RA":
                    return "10";
                case "CP":
                    return "11";
                case "ACSC":
                    return "12";
                case "QACS":
                    return "13";
                case "QACSCNP":
                    return "14";
                case "LPSP":
                    return "16";
                default:
                    return "-1";
            }
        }

    }
}
