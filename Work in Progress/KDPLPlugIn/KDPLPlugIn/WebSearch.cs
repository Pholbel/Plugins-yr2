using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace KDPLPlugIn
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
            // CHECK THAT THERE EXISTS ORGNAME
            if (provider.GetData("orgName") == string.Empty && provider.LicenseNumber == string.Empty)
            {
                return Result<IRestResponse>.Failure(ErrorMsg.LicAnd_OrgNameOrFirstLastName);
            }

            // PARAMETERS AND COOKIES WE WILL GET WITH FIRST GET
            Dictionary<string, string> boards = new Dictionary<string, string>();
            List<RestResponseCookie> allCookies = new List<RestResponseCookie>();
            string viewState = "";
            string viewStateGenerator = "";
            string eventValidation = "";
            string lastFocus = "";
            string eventTarget = "";
            string eventArgument = "";
            string baseUrl = "http://oop.ky.gov/";

            //GET PARAMETERS AND COOKIES
            RestClient client = new RestClient(baseUrl + "lic_search.aspx");
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            
            allCookies.AddRange(response.Cookies);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                GetBoardValues(ref boards, response);
                GetViewStates(ref viewState, ref viewStateGenerator, ref eventValidation, ref lastFocus, ref eventTarget, ref eventArgument, response);
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
            }

            //FORMING NEW POST WITH OUR PARAMS
            request = new RestRequest(Method.POST);

            string _param_prefix = "ctl00$ContentPlaceHolder2$";

            request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            request.AddParameter("__EVENTTARGET", eventTarget);
            request.AddParameter("__EVENTARGUMENT", eventArgument);
            request.AddParameter("__LASTFOCUS", lastFocus);
            request.AddParameter("__VIEWSTATE", viewState);
            request.AddParameter("__VIEWSTATEGENERATOR", viewStateGenerator);
            request.AddParameter("__EVENTVALIDATION", eventValidation);
            request.AddParameter(_param_prefix + "chkBoards$2", boards[provider.GetData("orgName")]);
            request.AddParameter(_param_prefix + "TFname", provider.FirstName);
            request.AddParameter(_param_prefix + "TLname", provider.LastName);
            request.AddParameter(_param_prefix + "TLicno", provider.LicenseNumber);
            request.AddParameter(_param_prefix + "DStatus", string.Empty);
            request.AddParameter(_param_prefix + "TBname", string.Empty);
            request.AddParameter(_param_prefix + "TBlicno", string.Empty);
            request.AddParameter(_param_prefix + "DBstatus", string.Empty);
            request.AddParameter(_param_prefix + "TCity", string.Empty);
            request.AddParameter(_param_prefix + "dlState", string.Empty);
            request.AddParameter(_param_prefix + "TZip", string.Empty);
            request.AddParameter(_param_prefix + "BSrch", "Search");
            request.AddParameter(_param_prefix + "HBoards", string.Empty);

            foreach (var c in allCookies)
            {
                request.AddCookie(c.Name, c.Value);
            }

            response = client.Execute(request);

            //CHECK IF NO RESULTS
            Match noMatchesFound = Regex.Match(response.Content, "No Matches Found", RegOpt);
            if (noMatchesFound.Success) { return Result<IRestResponse>.Failure(ErrorMsg.NoResultsFound); }

            //CHECK IF WE HAVE MULTIPLE PROVIDERS
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(response.Content);
            HtmlDocument tableParent = new HtmlDocument();
            tableParent.LoadHtml(doc.GetElementbyId("ContentPlaceHolder2_LData").LastChild.InnerHtml);
            var table = tableParent.DocumentNode.SelectSingleNode("//table[@class='tablestyle13']");

            if (table.ChildNodes.Count > 4)
            {
                return Result<IRestResponse>.Failure(ErrorMsg.MultipleProvidersFound);
            } 
            else
            {
                return Result<IRestResponse>.Success(response);
            }

        }

        private void GetBoardValues(ref Dictionary<string, string> boards, IRestResponse response)
        {
            MatchCollection values = Regex.Matches(response.Content, "(?<=ContentPlaceHolder2_chkBoards_\\d.*value=\")\\d*(?=\")", RegOpt);
            MatchCollection keys = Regex.Matches(response.Content, "(?<=for=.ContentPlaceHolder2_chkBoards_\\d*.>)[\\s\\w]*", RegOpt);

            for (var i = 0; i < keys.Count; i++)
            {
                boards.Add(keys[i].ToString(), values[i].ToString());
            }
        }

        private void GetViewStates(ref string viewState, ref string viewStateGenerator, ref string eventValidation, ref string lastFocus, ref string eventTarget, ref string eventArgument, IRestResponse response)
        {
            Match m = Regex.Match(response.Content, "id=\"__VIEWSTATE\" value=\"(?<VIEW>.*?)\"", RegOpt);
            if (m.Success)
            {
                viewState = m.Groups["VIEW"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__VIEWSTATEGENERATOR\" value=\"(?<VIEWGEN>.*?)\"", RegOpt);
            if (m.Success)
            {
                viewStateGenerator = m.Groups["VIEWGEN"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__EVENTVALIDATION\" value=\"(?<EVENT>.*?)\"", RegOpt);
            if (m.Success)
            {
                eventValidation = m.Groups["EVENT"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__LASTFOCUS\" value=\"(?<EVENT>.*?)\"", RegOpt);
            if (m.Success)
            {
                lastFocus = m.Groups["EVENT"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__EVENTTARGET\" value=\"(?<EVENTTAR>.*?)\"", RegOpt);
            if (m.Success)
            {
                eventTarget = m.Groups["EVENTTAR"].ToString();
            }
            m = Regex.Match(response.Content, "id=\"__EVENTARGUMENT\" value=\"(?<EVENTARG>.*?)\"", RegOpt);
            if (m.Success)
            {
                eventArgument = m.Groups["EVENTARG"].ToString();
            }
        }
    }
}
