using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MBBHTPlugIn
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
            //Get license dates
            MatchCollection exp = Regex.Matches(response, "<td class=\"td\">\\d+/\\d+/\\d+</td><td class=\"td\">(?<expiration>\\d+/\\d+/\\d+)</td>", RegOpt);

            //Set the expiration date to the expiration date of the latest one
            if (exp.Count > 0)
                Expiration = exp[exp.Count - 1].Groups["expiration"].Value;

            //Site does not support sanctions
        }

        private Result<string> ParseResponse(string response)
        {
            //Headers and values
            MatchCollection headers = Regex.Matches(response, "<td><h3>(?<header>[\\w]+):</h3></td>", RegOpt);
            MatchCollection values = Regex.Matches(response, "<span id=\"ContentPlaceHolder1_\\w+\">(?<value>[\\w\\s]+)</span>", RegOpt);
            Match employer = Regex.Match(response, "<tr class=\"employer\">\\s*<td>(?<employer>[\\w\\s]+)</td>\\s*</tr>", RegOpt);

            //License details
            MatchCollection licenseHeaders = Regex.Matches(response, "<td class=\"th\">(?<header>[\\w\\s]+)</td>", RegOpt);
            MatchCollection licenseValues = Regex.Matches(response, "<td class=\"td\">(?<value>([-/\\w]+\\s*)+)</td>", RegOpt);

            if (headers.Count > 0)
            {
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < headers.Count; i++)
                {
                    builder.AppendFormat(TdPair, headers[i].Groups["header"].Value, (i < values.Count) ? values[i].Groups["value"].Value : employer.Groups["employer"].Value);
                    builder.AppendLine();
                }

                for (int i = 0; i < licenseValues.Count; i++)
                {
                    builder.AppendFormat(TdPair, licenseHeaders[i % licenseHeaders.Count].Groups["header"].Value, licenseValues[i].Groups["value"].Value);
                    builder.AppendLine();
                }

                return Result<string>.Success(builder.ToString());
            }
            else // Error parsing table
            {
                return Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage);
            }
        }
    }
}
