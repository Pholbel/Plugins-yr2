using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AMCBPlugIn
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
            string baseUrl = "https://ams.amcbmidwife.org/amcbssa/f";

            //Set up security protocol
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //Set up client and request
            RestClient client = new RestClient(baseUrl);
            client.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
            RestRequest request = new RestRequest(Method.GET);
            request.AddQueryParameter("p", "AMCBSSA:17800");

            //Execute request
            if (!ExecuteRequest(client, request, ref allCookies, out IRestResponse response))
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);

            //Search By
            client.BaseUrl = new Uri("https://ams.amcbmidwife.org/amcbssa/wwv_flow.show");
            NewPostRequest(allCookies, out request);

            //TODO: Remove if unnecessary 
            //request.AddParameter("p_request", "PLUGIN=" + Regex.Match(response.Content, "ajaxIdentifier\":\"(?<identifier>[0-9A-Z]+)\"", RegOpt).Groups["identifier"].Value);

            string step_id = Regex.Match(response.Content, "p_flow_step_id\" value=\"(?<id>\\d+)\"", RegOpt).Groups["id"].Value;
            string[] args = { "FIRSTNAME", "LASTNAME", "SEARCH_FLG", "CERT_NUMBER" };

            //Form parameters
            request.AddParameter("p_request", "APXWGT");
            request.AddParameter("p_flow_id", Regex.Match(response.Content, "p_flow_id\" value=\"(?<id>\\d+)\"", RegOpt).Groups["id"].Value);
            request.AddParameter("p_flow_step_id", step_id);
            request.AddParameter("p_instance", Regex.Match(response.Content, "p_instance\" value=\"(?<id>\\d+)\"", RegOpt).Groups["id"].Value);
            request.AddParameter("p_debug", "");

            //Argument parameters
            foreach (string s in args)
                request.AddParameter("p_arg_names", $"P{step_id}_{s}");

            request.AddParameter("p_arg_values", provider.FirstName);
            request.AddParameter("p_arg_values", provider.LastName);
            request.AddParameter("p_arg_values", "Y");
            request.AddParameter("p_arg_values", provider.LicenseNumber);

            //Misc params
            request.AddParameter("p_widget_action", "reset");
            request.AddParameter("x01", Regex.Match(response.Content, "<div id=\"report_(?<val>\\d+)_catch\">", RegOpt).Groups["val"].Value);
            request.AddParameter("p_widget_name", "classic_report");

            //Execute request
            if (!ExecuteRequest(client, request, ref allCookies, out response))
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessDetailsPage);

            //Check for multiple responses
            if (!Regex.Match(response.Content, @"row\(s\) 1 - 1 of 1", RegOpt).Success)
                return Result<IRestResponse>.Failure(ErrorMsg.MultipleProvidersFound);

            return Result<IRestResponse>.Success(response);
        }

        void NewPostRequest(List<RestResponseCookie> cookies, out RestRequest request)
        {
            //Forming new post request with our parameters
            request = new RestRequest(Method.POST);

            //Headers
            request.AddHeader("X-Requested-With", "XMLHttpRequest");

            //Add cookies to our request
            foreach (RestResponseCookie c in cookies)
                request.AddCookie(c.Name, c.Value);
        }

        bool ExecuteRequest(RestClient client, RestRequest request, ref List<RestResponseCookie> cookies, out IRestResponse response)
        {
            //Execute our request
            response = client.Execute(request);

            //Store new cookies
            cookies.AddRange(response.Cookies);

            return response.StatusCode == HttpStatusCode.OK;
        }
    }
}
