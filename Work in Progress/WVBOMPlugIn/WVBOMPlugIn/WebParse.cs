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
            //Get license date
            Match exp = Regex.Match(response, "<td>\\s+\\d+/\\d+/\\d+\\s+</td>\\s+<td>\\s+(?<date>\\d+/\\d+/\\d+)\\s+</td>", RegOpt);

            if (exp.Success)
                Expiration = exp.Groups["date"].Value;

            //Get sanction status
            Match sanction = Regex.Match(response, "<td>\\s+None\\s+</td>", RegOpt);

            //The regex specifically looks for no sanctions
            if (sanction.Success)
                Sanction = SanctionType.None;
            else
                Sanction = SanctionType.Red;
        }

        //This site's way of displaying data is very inconsistent
        private Result<string> ParseResponse(string response)
        {
            //Headers
            MatchCollection headers = Regex.Matches(response, "<th>(?<header>[\\w\\s]+)(<br>[\\s]+)?(?<extension>[-\\w\\s\\(\\)]*)</th>", RegOpt);
            MatchCollection values = Regex.Matches(response, "<td>((?<value>[-\\w\\s\\./,#]+)(&nbsp;|<br />\\s*)*)*</td>", RegOpt);

            Match skipAction = Regex.Match(response, "<td colspan=\\\"2\\\">&nbsp;</td>", RegOpt);
            Match skipHistory = Regex.Match(response, "<thead>\\s+<tr>\\s+<th>License History</th>\\s+<th>Date of Action</th>\\s+</tr>\\s+</thead>\\s+<tbody>\\s+</tbody>", RegOpt);

            if (headers.Count > 0)
            {
                StringBuilder builder = new StringBuilder();

                int skip = 0;
                bool pac = false;

                for (int i = 0; i < headers.Count; i++)
                {
                    string header = string.Format("{0} {1}", headers[i].Groups["header"].Value, headers[i].Groups["extension"].Value).Trim();
                    Match mValue = values[i - skip];
                    string value = "";

                    foreach (Capture c in values[i - skip].Groups["value"].Captures)
                    {
                        value += c.Value;
                        value += " ";
                    }

                    value = value.Trim();

                    if (values.Count > headers.Count && value == "")
                    {
                        skip--;
                        i--;
                        continue;
                    }

                    if (value == "PA-C")
                        pac = true;
                    else if (pac && value == "")
                    {
                        skip -= 2;
                        pac = false;
                    }

                    if ((header == "Action Taken" || header == "Date Action Taken") && skipAction.Success)
                    {
                        builder.AppendFormat(TdPair, header, "");
                        skip++;
                    } else if ((header == "License History" || header == "Date of Action") && skipHistory.Success)
                    {
                        builder.AppendFormat(TdPair, header, "");
                        skip++;
                    } else
                    {
                        builder.AppendFormat(TdPair, header, value);
                    }

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
