using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using HtmlAgilityPack;
using PlugIn4_5;


namespace MDBPPlugIn
{
    public class Extract
    {
        public static string Address(HtmlDocument html)
        {
            HtmlNode subHeadNode = html.DocumentNode.SelectSingleNode("//span[@class='subhead' and contains(., 'Public Address')]");

            if (subHeadNode != null)
            {
                HtmlNodeCollection bodyTextNodes = subHeadNode.ParentNode.SelectNodes(".//td[@class='bodytext']");

                if (bodyTextNodes != null)
                {
                    StringBuilder builder = new StringBuilder(String.Empty);

                    foreach (HtmlNode bodyText in bodyTextNodes)
                    {
                        builder.Append(Regex.Replace(bodyText.InnerText.Trim(), "&nbsp;", " "));
                    }

                    return String.Format("<tr><td>{0}</td><td>{1}</td></tr>", subHeadNode.InnerText.Trim(), builder.ToString());
                }
                else
                {
                    return String.Empty;
                }
            }
            else
            {
                return String.Empty;
            }
        }

        public static string Convictions(HtmlDocument html, PlugInClass plugin)
        {
            HtmlNode tableNode = html.DocumentNode.SelectSingleNode("//table[contains(., 'Convictions for any crime')]");

            if (tableNode != null)
            {
                HtmlNode bodyText = tableNode.SelectSingleNode(".//td[@class='bodytext']");

                if (bodyText != null)
                {
                    string text = bodyText.InnerText.Trim();

                    if (!plugin.HasSacntion)
                    {
                        plugin.SetFlag(Regex.IsMatch(text, "None reported by the courts", RegexOptions.IgnoreCase) ? SanctionType.None : SanctionType.Red);
                    }

                    return String.Format("<tr><td>{0}</td><td>{1}</td></tr>", "Convictions for any crime involving moral turpitude", text);
                }
                else
                {
                    return String.Empty;
                }
            }
            else
            {
                return String.Empty;
            }
        }

        public static string DisciplineAction(HtmlDocument html, PlugInClass plugin)
        {
            HtmlNodeCollection tableNodes = html.DocumentNode.SelectNodes("//table[contains(., 'Known Disciplinary Actions')]/tr[contains(., 'Date of Actions:')]");

            if (tableNodes != null)
            {
                List<Table> tables = new List<Table>();

                for (int t = 0; t < tableNodes.Count; t++)
                {
                    HtmlNodeCollection rowNodes = tableNodes[t].SelectNodes(".//tr[@class='bodytext']");

                    if (rowNodes != null)
                    {
                        Table table = new Table();

                        for (int r = 0; r < rowNodes.Count; r++)
                        {
                            HtmlNodeCollection tds = rowNodes[r].SelectNodes("./td");

                            if (tds != null)
                            {
                                if (tds.Count >= 2)
                                {
                                    table.Data.Add(new TableData(tds[0], tds[1]));
                                }

                                if (tds.Count >= 4)
                                {
                                    table.Data.Add(new TableData(tds[2], tds[3]));
                                }                             
                            }
                        }

                        tables.Add(table);                      
                    }
                }

                if (tables.Count > 0)
                {
                    StringBuilder builder = new StringBuilder("<tr><td>--- Disciplinary Actions</td><td> ---</td></tr>");

                    for (int i = 0; i < tables.Count; i++)
                    {
                        builder.AppendFormat("<tr><td> - Discipline</td><td> {0} of {1} - </td></tr>", i + 1, tables.Count);

                        foreach (TableData data in tables[i].Data)
                        {
                            if (!plugin.HasSacntion)
                            {
                                plugin.SetFlag(Regex.IsMatch(data.Value, "No actions reported during the last ten year period", RegexOptions.IgnoreCase) ? SanctionType.None : SanctionType.Red);
                            }

                            builder.AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", data.Header, data.Value);
                        }
                    }

                    return builder.ToString();
                }
                else
                {
                    return String.Empty;
                }
            }
            else
            {
                HtmlNodeCollection noActions = html.DocumentNode.SelectNodes("//table[contains(., 'Known Disciplinary Actions')]/tr[contains(., 'No actions reported during the last ten year period')]/td");

                if (noActions != null)
                {
                    StringBuilder builder = new StringBuilder("<tr><td>--- Disciplinary Actions</td><td> ---</td></tr>");
                    builder.AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", "Summary", "No actions reported during the last ten year period.");

                    return builder.ToString();
                }
                else
                {
                    return String.Empty;
                }
            }
        }

        public static string Graduated(HtmlDocument html)
        {
            HtmlNode bodyTextNode = html.DocumentNode.SelectSingleNode("//td[@colspan=5 and @class='bodytext']");

            if (bodyTextNode != null)
            {
                string[] split = Regex.Split(bodyTextNode.InnerText.Trim(), ":");

                if (split.Length == 2)
                {
                    return String.Format("<tr><td>{0}</td><td>{1}</td></tr>", Regex.Replace(split[0], @"&nbsp;", " ").Trim(), Regex.Replace(split[1], @"&nbsp;", " ").Trim());
                }
                else
                {
                    return String.Empty;
                }
            }
            else
            {
                return String.Empty;
            }
        }

