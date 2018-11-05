using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IDBPPlugIn
{
    /// <summary>
    /// EchoPSV Plugin for New Hamshire Board of Medicine
    /// https://idbop.mylicense.com/verification/Search.aspx
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
                Result<IRestResponse> response = webSearch.Execute();

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

        private void HandleModes(string html, WebParse webParse)
        {
            HandleExpirables(provider.ExpirationDate, webParse.Expiration);
            SetSanction(webParse.Sanction);

            try
            {
                pdf.Html = html;
                pdf.ConvertToImage("http://business.nh.gov/medicineboard/Search.aspx");
            }
            catch { }
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
