using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using PlugIn4_5;
using RestSharp;
using HtmlAgilityPack;


namespace DEADupPlugIn
{
    /// <summary>
    /// This plugin used for Drug Enforcement Administration Duplicate Site
    /// https://apps.deadiversion.usdoj.gov/webforms/validateLogin.jsp"
    /// </summary>

    public class PlugInClass : IPlugIn
    {
        private const string HostUrl = "https://apps.deadiversion.usdoj.gov/";
        private const string SearchUrl = "webforms/validateLogin.jsp";
        private const string ValidateUrl = "webforms/validateLogin.do";
        private const string DetailsUrl = "webforms/validateSelect.do";

        public override string Fetch(DataRow dr)
        {
            Initialize(dr, true);

            string message = String.Empty;
            string output = String.Empty;
            string ssn = Regex.Replace(dr["ss_no"].ToString().Trim(), @"\-", String.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            Result<string> fields = HasFields(ssn);

            if (fields.IsValid)
            {
                Result<string> content = Search(ssn);

                if (content.IsValid)
                {
                    Result<string> searchResult = Parse(content.Value);

                    if (searchResult.IsValid)
                    {
                        output = searchResult.Value;
                        HandleModes(content.Value, searchResult.Value);
                    }
                    else
                    {
                        message = searchResult.Message;
                    }
                }
                else
                {
                    message = content.Message;
                }
            }
            else
            {
                message = fields.Message;
            }

            return ProcessResults(output, message);
        }

        private string BuildString(MatchCollection mc)
        {
            StringBuilder builder = new StringBuilder(String.Empty);

            foreach (Match m in mc)
            {
                builder.Append(m.Value);
            }

            return builder.ToString();
        }

        private string BuildTable(List<string> labels, List<string> values, MatchCollection other)
        {
            StringBuilder builder = new StringBuilder(String.Format("<tr><td>{0} {1} {2} {3}</td><td>{4}</td></tr>", other[0], other[1], other[2], other[3], other[4]));

            for (int i = 0; i < labels.Count; i++)
            {
                builder.AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", labels[i], values[i]);
            }

            return builder.ToString();
        }

        private List<string> FormatData(MatchCollection mc, string pattern)
        {
            List<string> collection = new List<string>();

            foreach (Match m in mc)
            {
                collection.Add(Regex.Replace(m.Value, pattern, String.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase));
            }

            return collection;
        }

        private string FormatDate(string date)
        {
            date = Regex.Replace(date, @"Expire Date: </td><td>", String.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            date = Regex.Replace(date, @"\-", "/", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return date;
        }

        private void HandleModes(string html, string results)
        {
            SetSanction((Regex.IsMatch(results, @"((This DEA Number is)|(Status)) ?(<\/td><td>)?Active(<\/td>)?", RegexOptions.Singleline | RegexOptions.IgnoreCase)) ? SanctionType.None : SanctionType.Red);

            Match m = Regex.Match(results, @"(Expire Date: <\/td><td>)([\d\s\-]+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (m.Success)
            {
                HandleExpirables(provider.ExpirationDate, FormatDate(m.Value));
            }

            html = Regex.Replace(html, "<img src=\"", "<img src=\"https://www.deadiversion.usdoj.gov");
            html = Regex.Replace(html, "<td>.*?<NOSCRIPT><BLOCKQUOTE>.*?</NOSCRIPT>.*?</td>.*?</tr>.*?<tr>.*?</tr>", String.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "online verification..*?</TD></TR>.*?</TABLE>.*?</TD></TR>.*?</table>.*?</td></tr>.*?alt=\"Validate\">", "</td></tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            
            pdf.Html = html;

            pdf.ConvertToABCImage(new ImageParameters()
                {
                    BaseUrl = "https://www.deadiversion.usdoj.gov",
                });
        }

        private Result<string> HasFields(string ssn)
        {
            if (String.IsNullOrEmpty(provider.LastName))
            {
                return Result<string>.Failure(ErrorMsg.InvalidLastName);
            }
            else if (String.IsNullOrEmpty(provider.LicenseNumber))
            {
                return Result<string>.Failure(ErrorMsg.InvalidLicense);
            }
            else if (String.IsNullOrEmpty(ssn) || ssn.Length != 9)
            {
                return Result<string>.Failure(ErrorMsg.InvalidSSN);
            }
            else
            {
                return Result<string>.Success(String.Empty);
            }
        }

        private Result<string> Parse(string html)
        {
            Match statusMatch = Regex.Match(html, @"<p>([\w\s])+</p>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (statusMatch.Success)
            {
                try 
                {
                    StringBuilder builder = new StringBuilder();
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    HtmlNode node = doc.DocumentNode.SelectSingleNode(".//table[@class='clearPaddedTable']");

                    if (node != null)
                    {
                        HtmlNodeCollection trs = node.SelectNodes(".//tr/td");

                        if (trs != null)
                        {
                            foreach (var tr in trs)
                            {
                                Match m = Regex.Match(tr.InnerHtml, "<b>(?<HEADER>.*?)</b>(?<VALUE>.*?)<", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                                if (m.Success)
                                {
                                    builder.AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", m.Groups["HEADER"].ToString(), m.Groups["VALUE"].ToString());
                                }
                                else
                                {
                                    builder.AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", tr.InnerText, "");
                                }
                            }
                        }
                    }



                    return Result<string>.Success(builder.ToString());
                }
                catch (Exception exception)
                {
                    return Result<string>.Exception(exception);
                }
            }
            else
            {
                return Result<string>.Failure(ErrorMsg.NoResultsFound);
            }
        }

        private Result<string> Search(string ssn)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                RestClient client = new RestClient(HostUrl);
                RestRequest request = new RestRequest(SearchUrl, Method.GET);
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Dictionary<string, string> cookieJar = new Dictionary<string, string>();

                    foreach (RestResponseCookie cookie in response.Cookies)
                    {
                        cookieJar.Add(cookie.Name, cookie.Value);
                    }

                    request = new RestRequest(ValidateUrl, Method.POST);
                    request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                    request.AddQueryParameter("deaNum", provider.LicenseNumber);
                    request.AddQueryParameter("lname", provider.LastName);
                    request.AddQueryParameter("ssn", ssn);
                    request.AddQueryParameter("taxid", String.Empty);
                    request.AddQueryParameter("buttons.next.x", "40");
                    request.AddQueryParameter("buttons.next.y", "15");
                    request.AddQueryParameter("buttons.next", "Login");

                    foreach (string key in cookieJar.Keys)
                    {
                        request.AddCookie(key, cookieJar[key]);
                    }

                    response = client.Execute(request);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        request = new RestRequest(DetailsUrl, Method.POST);
                        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                        request.AddQueryParameter("deaNum", provider.LicenseNumber);
                        request.AddQueryParameter("buttons.next.x", "28");
                        request.AddQueryParameter("buttons.next.y", "11");
                        request.AddQueryParameter("buttons.next", "Validate");

                        foreach (string key in cookieJar.Keys)
                        {
                            request.AddCookie(key, cookieJar[key]);
                        }

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
                        return Result<string>.Failure(ErrorMsg.CannotAccessSearchForm);
                    }
                }
                else
                {
                    return Result<string>.Failure(ErrorMsg.CannotAccessSite);
                }
            }
            catch (Exception exception)
            {
                return Result<string>.Exception(exception);
            }
        }
    }
}