        public static string LicenseAndEducation(HtmlDocument html, PlugInClass plugin)
        {
            HtmlNode subHeadNode = html.DocumentNode.SelectSingleNode("//span[@class='subhead' and contains(., 'License and Education')]");

            if (subHeadNode != null)
            {
                HtmlNodeCollection bodyTextNodes = subHeadNode.ParentNode.SelectNodes(".//tr[@class='bodytext']");

                if (bodyTextNodes != null)
                {
                    StringBuilder builder = new StringBuilder("<tr><td>--- License and Education</td><td> ---</td></tr>");

                    foreach (HtmlNode bodyText in bodyTextNodes)
                    {
                        string[] split = Regex.Split(bodyText.InnerText.Trim(), ":");

                        if (split.Length == 2)
                        {
                            string label = split[0].Trim();
                            string text = Regex.Replace(split[1].Trim(), "&nbsp;", String.Empty);

                            builder.AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", label, text);

                            if (Regex.IsMatch(label, "License Status", RegexOptions.IgnoreCase))
                            {
                                plugin.SetFlag(text.ToUpper() == "ACTIVE" ? SanctionType.None : SanctionType.Red);
                            }
                            else if (Regex.IsMatch(label, "License Expiration", RegexOptions.IgnoreCase))
                            {
                                plugin.SetExpirable(text);
                            }
                        }
                    }

                    return builder.ToString();
                }
                else
                {
                    return String.Empty;
                }
            }
            else
            {
                return String.Empty;
            }
        }

        public static string MalpracticeJudgements(HtmlDocument html, PlugInClass plugin)
        {
            StringBuilder builder = new StringBuilder("<tr><td>--- Malpractice</td><td> ---</td></tr>");
            HtmlNode judgementNode = html.DocumentNode.SelectSingleNode("//tr[contains(., 'Malpractice Judgments')]");
            HtmlNode settlementsNode = html.DocumentNode.SelectSingleNode("//tr[contains(., 'Malpractice Settlements')]");

            if (judgementNode != null)
            {
                HtmlNode bodyText = judgementNode.SelectSingleNode(".//following-sibling::tr");

                if (bodyText != null)
                {
                    string text = bodyText.InnerText.Trim();
                    builder.AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", "Judgements", text);

                    if (!plugin.HasSacntion)
                    {
                        plugin.SetFlag(Regex.IsMatch(text, "None Reported", RegexOptions.IgnoreCase) ? SanctionType.None : SanctionType.Red);
                    }
                }
            }

            if (settlementsNode != null)
            {
                HtmlNode bodyText = judgementNode.SelectSingleNode(".//following-sibling::tr");

                if (bodyText != null)
                {
                    string text = bodyText.InnerText.Trim();
                    builder.AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", "Settlements", text);

                    if (!plugin.HasSacntion)
                    {
                        plugin.SetFlag(Regex.IsMatch(text, "None Reported", RegexOptions.IgnoreCase) ? SanctionType.None : SanctionType.Red);
                    }
                }
            }

            return builder.ToString();
        }

        public static string Name(HtmlDocument html)
        {
            HtmlNode bodyTextNode = html.DocumentNode.SelectSingleNode("//td[@align='center' and @bgcolor='#E3D6A1' and @class='LargeFont']");

            if (bodyTextNode != null)
            {
                return String.Format("<tr><td>{0}</td><td>{1}</td></tr>", "Name", Regex.Replace(bodyTextNode.InnerText.Trim(), @"&nbsp;", " "));
            }
            else
            {
                return String.Empty;
            }
        }

        public static string PendingCharges(HtmlDocument html, PlugInClass plugin)
        {
            HtmlNode tableNode = html.DocumentNode.SelectSingleNode("//table[contains(., 'Pending Charges')]");

            if (tableNode != null)
            {
                HtmlNode bodyText = tableNode.SelectSingleNode(".//tr[@class='bodytext']");

                if (bodyText != null)
                {
                    string text = bodyText.InnerText.Trim();

                    if (!plugin.HasSacntion)
                    {
                        plugin.SetFlag(Regex.IsMatch(text, "None", RegexOptions.IgnoreCase) ? SanctionType.None : SanctionType.Red);
                    }

                    return String.Format("<tr><td>Pending Charges</td><td>{0}</td></tr>", text);
                }
                else
                {
                    return String.Empty;
                }
            }
            else
            {
                return String.Empty;
            }
        }

        public static string PrimaryPracticeSetting(HtmlDocument html)
        {
            HtmlNode subHeadNode = html.DocumentNode.SelectSingleNode("//span[@class='subhead' and contains(., 'Primary Practice')]");

            if (subHeadNode != null)
            {
                HtmlNodeCollection bodyTextNodes = subHeadNode.ParentNode.SelectNodes(".//td[@class='bodytext']");

                if (bodyTextNodes != null)
                {
                    StringBuilder builder = new StringBuilder(String.Empty);

                    foreach (HtmlNode bodyText in bodyTextNodes)
                    {
                        builder.Append(Regex.Replace(bodyText.InnerText.Trim(), "&nbsp;", " "));
                    }

                    return String.Format("<tr><td>{0}</td><td>{1}</td></tr>", subHeadNode.InnerText.Trim(), builder.ToString());
                }
                else
                {
                    return String.Empty;
                }
            }
            else
            {
                return String.Empty;
            }
        }

        public static string TimeStamp(HtmlDocument html)
        {
            HtmlNode bodyTextNode = html.DocumentNode.SelectSingleNode("//td[@align='center' and @bgcolor='#E3D6A1' and @class='bodytext']");

            if (bodyTextNode != null)
            {
                return String.Format("<tr><td>{0}</td><td>{1}</td></tr>", "Date Extracted", Regex.Replace(bodyTextNode.InnerText.Trim(), @"[^\d\/]+", String.Empty));
            }
            else
            {
                return String.Empty;
            }
        }
    }
}
