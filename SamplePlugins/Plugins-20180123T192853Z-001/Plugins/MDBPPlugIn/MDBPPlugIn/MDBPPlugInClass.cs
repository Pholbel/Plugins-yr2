using System;
using System.Data;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;


namespace MDBPPlugIn
{
    public class PlugInClass : IPlugIn
    {
        private const string BaseUrl = @"https://www.mbp.state.md.us/bpqapp/";

        private string basehref = String.Empty;
        private const string AnySpaces = "(|)*";

        public bool HasSacntion
        {
            get
            {
                return Regex.IsMatch(appletFlag, "R", RegexOptions.IgnoreCase);
            }
        }

        public override string Fetch(DataRow dr)
        {
            Initialize(dr, true);

            string message = String.Empty;
            string output = String.Empty;

            StringBuilder tester = new StringBuilder(String.Empty);

            if (String.IsNullOrEmpty(provider.LicenseNumber))
            {
                message = ErrorMsg.InvalidLicense;
            }
            else
            {
                Result<string> content = SendRequest();

                if (content.IsValid)
                {
                    HtmlDocument html = new HtmlDocument();
                    html.LoadHtml(content.Value);

                    StringBuilder builder = new StringBuilder(String.Empty);
                    builder.Append(Extract.TimeStamp(html));
                    builder.Append(Extract.Name(html));
                    builder.Append(Extract.LicenseAndEducation(html, this));
                    builder.Append(Extract.PrimaryPracticeSetting(html));
                    builder.Append(Extract.Address(html));
                    builder.Append(Extract.Graduated(html));
                    builder.Append(Extract.DisciplineAction(html, this));
                    builder.Append(Extract.PendingCharges(html, this));
                    builder.Append(Extract.MalpracticeJudgements(html, this));
                    builder.Append(Extract.Convictions(html, this));

                    pdf.Html = content.Value;
                    pdf.PreProcessTags();
                    pdf.ConvertToImage("http://www.mbp.state.md.us/");

                    output = builder.ToString();
                }
                else
                {
                    message = content.Error;
                }
            }

            output = Regex.Replace(output,"License Expiration","Expiration Date",RegexOptions.Singleline|RegexOptions.IgnoreCase);

            return ProcessResults(output, message);
        }

        private Result<string> GetResultLink(string content)
        {
            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(content);

            HtmlNode status = html.DocumentNode.SelectSingleNode("//input[@name='txtLicStatus']");

            if (status != null)
            {
                string value = (status.Attributes.Contains("value")) ? status.Attributes["value"].Value : String.Empty;
                string uri = String.Empty;

                if (Regex.IsMatch(provider.LicenseNumber.Substring(0, 1), "D|H", RegexOptions.IgnoreCase))
                {
                    if (Regex.IsMatch(value, "ACTIVE|PROBATION", RegexOptions.IgnoreCase))
                    {
                        uri = "PProfile.asp";
                    }
                    else if (Regex.IsMatch(value, "DECEASED", RegexOptions.IgnoreCase))
                    {
                        uri = "PProfileD.asp";
                    }
                    else
                    {
                        uri = "PProfile3.asp";
                    }
                }
                else
                {
                    if (Regex.IsMatch(value, "DECEASED", RegexOptions.IgnoreCase))
                    {
                        uri = "PProfile2D.asp";
                    }
                    else
                    {
                        uri = "PProfile2.asp";
                    }
                }

                return Result<string>.Success(uri);
            }
            else
            {
                HtmlNode bodyText = html.DocumentNode.SelectSingleNode("//td[@class='bodytext']");

                if (bodyText != null)
                {
                    return Result<string>.Failure((Regex.IsMatch(bodyText.InnerText.Trim(), "No Result found", RegexOptions.IgnoreCase)) ? ErrorMsg.NoResultsFound : ErrorMsg.CannotAccessSearchForm);
                }
                else
                {
                    return Result<string>.Failure(ErrorMsg.CannotAccessSite);
                }
            }
        }

