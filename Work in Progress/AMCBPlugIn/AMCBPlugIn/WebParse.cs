using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AMCBPlugIn
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
            Match exp = Regex.Match(response, "td\\s+headers=\"CURRENT_EXP_DATE_\\d+\">(?<date>[#&;\\w]+)</td>", RegOpt);

            //Set the expiration date
            try
            {
                Expiration = Convert.ToDateTime(CleanDate(exp.Groups["date"].Value)).ToShortDateString();
            } catch (FormatException e)
            {
                Expiration = "01/01/1492";
            }

            //Disciplinary action
            Match disc = Regex.Match(response, "<td\\s+headers=\"C\\d+_\\d+\">N</td>", RegOpt);

            //We check for the absence of disciplinary/corrective action
            if (disc.Success)
                Sanction = SanctionType.None;
            else
                Sanction = SanctionType.Red;
        }

        private Result<string> ParseResponse(string response)
        {
            //Headers
            MatchCollection headers = Regex.Matches(response, "<th\\s+align=\"left\"\\s+id=\"[_\\w]+\"\\s*>(?<header>[\\w ]+)</th>", RegOpt);
            MatchCollection values = Regex.Matches(response, "<td\\s+headers=\"[\\w ]+\">(?<value>[#&;\\w ]+)</td>", RegOpt);

            if (values.Count > 0)
            {
                //Details
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < headers.Count; i+=1)
                {
                    builder.AppendFormat(TdPair, headers[i].Groups["header"].Value, CleanDate(values[i].Groups["value"].Value));
                    builder.AppendLine();
                }

                return Result<string>.Success(builder.ToString());
            }
            else // Error parsing table
            {
                return Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage);
            }
        }

        private string CleanDate(string date)
        {
            return Regex.Replace(date, "&#x2F;", "/", RegOpt);
        }
    }
}
