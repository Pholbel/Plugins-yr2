using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IBPLPlugIn
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
            MatchCollection exp = Regex.Matches(response, "<td align=\"center\">(\\d*/\\d*/\\d*)</td>", RegOpt);

            //There are two dates in the form data: one for date issued and one for expiration,
            //but they do not have id tags to differentiate them
            if (exp.Count == 2)
                Expiration = exp[1].Groups[1].ToString();

            //Get sanction status
            Match sanction = Regex.Match(response, "<td style=\"padding-left:5px;\">Disciplinary Action</td>\n*\\s*<td style=\"padding-left:5px;\">\n*\\s*(N)", RegOpt);

            //The regex specifically looks for no sanctions
            if (sanction.Success)
                Sanction = SanctionType.None;
            else
                Sanction = SanctionType.Red;
        }

        private Result<string> ParseResponse(string response)
        {
            //Headers for all the relevant details
            MatchCollection headers = Regex.Matches(response, "<th>([\\w\\s]*)</th>", RegOpt);

            //Values of the fields
            MatchCollection fields = Regex.Matches(response, "<td align=\"center\">([\\d\\w\\s\\./]*)</td>", RegOpt);

            if (fields.Count > 0)
            {
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < fields.Count; i++)
                {
                    builder.AppendFormat(TdPair, headers[i].Groups[1].ToString(), fields[i].Groups[1].ToString());
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
