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

namespace DCNAPlugIn
{
    public class WebSearch
    {
        private Provider provider { get; set; }
        private RestClient client { get; set; }
        private RestRequest request { get; set; }
        private IRestResponse response { get; set; }
        private List<RestResponseCookie> cookieJar { get; set; }
        private System.Data.DataRow dr { get; set; }

        public WebSearch(Provider _provider, System.Data.DataRow _dr)
        {
            this.provider = _provider;
            this.cookieJar = new List<RestResponseCookie>();
            this.dr = _dr;
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
                lnumber = provider.LicenseNumber
            };

            //creating the post request using restsharp
            string query = "&licstring=" + reqObj.lnumber + "&sort_fl=NAME&searches=1";
            client = new RestClient("https://www.asisvcs.com/services/registry/Results_by_licno.asp?CPCat=0709NURSE" + query);
            request = new RestRequest(Method.POST);
            request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            request.AddHeader("Content-type", "application/x-www-form-urlencoded");
            response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var providerList = Deserialize(response.Content);

                if (providerList != null)
                {
                    if (providerList.Count == 1)
                    {
                        client = new RestClient("https://www.asisvcs.com/services/registry/" + providerList[0].Query);
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

        public List<ProviderObject> Deserialize(string responseContent)
        {
            List<ProviderObject> returnList = new List<ProviderObject>();

            //html was received so deserialize it into provider list
            string sdoc = responseContent;
            HtmlAgilityPack.HtmlDocument hdoc = new HtmlAgilityPack.HtmlDocument();
            hdoc.LoadHtml(sdoc);

            //var body = hdoc.DocumentNode.SelectNodes("//body").Single();

            var table = hdoc.DocumentNode
                .Descendants("tr")
                .Where(d =>
                d.ParentNode.Attributes.Contains("id")
                &&
                d.ParentNode.Attributes["id"].Value.Contains("results")
            );

            

            foreach (HtmlAgilityPack.HtmlNode n in table)
            {
                var node = new ProviderObject();

                if (n.FirstChild.Name == "td")
                {
                    node.Query = n.FirstChild.FirstChild.Attributes["href"].Value;

                    returnList.Add(node);

                } else { continue; }

            }

            return returnList;
        }
    }
}
