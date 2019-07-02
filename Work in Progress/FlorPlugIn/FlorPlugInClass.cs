using PlugIn4_5;
using RestSharp;

using System;
using System.Data;
using System.Net;
using System.Text.RegularExpressions;

namespace FlorPlugIn
{
    /// <summary>
    /// EchoPSV Plugin for New Hamshire Board of Medicine
    /// https://appsmqa.doh.state.fl.us/MQASearchServices/HealthCareProviders
    /// </summary>
    public class PlugInClass : IPlugIn
    {
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
                RestClient client = new RestClient(webParse.PrinterFriendlyUrl);
                RestRequest request = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(request);

                response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    pdf.Html = response.Content;
                    pdf.ConvertToABCImage(new ImageParameters { BaseUrl = "https://appsmqa.doh.state.fl.us/" });
                }
            }
            catch { }
        }

        private Result<string> Validate()
        {
            return (String.IsNullOrEmpty(provider.LicenseNumber) || Regex.IsMatch(provider.LicenseNumber, "Pending", RegexOptions.IgnoreCase))
                ? Result<string>.Failure(ErrorMsg.InvalidLicense)
                : Result<string>.Success(String.Empty);
        }
    }
}
