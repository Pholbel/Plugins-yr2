using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ARDCPlugIn
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

            // valid cases:
            //
            // CASE1: lic_no -> PROCEED
            // CASE2: lic_no, drtitle, orgName -> PROCEED
            //
            // no license number:
            //
            // CASE3: drtitle, orgName -> STOP
            // CASE4: drtitle -> STOP
            // CASE5: orgName -> STOP
            //
            // license number but only one of the two, board and type:
            //
            // CASE6: lic_no, drtitle -> STOP
            // CASE7: lic_no, orgName ->
            string lic_no = provider.LicenseNumber;
            string drtitle = provider.GetData("drtitle");
            string orgName = provider.GetData("orgName");

            if (lic_no == string.Empty)
            {
                // CASE 3,4,5
                return Result<IRestResponse>.Failure("missing field: lic_no");
            }
            if (drtitle != string.Empty && orgName == string.Empty)
            {
                // CASE 6
                return Result<IRestResponse>.Failure("missing field: orgName");
            }
            if (drtitle == string.Empty && orgName != string.Empty)
            {
                // CASE 7
                return Result<IRestResponse>.Failure("missing field: drtitle");
            }

            
            // declarations
            string body;
            string keyval = "{0}={1}&";
            string keyvalLast = keyval.Substring(0,(keyval.Length-1));
            string viewState = "";
            string viewStateVersion = "";
            string viewStateMAC = "";
            string KviewState = "com.salesforce.visualforce.ViewState";
            string KviewStateVersion = "com.salesforce.visualforce.ViewStateVersion";
            string KviewStateMAC = "com.salesforce.visualforce.ViewStateMAC";
            string jid64 = WebUtility.UrlEncode("j_id0:j_id61:j_id62:j_id64");
            string board = jid64 + WebUtility.UrlEncode(":Menu:Individuals:j_id94");
            string type = jid64 + WebUtility.UrlEncode(":Menu:Individuals:j_id96");
            string licenseNumber = jid64 + WebUtility.UrlEncode(":Menu:Individuals:LicenseNumber");
            string baseUrl = "https://elicense.az.gov/ARDC_LicenseSearch";
            StringBuilder builder = new StringBuilder();

            // first request to get viewstate
            RestClient client = new RestClient(baseUrl);
            RestRequest request = new RestRequest(Method.GET);
            request.AddHeader("cache-control", "no-cache");
            IRestResponse response = client.Execute(request);
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                GetViewStates(ref viewState, ref viewStateVersion, ref viewStateMAC, response);
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
            }

            // second request to execute search
            request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("content-type", "application/x-www-form-urlencoded");

            // build out body of request
            builder.AppendFormat(keyval, jid64, jid64);
            builder.AppendFormat(keyval, board, WebUtility.UrlEncode(orgName));
            builder.AppendFormat(keyval, type, WebUtility.UrlEncode(drtitle));
            builder.AppendFormat(keyval, licenseNumber, lic_no);
            builder.AppendFormat(keyval, KviewState, WebUtility.UrlEncode(viewState));
            builder.AppendFormat(keyval, KviewStateVersion, WebUtility.UrlEncode(viewStateVersion));
            builder.AppendFormat(keyvalLast, KviewStateMAC, WebUtility.UrlEncode(viewStateMAC));
            body = builder.ToString();
            request.AddParameter("application/x-www-form-urlencoded", body, ParameterType.RequestBody);

            // execute post
            response = client.Execute(request);


            // SEARCH SUCCESS
            if (response.StatusCode == HttpStatusCode.OK)
            {
                // check that board and type are valid values
                if (!response.Content.Contains(drtitle)) { return Result<IRestResponse>.Failure("invalid field: drtitle"); }
                if (!response.Content.Contains(orgName)) { return Result<IRestResponse>.Failure("invalid field: orgName"); }

                return Result<IRestResponse>.Success(response);
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSearchResultsPage);
            }


            // GO TO DETAILS PAGE

            
        }

        private void GetViewStates(ref string viewState, ref string viewStateVersion, ref string viewStateMAC, IRestResponse response)
        {
            viewState = Regex.Match(response.Content, "(?<=ViewState\".value=.).*(?=\"./><input.*ViewStateVersion)", RegOpt).ToString();
            viewStateVersion = Regex.Match(response.Content, "(?<=ViewStateVersion\".value=.).*(?=\"./><input.*ViewStateMAC)", RegOpt).ToString();
            viewStateMAC = Regex.Match(response.Content, "(?<=ViewStateMAC\".value=.).*(?=\"./></span)", RegOpt).ToString();
        }
    }
}
