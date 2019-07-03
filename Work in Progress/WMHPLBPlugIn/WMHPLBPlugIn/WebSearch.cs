using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public class WebSearch
    {
        // google sheets api
        static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        static string ApplicationName = "WMHPLBPlugIn";

        private RegexOptions RegOpt = RegexOptions.IgnoreCase | RegexOptions.Singleline;
        private Provider provider { get; set; }

        public WebSearch(Provider _provider)
        {
            this.provider = _provider;
        }

        public Result<ValueRange> Execute()
        {
            try
            {
                return Search();
            }
            catch (Exception ex)
            {
                return Result<ValueRange>.Exception(ex);
            }
        }

        private Result<ValueRange> Search()
        {

            /*----------------------------------------------------------------------------------------------------*/
            /*------------------------------------------ GOOGLE SHEETS -------------------------------------------*/
            /*----------------------------------------------------------------------------------------------------*/

            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define request parameters.
            String spreadsheetId = "1HdBP5KsCmmy1VuYTU8LZZI7RKUZ1aW4sFRjsILPYWXE";
            String range = "Sheet1!A5:H";
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);


            // GET all values from spreadsheet
            ValueRange response = request.Execute();

            return Result<ValueRange>.Success(response);

        }


            /*----------------------------------------------------------------------------------------------------*/
            /*------------------------------------------ GOOGLE SHEETS -------------------------------------------*/
            /*----------------------------------------------------------------------------------------------------*/
   

        }
}
