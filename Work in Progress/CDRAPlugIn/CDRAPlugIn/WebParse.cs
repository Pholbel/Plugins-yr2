using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CDRAPlugIn
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
            //Ensure we get the expiration date of the license
            Match exp = Regex.Match(response, "<td>\\w*\\.\\d+</td>(<td>(<font color=green>)?[\\w ]+(</font>)?</td>){3}(<td>\\d+/\\d+/\\d+</td>){2}<td>(?<date>\\d+/\\d+/\\d+)</td>", RegOpt);

            //Set the expiration date to the expiration date of the latest one
            if (exp.Success)
                Expiration = exp.Groups["date"].Value;

            //Disciplinary action
            Match disc = Regex.Match(response, "There is no Discipline or Board Actions on file for this credential", RegOpt);

            //We check for the absence of disciplinary/corrective action
            if (disc.Success)
                Sanction = SanctionType.None;
            else
                Sanction = SanctionType.Red;
        }

        private Result<string> ParseResponse(string response)
        {
            //Headers
            MatchCollection headers = Regex.Matches(response, "<th scope=\"col\">(?<header>[\\w ]+)</th>", RegOpt);
            MatchCollection values = Regex.Matches(response, "<td>(<font color=green>)?(?<value>[,/\\.\\w ]*)(&nbsp;)?(</font>)?</td>", RegOpt);

            if (values.Count > 0)
            {
                //Details
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < headers.Count; i++)
                {
                    builder.AppendFormat(TdPair, headers[i].Groups["header"].Value, values[i].Groups["value"].Value);
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
