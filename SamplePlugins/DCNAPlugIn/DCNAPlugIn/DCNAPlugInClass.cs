using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DCNAPlugIn
{
    /// <summary>
    /// EchoPSV Plugin for Alabama Board of Medical Examiners
    /// https://www.asisvcs.com/services/registry/search_generic.asp?CPCat=0709NURSE
    /// </summary>
    public class PlugInClass : IPlugIn
    {
        public override string Fetch(DataRow dr)
        {
            Initialize(dr, true, new SPMConfig() { AcceptAllCertificatePolicy = true, Expect100Continue = false });
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            string message = String.Empty;
            string output = String.Empty;

            Result<string> fields = Validate();

            if (fields.IsValid)
            {
                WebSearch webSearch = new WebSearch(provider, dr);
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
                pdf.Html = Regex.Replace(html, "src=\"../images/albop.png\"", "src=\"https://abme.igovsolution.com/online//images/albop.png\"", RegexOptions.IgnoreCase);
                pdf.ConvertToABCImage(new ImageParameters()
                {
                    //BaseUrl = "http://www.MNAR.org",
                    FixUrls = false,
                });
            }
            catch { }
        }

        private Result<string> Validate()
        {
            if (String.IsNullOrEmpty(provider.LicenseNumber))
            {
                return Result<string>.Failure(ErrorMsg.InvalidLicense);
            }
            else
            {
                return Result<string>.Success(String.Empty);
            }
            //return Result<string>.Success(String.Empty);
        }

    }

}