        private Result<string> SendRequest()
        {
            RestClient client = new RestClient(BaseUrl);
            RestRequest request = new RestRequest("Search.asp", Method.POST);
            request.AddQueryParameter("txtAction", "SearchByLicNo");
            request.AddQueryParameter("txtLicNo", provider.LicenseNumber);
            request.AddQueryParameter("submit", "20");
            request.AddQueryParameter("submit", "12");
            request.AddQueryParameter("submit", "submit");

            IRestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Result<string> uri = GetResultLink(response.Content);

                if (uri.IsValid)
                {
                    request = new RestRequest(uri.Value, Method.POST);
                    request.AddQueryParameter("txtLicNo", provider.LicenseNumber);

                    response = client.Execute(request);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return Result<string>.Success(response.Content);
                    }
                    else
                    {
                        return Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage);
                    }
                }
                else
                {
                    return Result<string>.Failure(uri.Error);
                }
            }
            else
            {
                return Result<string>.Failure(ErrorMsg.SiteUnavailabe);
            }
        }

        public void SetExpirable(string date)
        {
            HandleExpirables(provider.ExpirationDate, date);
        }

        public void SetFlag(SanctionType type)
        {
            SetSanction(type);
        }

        private string rearrange_MDBP(string origHTML, string lic_exp_dt)
        {
            string oTxt = Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(this.replaceEndTags(this.replaceStartTags(this.replaceEndTags(this.replaceStartTags(this.replaceEndTags(this.replaceStartTags(this.replaceEndTags(this.replaceStartTags(this.replaceEndTags(this.replaceStartTags(origHTML, "table", "<table>"), "table", "</table>"), "tr", "<tr>"), "tr", "</tr>"), "td", "<td>"), "td", "</td>"), "b", ""), "b", ""), "span", ""), "span", ""), "Primary" + AnySpaces + "Practice" + AnySpaces + "Setting", "Primary Practice Setting", RegexOptions.IgnoreCase | RegexOptions.Singleline), ">" + AnySpaces + "Graduated", ">Graduated", RegexOptions.IgnoreCase | RegexOptions.Singleline), "Postgraduate" + AnySpaces + "Training" + AnySpaces + "Program", "Postgraduate Training Program", RegexOptions.IgnoreCase | RegexOptions.Singleline), "Specialty" + AnySpaces + "Board" + AnySpaces + "Certification", "Specialty Board Certification", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            string toReturn =  Regex.Replace(this.replaceEndTags(this.replaceStartTags(this.replaceEndTags(this.replaceStartTags(this.replaceEndTags(this.replaceStartTags(this.replaceEndTags(this.replaceStartTags(this.replaceStartTags(this.replaceEndTags(this.replaceStartTags(this.getResDate(oTxt, lic_exp_dt) + this.getResName(oTxt, lic_exp_dt) + this.getLicAndEduc(oTxt, lic_exp_dt) + this.getPrimaryPractSet(oTxt) + this.getPubAddr(oTxt) + this.getPostGradInfo(oTxt) + this.getSpecBrdCert(oTxt) + this.getSelfDesigPrac(oTxt) + this.getMDPrivInfo(oTxt) + this.getDiscAct(oTxt) + this.getDownloadAll(oTxt) + this.getMalPract(oTxt) + this.getConvict(oTxt), "span", ""), "span", ""), "br", ""), "a", ""), "a", ""), "b", ""), "b", ""), "em", ""), "em", ""), "strong", ""), "strong", ""), "&#149;", "-", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            origHTML = Regex.Replace(origHTML, "<form", "<img src=\"http://www.mbp.state.md.us/images/page/mbp_banner.gif\"><br><form");
            pdf.Html = origHTML;
            pdf.ConvertToImage("http://www.mbp.state.md.us/bpqapp/Search.asp");

            return toReturn;
        }

        private string getResName(string oTxt, string drLicExp)
        {
            string str = "";
            oTxt = this.replaceStartTags(oTxt, "tr", "");
            oTxt = this.replaceEndTags(oTxt, "tr", "");
            oTxt = Regex.Replace(oTxt, "&nbsp;", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            string pattern = "<td>\\s*This\\s+data\\s+was\\s+extracted\\s+on[^<]*</td>\\s*<td>\\s*(?<NAME>[^<]*?)\\s*</td>";
            Match match = Regex.Match(oTxt, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success)
                str = "<td>Name</td><td>" + match.Groups["NAME"].ToString().Trim() + "</td>\n";
            return str;
        }

        private string getResDate(string oTxt, string drLicExp)
        {
            string str = "";
            oTxt = this.replaceStartTags(oTxt, "tr", "");
            oTxt = this.replaceEndTags(oTxt, "tr", "");
            oTxt = Regex.Replace(oTxt, "&nbsp;", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            string pattern = "<td>\\s*(?<TXT>This\\s+data\\s+was\\s+extracted\\s+on\\s+?)(?<EXTDT>[^<]*?)\\s*</td>\\s*<td>\\s*(?<NAME>[^<]*?)\\s*</td>";
            Match match = Regex.Match(oTxt, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success)
                str = "<td>" + match.Groups["TXT"].ToString().Trim() + "</td><td>" + match.Groups["EXTDT"].ToString().Trim() + "</td>\n";
            return str;
        }

        private string getLicAndEduc(string oTxt, string drLicExp)
        {
            string pattern1 = "License and Education.*?</table>";
            Match match1 = Regex.Match(oTxt, pattern1, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            string str;

            if (match1.Success)
            {
                str = match1.ToString();
                string patrn_lic_exp = "License" + AnySpaces + "Expiration" + AnySpaces + "(:)*" + AnySpaces + "</td>" + AnySpaces + "<td>" + AnySpaces + "(?<EXPDT>.*?)" + AnySpaces + "</td>";
                if (this.expirable)
                    this.handleExpirable(str, drLicExp, patrn_lic_exp, "EXPDT");
            }
            else
                str = "<table><tr><td>Could not find</td><td>License and Education section</td></tr></table>";

            string pattern2 = "Graduated From:&nbsp;(?<SCHOOL>.*?)</td>";
            Match match2 = Regex.Match(oTxt, pattern2, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            string s = match2.Groups["SCHOOL"].ToString();
            if (match2.Success && !match2.Groups["SCHOOL"].ToString().Equals("&nbsp"))
                str = str + "<table><tr><td>Graduated From</td><td>" + match2.Groups["SCHOOL"].ToString().Trim() + "</td></tr></table>";

            return str;
        }

        private string getPrimaryPractSet(string oTxt)
        {
            //string pattern = ">" + this.anySpaces + "Primary Practice Setting.*?<table>" + this.anySpaces + "(?<PPSETDATA><tr>.*?)</table>";
            string pattern = "Primary Practice.*?</table>";
            Match match = Regex.Match(oTxt, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return !match.Success ? "<table><tr><td>Could not find</td><td>Primary Practice Setting section</td></tr></table>" : "<table><tr><td>Primary Practice Setting:</td><td> </td>" + Regex.Replace(match.ToString().Trim(), "<td>", "<td> - </td><td>", RegexOptions.IgnoreCase | RegexOptions.Singleline) + "</table>";
        }

        private string getPubAddr(string oTxt)
        {
            //string pattern = ">" + this.anySpaces + "Public" + this.anySpaces + "Address.*?<table>" + this.anySpaces + "(?<PUBADDR><tr>.*?)</table>";
            string pattern = "Public Address.*?</table>";
            Match match = Regex.Match(oTxt, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            string s = !match.Success ? "<table><tr><td>Could not find</td><td>Public Address section</td></tr></table>" : "<table><tr><td>Public Address:</td><td> </td>" + Regex.Replace(match.ToString().Trim(), "<td>", "<td> - </td><td>", RegexOptions.IgnoreCase | RegexOptions.Singleline) + "</table>";
            return s;
        }

        private string getPostGradInfo(string oTxt)
        {
            string str1 = "";
            //string pattern = "(?<POSTGTXT>Postgraduate Training.*?)</td>" + this.anySpaces + "<td>(?<CONCTXT>.*?)" + this.anySpaces + "</td>" + this.anySpaces + ".*?<table>" + this.anySpaces + "(?<POSTGDATA><tr>.*?</tr>)" + this.anySpaces + "</table>";
            string pattern = "(?<POSTGTXT>Postgraduate Training.*?)</td>.*?<td>(?<CONCTXT>.*?)</td>.*?</table>.*?<table>(?<POSTGDATA>.*?)</table>";
            Match match1 = Regex.Match(oTxt, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            string str2;
            if (match1.Success)
            {
                //string input = Regex.Replace(match1.Groups["POSTGDATA"].ToString().Trim(), "<td>" + this.anySpaces, "<td>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                //input = Regex.Replace(input, this.anySpaces + "</td>", "</td>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                string input = Regex.Replace(match1.Groups["POSTGDATA"].ToString().Trim(), "&#149;", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                input = Regex.Replace(input, "<td>\\s*</td>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                //string input = Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(match1.Groups["POSTGDATA"].ToString().Trim(), "<td>" + this.anySpaces, "<td>", RegexOptions.IgnoreCase | RegexOptions.Singleline), this.anySpaces + "</td>", "</td>", RegexOptions.IgnoreCase | RegexOptions.Singleline), "&#149;", "", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<td>\\s*</td>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                foreach (Match match2 in Regex.Matches(input, "<td>(?<POSTGVAL>.*?)</td>.*?<td>(?<CONCVAL>.*?)</td>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
                {
                    str1 = str1 + "<tr><td>" + match1.Groups["POSTGTXT"].ToString().Trim() + "</td><td>" + match2.Groups["POSTGVAL"].ToString().Trim() + "</td>";
                    str1 = str1 + "<td>" + match1.Groups["CONCTXT"].ToString().Trim() + "</td><td>" + match2.Groups["CONCVAL"].ToString().Trim() + "</td></tr>";
                }
                if (str1 != "")
                    input = str1;
                str2 = "<table>\n" + input + "\n</table>\n";
            }
            else
                str2 = "<table><tr><td>Could not find</td><td>Postgraduate Training Program section</td></tr></table>";
            return str2;
        }

        private string getDiscAct(string oTxt)
        {
            oTxt = this.replaceStartTags(oTxt, "!", "");
            //string pattern1 = "(?<DISPTXT>Known Disciplinary.*?)<.*?<table>" + this.anySpaces + "(?<DISCDATA><tr>.*?</tr>)" + this.anySpaces + "</table>" + this.anySpaces + "</td>";
            string pattern1 = "(?<DISPTXT>Known Disciplinary.*?)<.*?<table>.*?(?<DISCDATA><tr>.*?</tr>).*?</table>.*?</td>";
            Match match = Regex.Match(oTxt, pattern1, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            string str1;
            if (match.Success)
            {
                string input = match.Groups["DISCDATA"].ToString().Trim();
                string pattern2 = "Summary" + AnySpaces + "(:)*" + AnySpaces + "</td>" + AnySpaces + "<td>" + AnySpaces + "No actions reported";
                if (!Regex.Match(input, pattern2, RegexOptions.IgnoreCase | RegexOptions.Singleline).Success)
                    this.setSanction(true);
                else
                    this.setSanction(false);
                string str2 = Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(input, "<tr>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline), "</tr>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<table>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline), "</table>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<td>" + AnySpaces + "</td>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                str1 = "<table><tr><td>" + match.Groups["DISPTXT"].ToString().Trim() + "</td><td> </td></tr><tr>" + str2 + "</tr></table>";
            }
            else
                str1 = "<table><tr><td>Could not find</td><td>Known Disciplinary Action section</td></tr></table>";
            return str1;
        }

        private string getDownloadAll(string oTxt)
        {
            string str1 = "";
            string pattern1 = ">" + AnySpaces + "(?<DOWNLTXT>Download( |\t|\r|\v|\f|\n|(&nbsp;))*All.*?)</td>.*?(?<DOWNLDATA><tr>.*?</tr>)( |\t|\r|\v|\f|\n|(&nbsp;))*</table>";
            Match match1 = Regex.Match(oTxt, pattern1, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            string str2;
            if (match1.Success)
            {
                string input1 = Regex.Replace(match1.Groups["DOWNLDATA"].ToString().Trim(), AnySpaces + "&#149;" + AnySpaces, "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                string pattern2 = "<td>(?<COL1>.*?)</td>" + AnySpaces + "<td>(?<COL2>.*?)</td>";
                MatchCollection matchCollection = Regex.Matches(input1, pattern2, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                string pattern3 = "a" + AnySpaces + "href" + AnySpaces + "=" + AnySpaces + "(\"|'|)(?<OLINK>.*?)(\"|'|)>";
                foreach (Match match2 in matchCollection)
                {
                    string str3 = match2.Groups["COL2"].ToString().Trim();
                    Match match3 = Regex.Match(str3, pattern3, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    string input2 = "";
                    if (match3.Success)
                    {
                        input2 = match3.Groups["OLINK"].ToString().Trim();
                        if (!Regex.Match(input2, "://", RegexOptions.IgnoreCase | RegexOptions.Singleline).Success)
                        {
                            if (!input2.StartsWith("/"))
                                input2 = "/" + input2;
                            input2 = this.basehref + input2;
                        }
                    }
                    string str4 = Regex.Replace(this.replaceEndTags(this.replaceStartTags(str3, "a", ""), "a", ""), ":", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    str1 = str1 + "<tr><td>" + str4 + "</td><td>" + input2 + "</td></tr>";
                }
                if (str1 != "")
                    input1 = str1;
                string str5 = this.replaceEndTags(this.replaceStartTags(Regex.Replace(input1, "\\(using .*?\\)", "", RegexOptions.IgnoreCase | RegexOptions.Singleline), "a", ""), "a", "");
                str2 = "<table><tr><td>" + match1.Groups["DOWNLTXT"].ToString().Trim() + "</td><td> </td></tr>" + str5 + "</table>";
            }
            else
                str2 = "<table><tr><td>Could not find</td><td>Download All Disciplinary Actions section</td></tr></table>";
            return str2;
        }

        private string getSpecBrdCert(string oTxt)
        {
            string str = "";
            string pattern1 = "(?<SPECTXT>Specialty Board.*?)</td>.*?(?<SPECDATA><tr>.*?</tr>).*?</table>";
            Match match1 = Regex.Match(oTxt, pattern1, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            string html;
            if (match1.Success)
            {
                string input = Regex.Replace(match1.Groups["SPECDATA"].ToString().Trim(), AnySpaces + "&#149;" + AnySpaces, "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                string pattern2 = "<td>.*?(?<COL1>.*?)</td>.*?<td>.*?(?<COL2>.*?)</td>";
                foreach (Match match2 in Regex.Matches(input, pattern2, RegexOptions.IgnoreCase | RegexOptions.Singleline))
                {
                    str += "<tr><td>";
                    str = !(match2.Groups["COL1"].ToString().Trim() == "") ? str + match2.Groups["COL1"].ToString().Trim() : str + match2.Groups["COL2"].ToString().Trim();
                    str += "</td><td> </td></tr>";
                    //str = str + "</td><td>" + match2.Groups["COL2"].ToString().Trim() + "</td></tr>";
                }
                if (str != "")
                    input = str;
                html = "<table><tr><td>" + match1.Groups["SPECTXT"].ToString().Trim() + "</td><td> </td></tr><tr>" + input + "</tr></table>";
            }
            else
                html = "<table><tr><td>Could not find</td><td>Specialty Board Certification section</td></tr></table>";
            return this.replaceEndTags(this.replaceStartTags(html, "a", ""), "a", "");
        }

        private string getSelfDesigPrac(string oTxt)
        {
            string str1 = "";
            string pattern = "(?<SELFDESTXT>Self( |\t|\r|\v|\f|\n|(&nbsp;)|-)*Designated.*?)<.*?(?<SELFDESDATA><tr>.*?</tr>).*?</table>";
            Match match1 = Regex.Match(oTxt, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            string str2;
            if (match1.Success)
            {
                string input = Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(match1.Groups["SELFDESDATA"].ToString().Trim(), "<td>" + AnySpaces, "<td>", RegexOptions.IgnoreCase | RegexOptions.Singleline), AnySpaces + "</td>", "</td>", RegexOptions.IgnoreCase | RegexOptions.Singleline), "&#149;", "", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<td>" + AnySpaces + "</td>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                foreach (Match match2 in Regex.Matches(input, "<td>(?<SELFDESVAL>.*?)</td>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
                    str1 = str1 + "<tr><td>" + match1.Groups["SELFDESTXT"].ToString().Trim() + "</td><td></td></tr><tr><td>" + match2.Groups["SELFDESVAL"].ToString().Trim() + "</td><td></td></tr>";
                if (str1 != "")
                    input = str1;
                str2 = "<table>" + input + "</table>";
            }
            else
                str2 = "<table><tr><td>Could not find</td><td>Self-Designateed Practice Area section.</td></tr></table>";
            return str2;
        }

        private string getMDPrivInfo(string oTxt)
        {
            string pattern = "(?<MDPRIVTXT>Maryland Hospital Privilege.*?)</td>.*?<tr>(?<MDPRIVDATA>.*?)</tr>.*?</table>";
            Match match = Regex.Match(oTxt, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            string str1;
            if (match.Success)
            {
                string str2 = match.Groups["MDPRIVDATA"].ToString().Trim();
                str1 = "<table><tr><td>" + match.Groups["MDPRIVTXT"].ToString().Trim() + "</td></tr><tr><td>" + str2 + "</td><td></td></tr></table>";
            }
            else
                str1 = "<table><tr><td>Could not find</td><td>Maryland Hospital Privilege Information section.</td></tr></table>";
            return str1;
        }

        private string getMalPract(string oTxt)
        {
            string pattern = "(?<MALPRTXT>Malpractice Settlements.*?)</td>.*?<tr>(?<MALPRDATA>.*?)</tr>.*?</table>";
            Match match = Regex.Match(oTxt, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            string str1;
            if (match.Success)
            {
                string str2 = match.Groups["MALPRDATA"].ToString().Trim();
                str1 = "<table><tr><td>" + match.Groups["MALPRTXT"].ToString().Trim() + "</td></tr><tr><td>" + str2 + "</td><td></td></tr></table>";
            }
            else
                str1 = "<table><tr><td>Could not find</td><td>Malpractice Settlements section.</td></tr></table>";
            return str1;
        }

        private string getConvict(string oTxt)
        {
            string pattern = "(?<CONVTXT>Convictions for.*?)</td>.*?<tr>(?<CONVDATA>.*?)</tr>.*?</table>";
            Match match = Regex.Match(oTxt, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            string str1;
            if (match.Success)
            {
                string str2 = match.Groups["CONVDATA"].ToString().Trim();
                str1 = "<table><tr><td>" + match.Groups["CONVTXT"].ToString().Trim() + "</td></tr><tr><td>" + str2 + "</td><td></td></tr></table>";
            }
            else
                str1 = "<table><tr><td>Could not find</td><td>Convictions for any crime section.</td></tr></table>";
            return str1;
        }

        protected string replaceStartTags(string html, string tag, string newvalue)
        {
            if (tag == "!" || tag == "!-" || tag == "!--")
            {
                tag = "!--";
                html = Regex.Replace(html, "<" + tag + ".*?->", newvalue, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }
            else
                html = Regex.Replace(html, "<" + tag + "( .*?|)>", newvalue, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return html;
        }

        protected string replaceEndTags(string html, string tag, string newvalue)
        {
            html = Regex.Replace(html, "</" + tag + "( .*?|)>", newvalue, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return html;
        }

        protected string HttpPrefix(string theLoc, string prefix)
        {
            string str = theLoc;
            string pattern = "(^(http|https))://(?<LOCATION>.*?)('|\"|>| |\t|$)";
            if (!Regex.Match(theLoc, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline).Success)
            {
                if (theLoc.StartsWith("/") && prefix.EndsWith("/"))
                    theLoc = theLoc.Substring(1);
                if (!theLoc.StartsWith("/") && !prefix.EndsWith("/"))
                    theLoc = "/" + theLoc;
                str = prefix + theLoc;
            }
            return str;
        }

        protected void handleExpirable(string result, string dr_lic_exp, string patrn_lic_exp, string expGrpName)
        {
            Match match = Regex.Match(result, patrn_lic_exp, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success)
                return;
            try
            {
                DateTime dateTime = DateTime.Parse(match.Groups[expGrpName].ToString().Trim());
                try
                {
                    this.checkExpirable(DateTime.Parse(dr_lic_exp), dateTime);
                }
                catch (Exception ex)
                {
                    this.checkExpirable(new DateTime(1492, 1, 1), dateTime);
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
            }
        }
    }
}