using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WICVPlugIn
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

        public Result<string> Execute(IRestResponse response)
        {
            try
            {
                CheckLicenseDetails(response.Content);
                return ParseResponse(response.Content);
            }
            catch (Exception e)
            {
                return Result<string>.Exception(e);
            }
        }

        private void CheckLicenseDetails(string response)
        {
            Match ACT = Regex.Match(response, "Status:</strong>(?<ACT>.*)<br");
            if (ACT.Success)
            {
                string activity = ACT.Groups["ACT"].ToString();
                if (!activity.Contains("Active"))
                {
                    Sanction = SanctionType.Red;
                }
            }
        }

        private Result<string> ParseResponse(string response)
        {
            MatchCollection headers = Regex.Matches(response, "(?<=(<strong>)).*(?=:)");
            MatchCollection fields = Regex.Matches(response, "(?<=(</strong>)).*(?=<br)");
            Match otherNames = Regex.Match(response, "(?<=(Other Names:</strong>)).*(?= </p>)", RegOpt);

            if (headers.Count == 13)
            {
                StringBuilder builder = new StringBuilder();

                // Headers and Fields
                for (int idx = 0; idx < headers.Count-1; idx++)
                {
                    string header = headers[idx].ToString();
                    string text = fields[idx].ToString();

                    builder.AppendFormat(TdPair, header, text);
                    builder.AppendLine();
                }

                // Get Other Names
                string[] names = CleanNamesString(otherNames.ToString()).Split(',');
                if (names.Length == 1)
                {
                    builder.AppendFormat(TdPair, headers[12], names[0]);
                    builder.AppendLine();
                }
                else
                {
                    string outputNames = "";
                    for (int idx = 0; idx < names.Length - 1; idx++) { outputNames += (names[idx] + ','); }
                    builder.AppendFormat(TdPair, headers[12], outputNames.Substring(0, outputNames.Length - 1));
                    builder.AppendLine();
                }
                

                return Result<string>.Success(builder.ToString());
            }
            else // Error parsing table
            {
                return Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage);
            }
        }

        private string CleanNamesString(string str)
        {
            str = str.Replace("\n", "");
            str = str.Replace("\t", "");
            str = str.Replace("\r", "");
            str = str.Replace(" <br />", ",");

            return str;
        }
    }
}
