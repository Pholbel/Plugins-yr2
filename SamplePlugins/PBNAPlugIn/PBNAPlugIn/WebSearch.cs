/* 
 * Tim Ferido 
 * timferido@gmail.com
 * 2-27-18
 * 14:44 UTC-8
 */

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

namespace PBNAPlugIn
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
                fname = provider.FirstName,
                lname = provider.LastName,
                lnumber = provider.LicenseNumber
            };

            string search_url1 = "https://pulseportal.com/Inquiry/searchEntityForProducerInfoSimple.do?method=menuInit&criteriaNextAction=producerInformationSimple&moduleCode=PRDCR_LIC&serviceCode=RQST_CNSMR&accessCode=PA&functionCode=NA";
            string search_url2 = "https://pulseportal.com/Inquiry/searchEntityForProducerInfoSimple.do?navigationHistoryLevel=2";


            /* Let's get the cookies */

            client = new RestClient(search_url1);
            request = new RestRequest(Method.GET);
            response = client.Execute(request);
            cookieJar.AddRange(response.Cookies);
            

            /* Now POST to nav level 2 */

            client = new RestClient(search_url2);
            request = new RestRequest(Method.POST);
            foreach (var c in cookieJar) { request.AddCookie(c.Name, c.Value); }

            request.AddParameter("method", "search");
            request.AddParameter("displayAlternateEntity", "false");
            request.AddParameter("reversedRelationship", "false");
            request.AddParameter("searchNextActionMethod", "selectIndividual");
            request.AddParameter("criteriaNextAction", "producerInformationSimple");
            request.AddParameter("criteriaNextActionAnchor", "");
            request.AddParameter("criteriaId", "");
            request.AddParameter("entityType", "IL");
            request.AddParameter("titleParameter", "");
            request.AddParameter("headerKeyParameter","");
            request.AddParameter("enableCreateEntity", "false");
            request.AddParameter("additionalFieldValue", "");
            request.AddParameter("criteriaLicenseNumber", provider.LicenseNumber);
            request.AddParameter("licenseType", "NA");
            request.AddParameter("criteriaLastName", provider.LastName);
            request.AddParameter("criteriaFirstName", provider.FirstName);
            request.AddParameter("city", "");
            request.AddParameter("displayEntityTypes", "Y");
            request.AddParameter("disableEntityTypes", "");
            request.AddParameter("maxRecords", "10");
            request.AddParameter("maxRecords", "10");
            request.AddParameter("buttons.search.name", "Next>>");
            
            /* Voila */ 

            response = client.Execute(request);
           
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return Result<IRestResponse>.Success(response);
            }
            return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSearchResultsPage);
        }
    }
}

