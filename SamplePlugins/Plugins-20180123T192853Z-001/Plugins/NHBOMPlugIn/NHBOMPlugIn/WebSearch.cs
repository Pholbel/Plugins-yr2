using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NHBOMPlugIn
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
            List<RestResponseCookie> allCookies = new List<RestResponseCookie>();
            string viewState = "";
            string viewStateGenerator = "";
            string eventValidation = "";
            string lastFocus = "";
            string eventTarget = "";
            string eventArgument = "";

            //RestClient client = new RestClient("http://business.nh.gov/medicineboard/Search.aspx");
            RestClient client = client = new RestClient("http://business.nh.gov/MedicineBoard/Disclaimer.aspx");
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            
            allCookies.AddRange(response.Cookies);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                GetViewStates(ref viewState, ref viewStateGenerator, ref eventValidation, ref lastFocus, ref eventTarget, ref eventArgument, response);
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
            }

            client = new RestClient("http://business.nh.gov/MedicineBoard/Disclaimer.aspx");
            request = new RestRequest(Method.POST);
            request.AddParameter("__VIEWSTATE", viewState);
            request.AddParameter("__VIEWSTATEGENERATOR", viewStateGenerator);
            request.AddParameter("__EVENTVALIDATION", eventValidation);
            request.AddParameter("ctl00$cphMain$btnAgree", "Accept");
            foreach (var c in allCookies)
            {
                request.AddCookie(c.Name, c.Value);
            }
            response = client.Execute(request);
            allCookies.AddRange(response.Cookies);


            if (response.StatusCode == HttpStatusCode.OK)
            {
                GetViewStates(ref viewState, ref viewStateGenerator, ref eventValidation, ref lastFocus, ref eventTarget, ref eventArgument, response);
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSite);
            }

            client = new RestClient("http://business.nh.gov/medicineboard/Search.aspx");
            request = new RestRequest(Method.POST);
            request.AddParameter("__LASTFOCUS", lastFocus);
            request.AddParameter("__VIEWSTATE", viewState);
            request.AddParameter("__VIEWSTATEGENERATOR", viewStateGenerator);
            request.AddParameter("__EVENTTARGET", eventTarget);
            request.AddParameter("__EVENTARGUMENT", eventArgument);
            request.AddParameter("__EVENTVALIDATION", eventValidation);
            request.AddParameter("ctl00$cphMain$rblChoice", "rdoBoth");
            request.AddParameter("ctl00$cphMain$txtLName", "");
            request.AddParameter("ctl00$cphMain$txtLicense", provider.LicenseNumber);
            request.AddParameter("ctl00$cphMain$btnLicense", "License");
            request.AddParameter("ctl00$cphMain$ddlSpecialty", "-1");
            //request.AddParameter("ctl00$cphMain$btnRight", "next");
            foreach (var c in allCookies)
            {
                request.AddCookie(c.Name, c.Value);
            }
            response = client.Execute(request);


            if (response.StatusCode == HttpStatusCode.OK)
            {
                return Result<IRestResponse>.Success(response);
            }
            else
            {
                return Result<IRestResponse>.Failure(ErrorMsg.CannotAccessSearchResultsPage);
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
