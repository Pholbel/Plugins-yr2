using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;

namespace OBDPlugIn
{
    /// <summary>
    /// EchoPSV Plugin for Oregon Board of...
    /// http://www.oregon.gov/pages/index.aspx
    /// </summary>
    public class PlugInClass : IPlugIn
    {
        private RegexOptions RegOpt = RegexOptions.IgnoreCase | RegexOptions.Singleline;

        public override string Fetch(DataRow dr)
        {
            Initialize(dr, true, new SPMConfig() { AcceptAllCertificatePolicy = true, Expect100Continue = false });
            string message = String.Empty;
            string output = String.Empty;

            Result<string> fields = Validate();

            if (fields.IsValid)
            {
                Result<IRestResponse> response = GetResponse(dr["orgName"].ToString());

                if (response.IsValid)
                {
                    WebParse webParse = new WebParse();
                    Result<string> parseResult = webParse.Execute(response.Value);

                    if (parseResult.IsValid)
                    {
                        HandleModes(response.Value.Content, webParse);

                        output = parseResult.Value;
                    }
                    else
                    {
                        message = parseResult.Message;
                    }
                }
                else
                {
                    message = response.Message;
                }
            }
            else
            {
                message = fields.Message;
            }

            return ProcessResults(output, message);
        }

        private Result<IRestResponse> GetResponse(string orgName)
        {
            WebSearch webSearch = new WebSearch(provider);

            switch (orgName.Trim())
            {
                case "Oregon Board of Clinical Social Workers":
                    return webSearch.Search("https://hrlb.oregon.gov/BLSW/LicenseeLookup/searchdir.asp", true);
                case "Oregon Board of Naturopathic Examiners":
                    return webSearch.Search("https://hrlb.oregon.gov/OBNM/LicenseeLookup/searchdir.asp", true);
                case "Oregon Board of Speech Pathology and Audiology":
                    return webSearch.Search("https://hrlb.oregon.gov/BSPA/LicenseeLookup/searchdir.asp", true);
                case "Oregon Board of Massage Therapists":
                    return webSearch.Search("https://hrlb.oregon.gov/OBMT/licenseelookup/searchdir.asp", true);
                case "Oregon Board of Dentistry":
                    return webSearch.Search("http://obd.oregonlookups.com/searchdir.asp", false);
                case "Oregon Board of Pharmacy":
                    return webSearch.SearchPharm("https://obop.oregon.gov/LicenseeLookup/personsearch.asp");
                default:
                    return Result<IRestResponse>.Failure(ErrorMsg.ErrorOccuredWhileQuerying);
            }
        }

        private void HandleModes(string html, WebParse webParse)
        {
            HandleExpirables(provider.ExpirationDate, webParse.Expiration);
            SetSanction(webParse.Sanction);

            try
            {
                // Repair errors in returned html table:
                html = Regex.Replace(html, "<table (align=.?center.? ?)?class=.?bodytext.?.*?<tr.*?>", "<table>", RegOpt);
                pdf.Html = html;
                pdf.ConvertToImage("http://obd.oregonlookups.com/");
            }
            catch { }
        }


        private Result<string> Validate()
        {
            if (String.IsNullOrEmpty(provider.LicenseNumber) || Regex.IsMatch(provider.LicenseNumber, "Pending", RegexOptions.IgnoreCase))
            {
                return Result<string>.Failure(ErrorMsg.InvalidLicense);
            }
            else
            {
                return Result<string>.Success(String.Empty);
            }
        }
    }
}
