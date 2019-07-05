using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace WMHPLBPlugIn
{
    /// <summary>
    /// EchoPSV Plugin for Wyoming
    /// https://docs.google.com/spreadsheets/d/1HdBP5KsCmmy1VuYTU8LZZI7RKUZ1aW4sFRjsILPYWXE/edit#gid=0
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
                WebSearch webSearch = new WebSearch(provider);
                Result<List<String>> response = webSearch.Execute();

                if (response.IsValid)
                {
                    WebParse webParse = new WebParse();
                    Result<string> parseResult = webParse.Execute(response.Value);

                    if (parseResult.IsValid)
                    {
                        HandleModes(webParse);

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

        private void HandleModes(WebParse webParse)
        {
            HandleExpirables(provider.ExpirationDate, webParse.Expiration);
            SetSanction(webParse.Sanction);

            //try
            //{
            //    pdf.Html = html;
            //    pdf.ConvertToImage("http://business.nh.gov/medicineboard/Search.aspx");
            //}
            //catch { }
        }

        private Result<string> Validate()
        {

            Boolean null_license = String.IsNullOrEmpty(provider.LicenseNumber);
            Boolean pending_license = Regex.IsMatch(provider.LicenseNumber, "Pending", RegexOptions.IgnoreCase);
            Boolean null_fname = String.IsNullOrEmpty(provider.FirstName);
            Boolean null_lname = String.IsNullOrEmpty(provider.LastName);

            if ( (null_license || pending_license) && (null_fname || null_lname) )
            {
                return Result<string>.Failure(ErrorMsg.InvalidLicenseAndFirstLastName);
            }
            else
            {
                return Result<string>.Success(String.Empty);
            }
        }

    }
}
