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
            Match exp = Regex.Match(response, "<td>\\w*\\.\\d+</td>(<td>(<font color=\\w+>)?[&;\\w ]+(</font>)?</td>){3}(<td>((\\d+/\\d+/\\d+)|(&nbsp;))</td>){2}<td>(?<date>\\d+/\\d+/\\d+)</td>", RegOpt);

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
            MatchCollection values = Regex.Matches(response, "<td>(<font color=\\w+>)?((?<value>[-&,/\\.\\w ]+)|(&nbsp;)?(<br>)*)+(</font>)?</td>", RegOpt);
            Match sanctionHeaders = Regex.Match(response, "Board/Program Actions[-;:/\"=<>/\\w\\s]+?(<th scope=\"col\">(?<header>[\\w ]+)</th>)+", RegOpt);
            Match sanctionValues = Regex.Match(response, "Board/Program Actions[-;:/\"=<>/\\w\\s]+?((<td>(?<value>[-&,/\\.\\w]*)(&nbsp;)?</td>)+[=\"<>/\\s\\w]*?(<td>(?<value>[-&,/\\.\\w ]*)(&nbsp;)?</td>)*)+\\s*</tr>\\s*</tbody>", RegOpt);

            if (values.Count > 0)
            {
                //Details
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < headers.Count; i++)
                {
                    //We handle sanctions in a separate loop
                    if (headers[i].Groups["header"].Value == "Case Number")
                        break;

                    string value = "";

                    for (int j = 0; j < values[i].Groups["value"].Captures.Count; j++)
                        value += values[i].Groups["value"].Captures[j].Value + " ";

                    builder.AppendFormat(TdPair, headers[i].Groups["header"].Value, value);
                    builder.AppendLine();
                }

                int headerCount = sanctionHeaders.Groups["header"].Captures.Count;

                for (int i = 0; i < sanctionValues.Groups["value"].Captures.Count; i++)
                {
                    builder.AppendFormat(TdPair, sanctionHeaders.Groups["header"].Captures[i % headerCount], sanctionValues.Groups["value"].Captures[i]);
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
