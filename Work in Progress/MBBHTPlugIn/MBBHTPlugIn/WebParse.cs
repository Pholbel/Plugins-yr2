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
            //If they have multiple licenses, we return the expiration date of the license searched for
            Match exp = Regex.Match(response, "<b>Expiration\\s+Date:(</b></span>){3}</td>\\s+<td width=\"\\d+\"><span id=\"[_\\w]+\" class=\"normal\"( style=\"[;:\\w]+\")?>(?<date>\\d+-\\d+-\\d+)</span>", RegOpt);

            //Set the expiration date to the expiration date of the latest one
            if (exp.Success)
                Expiration = exp.Groups["date"].Value;

            //Site has sections for disciplinary action and corrective action
            Match disc = Regex.Match(response, "Disciplinary Action:[:;_\"=/<>\\w\\s]+No</span></td>\\s+<td vAlign", RegOpt);
            Match corr = Regex.Match(response, "Corrective\\s+Action:[:;_\"=/<>\\w\\s]+No</span></td>\\s+</tr>", RegOpt);

            //We check for the absence of disciplinary/corrective action
            if (disc.Success && corr.Success)
                Sanction = SanctionType.None;
            else
                Sanction = SanctionType.Red;
        }

        private Result<string> ParseResponse(string response)
        {
            //Headers and values
            MatchCollection data = Regex.Matches(response, "<span class=\"Normal\"><b>(?<header>[()\\s\\w,:]+)(</b>\\s*</span>)+</td>\\s*<td( vAlign=\"top\")?( width=\"\\d+\")?><span id=\"[_\\w]+\" class=\"normal\"( style=\"[;:\\w]+\")?>(?<value>[-,()\\w\\s]*)</span>", RegOpt);

            if (data.Count > 0)
            {
                //Multiple licenses
                string licNo = data[3].Groups["value"].Value;
                Match licHeaders = Regex.Match(response, "(<td>(?<header>[\\w\\s]+)</td>){5}", RegOpt);
                MatchCollection licDetails = Regex.Matches(response, "(<td (style=\"[:;\\w]+\")?(width=\"\\d+\")?>(?<value>[-()\\s\\w]+)</td>){5}", RegOpt);

                //Details
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < data.Count; i++)
                {
                    builder.AppendFormat(TdPair, data[i].Groups["header"].Value, data[i].Groups["value"].Value);
                    builder.AppendLine();
                }

                //Multiple licenses
                for (int i = 0; i < licDetails.Count; i++)
                {
                    if (licDetails[i].Groups["value"].Captures[1].Value == licNo)
                        continue;
                    for (int j = 0; j < 5; j++)
                    {
                        builder.AppendFormat(TdPair, licHeaders.Groups["header"].Captures[j].Value, licDetails[i].Groups["value"].Captures[j].Value);
                        builder.AppendLine();
                    }
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
