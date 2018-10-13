using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WVBOMPlugIn
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
            //Some providers have multiple licenses
            MatchCollection exp = Regex.Matches(response, "(Until|Date|Expire[ds]):</b></td><td>(?<date>\\d+/\\d+/\\d+)", RegOpt);

            //We make the assumption that the license higher up in the list is the more recent one
            //The website appears to follow this trend
            if (exp.Count > 0)
                Expiration = exp[0].Groups["date"].Value;

            //Get sanction status
            Match sanction = Regex.Match(response, "Board Action\\?</b></td><td>Yes", RegOpt);

            //The regex specifically looks for sanctions
            if (sanction.Success)
                Sanction = SanctionType.Red;
            else
                Sanction = SanctionType.None;
        }

        private Result<string> ParseResponse(string response)
        {
            //Headers for all the relevant details
            MatchCollection data = Regex.Matches(response, "<td><b>(?<header>[\\w\\s#\\?]+):*</b></td><td>(?<value>[\\w,\\s/.-]+)<", RegOpt);

            if (data.Count > 0)
            {
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < data.Count; i++)
                {
                    builder.AppendFormat(TdPair, data[i].Groups["header"].Value, data[i].Groups["value"].Value);
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
