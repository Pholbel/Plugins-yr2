using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VTCVPlugIn
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
            Match exp = Regex.Match(response, "Expiration Date</span><[-='\\w ]+><span[-='\\w ]+>(?<date>[,\\w ]+)", RegOpt);

            //Set the expiration date
            try
            {
                Expiration = Convert.ToDateTime(exp.Groups["date"].Value).ToShortDateString();
            } catch (FormatException e)
            {
                Expiration = "01/01/1970";
            }

            //Disciplinary action
            Match disc = Regex.Match(response, "No cases", RegOpt);

            //We check for the absence of disciplinary/corrective action
            if (disc.Success)
                Sanction = SanctionType.None;
            else
                Sanction = SanctionType.Red;
        }

        private Result<string> ParseResponse(string response)
        {
            //Headers
            MatchCollection data = Regex.Matches(response, "<span[-'=\\w ]+class='field-caption[-\\w ]+'\\s*>(?<header>[\\w ]+)</span><div class='field-item[\\w ]+'>[-='<\\w ]*?>?(?<value>[-,\\.\\w ]*)(</span>)?</div>", RegOpt);
            MatchCollection cases = Regex.Matches(response, "<td\\s*title[-:;='\\w ]+><div\\s*class[-:;='\\w ]+><span\\s*data[-:;='\\w ]+>(?<case>[-,\\w ]+)</span></div></td>", RegOpt);
            string[] sanctionHeaders = { "Case Number", "Date Opened", "Date Closed", "Status" };

            if (data.Count > 0)
            {
                //Details
                StringBuilder builder = new StringBuilder();

                builder.AppendFormat(TdPair, "Personal Information", "");
                builder.AppendLine();

                for (int i = 0; i < data.Count && i < 3; i++)
                {
                    builder.AppendFormat(TdPair, data[i].Groups["header"].Value, data[i].Groups["value"].Value);
                    builder.AppendLine();
                }

                builder.AppendFormat(TdPair, "Address Details", "");
                builder.AppendLine();

                for (int i = 3; i < data.Count && i < 15; i++)
                {
                    builder.AppendFormat(TdPair, data[i].Groups["header"].Value, data[i].Groups["value"].Value);
                    builder.AppendLine();
                }

                builder.AppendFormat(TdPair, "License Information", "");
                builder.AppendLine();

                for (int i = 15; i < data.Count && i < 22; i++)
                {
                    builder.AppendFormat(TdPair, data[i].Groups["header"].Value, data[i].Groups["value"].Value);
                    builder.AppendLine();
                }

                builder.AppendFormat(TdPair, "Other Information", "");
                builder.AppendLine();

                for (int i = 22; i < data.Count; i++)
                {
                    builder.AppendFormat(TdPair, data[i].Groups["header"].Value, data[i].Groups["value"].Value);
                    builder.AppendLine();
                }

                if (cases.Count == 0)
                    builder.AppendFormat(TdPair, "Cases", "None");
                else
                    builder.AppendFormat(TdPair, "Cases", "");

                builder.AppendLine();

                for (int i = 0; i < cases.Count; i++)
                {
                    builder.AppendFormat(TdPair, sanctionHeaders[i % sanctionHeaders.Length], cases[i].Groups["case"].Value);
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
