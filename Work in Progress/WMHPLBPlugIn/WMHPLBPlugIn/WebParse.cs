using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
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
    public class WebParse
    {
        public string Expiration { get; private set; }
        public SanctionType Sanction { get; private set; }

        private string TdPair = "<tr><td>{0}</td><td>{1}</td></tr>";
        private string TdSingle = "<td>{0}</td>";
        private RegexOptions RegOpt = RegexOptions.IgnoreCase | RegexOptions.Singleline;

        public WebParse()
        {
            Expiration = String.Empty;
            Sanction = SanctionType.None;
        }

        public Result<string> Execute(List<String> response)
        {
            try
            {
                return ParseResponse(response);
            }
            catch (Exception e)
            {
                return Result<string>.Exception(e);
            }
        }

        private Result<string> ParseResponse(List<String> response)
        {
            List<String> headers = new List<String>();
            headers.Add("LASTNAME");
            headers.Add("FIRSTNAME");
            headers.Add("INITIAL");
            headers.Add("LICENSE #");
            headers.Add("DATE ISSUED");
            headers.Add("EXPIRATION DATE");
            headers.Add("License/Certificate Status");
            headers.Add("Disciplined");

            try
            {

                // check discipline
                if (string.Equals(response[response.Count-1], "Yes", StringComparison.OrdinalIgnoreCase))
                {
                    Sanction = SanctionType.Red;
                } else
                {
                    Sanction = SanctionType.None;
                }

                StringBuilder builder = new StringBuilder();

                for (var i = 0; i < response.Count; i++)
                {
                    builder.AppendFormat(TdPair, headers[i], response[i]);
                    builder.AppendLine();
                }

                return Result<string>.Success(builder.ToString());
            }
            catch
            {
                return Result<string>.Failure(ErrorMsg.Custom("Error reading results"));
            }
        }
    }
}
