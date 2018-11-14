using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VRCVPlugIn
{
    public class WebSearch
    {

        private RegexOptions RegOpt = RegexOptions.IgnoreCase | RegexOptions.Singleline;
        private Provider provider { get; set; }

        //Prefix
        const string DISPLAY_PREFIX = "$PpyDisplayHarness$";

        //Parameter dictionary
        readonly Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            { "pyActivity", "ReloadSection" },
            { "pzFromFrame", "pyDisplayHarness" },
            { "pzPrimaryPageName", "pyDisplayHarness" },
            { "pyEncodedParameters", "true" },
            { "pyKeepPageMessages", "false" },
            { "UITemplatingStatus", "N" },
            { "StreamName", "SearchLicenseLookupForGuest" },
            { "BaseReference", "" },
            { "StreamClass", "Rule-HTML-Section" },
            { "bClientValidation", "true" },
            { "FormError", "NONE" },
            { "pyCustomError", "DisplayErrors" },
            { "HeaderButtonSectionName", "SubSectionSearchLicenseLookupForGuestB" },
            { "ReadOnly", "-1" },
            { "inStandardsMode", "true" }
        };

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
            string baseUrl = "https://secure.professionals.vermont.gov/prweb/PRServletCustom/V9csDxL3sXkkjMC_FR2HrA%5B%5B*/!STANDARD";

            //Set up security protocol
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //Set up client and request
            RestClient client = new RestClient(baseUrl);
            client.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
            RestRequest request = new RestRequest(Method.GET);
            request.AddQueryParameter("UserIdentifier", "LicenseLookupGuestUser");

            //Execute request
            if (!ExecuteRequest(client, request, ref allCookies, out IRestResponse response))
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);

            //Global parameters
            string transactionID = Search(response.Content, "&pzTransactionId=(\\w+)");
            string harnessID = Search(response.Content, "id='pzHarnessID' value='(\\w+)'");
            string id = (int.Parse(Search(response.Content, "AJAXCT' data-json='{\"ID\":(\\d+),")) - 1).ToString();

            //Search By
            NewPostRequest(allCookies, out request);

            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData.Add("PreActivitiesList", "<pagedata><dataTransforms REPEATINGTYPE=\"PageList\"><rowdata REPEATINGINDEX=\"1\"><dataTransform></dataTransform></rowdata></dataTransforms></pagedata>");
            formData.Add("ActivityParams", "&ApplicantType=&Age=&PrimaryState=&LicenseType=&ProductIDs=");
            AddParameters(transactionID, harnessID, id, "SetEligibleProductIDForInternal", formData, response, ref request);
            formData.Clear();

            //Execute request
            if (!ExecuteRequest(client, request, ref allCookies, out response))
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSearchForm);

            //Select By
            NewPostRequest(allCookies, out request);

            formData.Add(DISPLAY_PREFIX + "pSelectProfessions", "Search by Profession Type");
            formData.Add("PreActivitiesList", "<pagedata><dataTransforms REPEATINGTYPE=\"PageList\"><rowdata REPEATINGINDEX=\"1\"><dataTransform>ShowResultsByProfession</dataTransform></rowdata></dataTransforms></pagedata>");
            formData.Add("ActivityParams", "");
            AddParameters(transactionID, harnessID, id, "", formData, response, ref request);
            formData.Clear();

            //Execute request
            if (!ExecuteRequest(client, request, ref allCookies, out response))
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSearchForm);

            //Display Results
            NewPostRequest(allCookies, out request);

            formData.Add(DISPLAY_PREFIX + "pSelectProfessions", "Search by Profession Type");
            formData.Add("D_EligibleLicenseLookupPpxResults1colWidthGBL", "");
            formData.Add("D_EligibleLicenseLookupPpxResults1colWidthGBR", "");
            for (int i = 1; i <= 10; i++)
                formData.Add("$PD_EligibleLicenseLookup_pa1026841396639549pz$ppxResults$l" + i.ToString() + "$ppySelected", "false");
            formData.Add(DISPLAY_PREFIX + "pFirstName", provider.FirstName);
            formData.Add(DISPLAY_PREFIX + "pLastName", provider.LastName);
            formData.Add(DISPLAY_PREFIX + "pLicenseNumber", GetLicenseNum(provider.LicenseNumber));
            formData.Add("PreActivitiesList", "<pagedata><dataTransforms REPEATINGTYPE=\"PageList\"><rowdata REPEATINGINDEX=\"1\"><dataTransform></dataTransform></rowdata></dataTransforms></pagedata>");
            formData.Add("ActivityParams", "&ProductID=&TempID=");
            AddParameters(transactionID, harnessID, id, "InternalLookupResults", formData, response, ref request);
            formData.Clear();

            //Execute request
            if (!ExecuteRequest(client, request, ref allCookies, out response))
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSearchForm);

            MatchCollection providerList = Regex.Matches(response.Content, "DisplayLicenceDetail\\(&#39;(?<Detail>[;\\w ]+)&#39;\\)");

            //Check if we have multiple providers
            if (providerList.Count > 1)
                return Result<IRestResponse>.Failure(ErrorMsg.MultipleProvidersFound);

            //Check if we have no providers
            if (providerList.Count == 0)
                return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound);

            //Navigate to the details page
            request = new RestRequest("licensedetail.aspx", Method.GET);

            //Add cookies to our request
            foreach (RestResponseCookie c in allCookies)
                request.AddCookie(c.Name, c.Value);

            request.AddQueryParameter("id", providerList[0].Groups["Detail"].Value);

            response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessDetailsPage);

            return Result<IRestResponse>.Success(response);
        }

        string GetLicenseNum(string licNo)
        {
            Match m = Regex.Match(licNo, "([-A-Za-z]*?)[-.]?(?<num>[\\d]+)[-.]?([-A-Za-z]*)", RegOpt);
            return m.Groups["num"].Value;
        }

        void AddParameters(string transactionID, string harnessID, string trackID, string preActivity, Dictionary<string, string> formData, IRestResponse response, ref RestRequest request)
        {
            //Query params
            foreach (KeyValuePair<string, string> par in parameters)
                request.AddQueryParameter(par.Key, par.Value);

            request.AddQueryParameter("pzTransactionId", transactionID);
            request.AddQueryParameter("pzHarnessID", harnessID);
            request.AddQueryParameter("AJAXTrackID", trackID);
            request.AddQueryParameter("PreActivity", preActivity);

            //Form data
            foreach (KeyValuePair<string, string> dat in formData)
                request.AddParameter(dat.Key, dat.Value);

            request.AddParameter("$PpyDisplayHarness$pSearchCategory", "Constituent");
            request.AddParameter("EXPANDEDSubSectionSearchLicenseLookupForGuestB", "true");
        }

        string Search(string input, string expression)
        {
            return Regex.Match(input, expression).Groups[1].Value;
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
