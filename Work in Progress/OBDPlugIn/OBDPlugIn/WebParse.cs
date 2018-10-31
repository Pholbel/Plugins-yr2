using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OBDPlugIn
{
    public class WebParse
    {
        public string Expiration { get; private set; }
        public SanctionType Sanction { get; private set; }

        private string TdPair = "<tr><td>{0}</td><td>{1}</td></tr>";
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
                if (Regex.Matches(response.Content, "<a href='detail.asp.num=[0-9]*?.searchby.*?/a>", RegOpt).Count > 2)
                {
                    return Result<string>.Failure(ErrorMsg.MultipleProvidersFound);
                }
                else if (Regex.Match(response.Content, "No Records Found", RegOpt).Success)
                {
                    return Result<string>.Failure(ErrorMsg.NoResultsFound);
                }
                else // Returned successful query
                {
                    CheckLicenseDetails(response.Content);
                    return ParseResponse(response.Content);
                }
            }
            catch(Exception e)
            {
                return Result<string>.Exception(e);
            }
        }

        private void CheckLicenseDetails(string response)
        {
            Match exp1 = Regex.Match(response, "License Expire(s|d)?.?</b></td><td.*?>(?<EXP>.*?)(<tr|</td)", RegOpt);
            Match exp2 = Regex.Match(response, "License Lapsed.?</b></td><td.*?>(?<EXP>.*?)(<tr|</td)", RegOpt);
            Match exp3 = Regex.Match(response, "Expiration Date.?</b></td><td.*?>(?<EXP>.*?)(<tr|</td)", RegOpt);

            if(exp1.Success)
            {
                Expiration = exp1.Groups["EXP"].ToString();
            }
            else if (exp2.Success)
            {
                Expiration = exp2.Groups["EXP"].ToString();
            }
            else if (exp3.Success)
            {
                Expiration = exp3.Groups["EXP"].ToString();
            }

            Match status1 = Regex.Match(response, "<b>Board Action.</b></td><td.*?>(?<ACTION>.*?)(<tr|</td)", RegOpt);
            Match status2 = Regex.Match(response, "<b>Has the Board taken action.*?</b></td><td.*?>(?<ACTION>.*?)(<tr|</td)");
            Match status3 = Regex.Match(response, "<b>(Publicly )?Disciplined.*?</b></td><td.*?>(?<ACTION>.*?)(<tr|</td)");

            if (status1.Success)
            {
                Sanction = Regex.Match(status1.Groups["ACTION"].ToString(), "There has been no.", RegOpt).Success ? SanctionType.None : SanctionType.Red;
            }
            else if (status2.Success)
            {
                Sanction = Regex.Match(status2.Groups["ACTION"].ToString(), "nochecked.gif", RegOpt).Success ? SanctionType.None : SanctionType.Red;
            }
            else if (status3.Success)
            {
                Sanction = Regex.Match(status3.Groups["ACTION"].ToString(), "nochecked.gif", RegOpt).Success ? SanctionType.None : SanctionType.Red;
            }

            if (Sanction == SanctionType.None) // Check again in Malpractice actions - (if section exists)
            {
                Match action = Regex.Match(response, "<b>Malpractice Action:</b></td><td.*?>(?<ACTION>.*?)(<tr|</td)", RegOpt);
                if (action.Success)
                {
                    Sanction = Regex.Match(action.Groups["ACTION"].ToString(), "There has been no reported malpractice on this license", RegOpt).Success ? SanctionType.None : SanctionType.Red;
                }
            }
        }

        private Result<string> ParseResponse(string response) 
        {
            Match table = Regex.Match(response, "<table (align=.?center.? ?)?class=.?bodytext.?.*?/table>", RegOpt);
            string takenAction = "Has the Board taken action against license";
            string disciplined = "Disciplined";
 
            if (table.Success)
            {
                string result = table.Value;
                StringBuilder builder = new StringBuilder(string.Empty);
                MatchCollection matches = Regex.Matches(result, "<b>(?<HEADER>.*?)</b></td><td>(?<TEXT>.*?)(<tr|</td)", RegOpt);

                foreach (Match m in matches)
                {
                    if (Regex.Match(m.Value, takenAction, RegexOptions.IgnoreCase).Success
                        || Regex.Match(m.Value, disciplined, RegexOptions.IgnoreCase).Success)
                    {
                        continue;
                    }
                    string header = RemoveHTML(m.Groups["HEADER"].ToString());
                    string text = RemoveHTML(m.Groups["TEXT"].ToString());

                    Regex.Replace(header, "License Lapsed", "License Expired", RegexOptions.IgnoreCase);

                    builder.AppendFormat(TdPair, String.IsNullOrEmpty(header) ? text : header, String.IsNullOrEmpty(header) ? "" : text);
                }

                /// Reformat any items that contain images instead of texts or was not added in the above

                Match action = Regex.Match(response, "Has the board taken action.*?<td>(?<YESNOIMAGES>.*?)</td>", RegOpt);
                if (action.Success)
                {
                    if (Regex.Match(action.Groups["YESNOIMAGES"].ToString(), "nonotchecked.gif", RegOpt).Success)
                    {
                        builder.AppendFormat(TdPair, takenAction, "Yes");
                    }
                    else
                    {
                        builder.AppendFormat(TdPair, takenAction, "No");
                    }
                }

                Match discipline = Regex.Match(response, "(Publicly )?Disciplined.*?<td>(?<YESNOIMAGES>.*?)</td>", RegOpt);
                if (discipline.Success)
                {
                    if (Regex.Match(discipline.Groups["YESNOIMAGES"].ToString(), "nonotchecked.gif", RegOpt).Success)
                    {
                        builder.AppendFormat(TdPair, disciplined, "Yes");
                    }
                    else
                    {
                        builder.AppendFormat(TdPair, disciplined, "No");
                    }
                }

                Match lastUp = Regex.Match(response, "This information was last updated (?<UPDATED>.*?)<BR>", RegOpt);
                if (lastUp.Success)
                {
                    builder.AppendFormat(TdPair, "This information was last updated ", lastUp.Groups["UPDATED"]);
                }

                return Result<string>.Success(builder.ToString());
            }
            else // Error parsing table
            {
                return Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage); 
            }
        }

        private string RemoveHTML(string input)
        {
            input = Regex.Replace(input, "<br ?/>", " ", RegOpt);
            input = Regex.Replace(input, "<BR>", " ", RegOpt);
            input = Regex.Replace(input, "<b>", "", RegOpt);
            input = Regex.Replace(input, "</b>", "", RegOpt);
            input = Regex.Replace(input, "<img.*?>", "", RegOpt);
            input = Regex.Replace(input, "<p>", "", RegOpt);
            return input;
        }



    }
}
