using PlugIn4_5;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;


namespace DOCPlugIn
{
    public class PlugInClass : IPlugIn
    {
        // provide friendly names for internal db names
        private Dictionary<string, string> friendlyName = new Dictionary<string, string>();
        private static List<List<string>> tableList = null;

        public PlugInClass()
        {
            friendlyName.Add("Name_and_Address", "Name and Address");
            friendlyName.Add("Effective_Date", "Effective Date");
            friendlyName.Add("Expiration_Date", "Expiration Date");
            friendlyName.Add("Type_of_Denial", "Type of Denial");
        }

        private bool RunSearch(string searchString, ref string result, bool areMultipleResultsAllowed, bool isNameSearch, string firstName, string lastName)
        {
            // there are 4 outcomes of any given search
            // 1 - exact match - format and return result
            // 2 - multiple match - depending on conditions may return results or may report error
            // 3 - no match found.  If it's not the final search, it will return an empty string, signalling search should continue
            // 4 - unexpected error

            bool wasMatchOrStoppingPointFound = false;
            DataTable dt = new DataTable();

            try
            {
                dt = CallQuery(searchString);

                if (dt == null)
                {
                    result = ErrorMsg.NoResultsFound;
                }
                else if (dt.Rows.Count > 0)  // dt != null
                {
                    result = ParseResults(dt, firstName, lastName);

                    SetSanction(SanctionType.Red); // Found match, so set "red flag"

                    wasMatchOrStoppingPointFound = true;
                }
                else // no records found
                {
                    result += ErrorMsg.NoResultsFound;
                }
            }
            catch (Exception ex)
            {
                // todo: where do we report these exceptions
                result = ErrorMsg.UnexpectedErrorWhileCheckingDB;
            }

            return wasMatchOrStoppingPointFound;
        }

        public override string Fetch(DataRow dr)
        {
            string message = String.Empty;
            string output = String.Empty;

            try
            {
                Initialize(dr, true, new SPMConfig() { AcceptAllCertificatePolicy = true, Expect100Continue = false });
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                if (string.IsNullOrEmpty(provider.FirstName) && string.IsNullOrEmpty(provider.LastName))
                {
                    message = ErrorMsg.InvalidFirstLastName;
                }
                else
                {
                    tableList = GetTableList();

                    // 3a - check for practice (identified by only a lastname provided)
                    if (!string.IsNullOrEmpty(provider.LastName) && string.IsNullOrEmpty(provider.FirstName))
                    {
                        // allow fall-through
                        RunSearch(provider.LastName, ref output, true, false, provider.FirstName, provider.LastName);
                    }

                    // 3b - check for first and last name
                    else if (!string.IsNullOrEmpty(provider.FirstName) && !string.IsNullOrEmpty(provider.LastName))
                    {
                        // allow fall-through
                        RunSearch(String.Format("{0}|{1}", provider.FirstName, provider.LastName), ref output, true, true, provider.FirstName, provider.LastName);
                    }
                }
            }
            catch (Exception exception)
            {
                message = ErrorMsg.CustomException(exception);
            }

            return ProcessResults(output, message);
        }

        #region helper functions
        private string ParseResults(DataTable dt,string firstName, string lastName)
        {
            StringBuilder result = new StringBuilder();

            if (dt.Rows.Count > 1)
            {
                result.AppendFormat("<tr><td>Multiple Results ({0}) Found</td><td></td></tr>", dt.Rows.Count);
            }

            for (int count = 0; count < dt.Rows.Count; count++)
            {
                if (dt.Rows.Count > 1)
                {
                    result.AppendFormat("<tr><td>--------Result {0} of {1}--------</td><td></td></tr>", count, dt.Rows.Count);
                }

                foreach (DataColumn column in dt.Columns)
                {
                    result.AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", (friendlyName.ContainsKey(column.ColumnName)) 
                        ? friendlyName[column.ColumnName] : column.ColumnName, dt.Rows[count][column]);
                }
            }

            return result.ToString();
        }

        private static DataTable CallQuery(string searchString)
        {
            DataTable dt = new DataTable();
            bool matchCol = false;

            try
            {
                dt.Clear();
                dt.Columns.Add("Name_and_Address");
                dt.Columns.Add("Effective_Date");
                dt.Columns.Add("Expiration_Date");
                dt.Columns.Add("Type_of_Denial");

                for (int i = 0; i < tableList.Count; i++)
                {
                    DataRow row = dt.NewRow();
                    for (int j = 0; j < tableList[i].Count; j++)
                    {
                        matchCol = false;
                        string colData = WebUtility.HtmlDecode(Regex.Replace(tableList[i][j], @"<[^>]+>|&nbsp;", "").Trim());
                        if (searchString.Contains("|"))
                        {
                            string[] arr = searchString.Split('|');
                            if (colData.IndexOf(arr[0], StringComparison.OrdinalIgnoreCase) >= 0 && colData.IndexOf(arr[0], StringComparison.OrdinalIgnoreCase) < 40)
                            {
                                if (colData.IndexOf(arr[1], StringComparison.OrdinalIgnoreCase) >= 0) matchCol = true;
                                else matchCol = false;
                            }
                            else
                                matchCol = false;
                        }
                        else
                        {
                            DateTime dt1;
                            DateTime dt2;
                            if (DateTime.TryParse(searchString, out dt1))
                            {
                                if (DateTime.TryParse(colData, out dt2))
                                {
                                    if (dt1.CompareTo(dt2) == 0) matchCol = true;
                                    else matchCol = false;
                                }

                            }
                            else
                            {
                                if (colData.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0 && colData.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) < 40) matchCol = true;
                                else matchCol = false;
                            }
                        }

                        if (matchCol)
                        {
                            for (int k = 0; k < tableList[i].Count; k++)
                            {
                                colData = Regex.Replace(tableList[i][k], @"<[^>]+>|&nbsp;", "").Trim();
                                row[dt.Columns[k].ColumnName] = colData;
                            }
                            dt.Rows.Add(row);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return dt;
        }

        private static List<List<string>> GetTableList()
        {
            var client = new RestSharp.RestClient("https://www.bis.doc.gov/dpl/public/dpl.php");
            var request = new RestSharp.RestRequest(RestSharp.Method.GET);
            var response = client.Execute(request);
            var page = response.Content;

            page = Regex.Replace(page, "<br />", "     ", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(page);

            return doc.DocumentNode.SelectSingleNode("//table")
                .Descendants("tr")
                .Skip(1)
                .Where(tr => tr.Elements("td").Count() > 1)
                .Select(tr => tr.Elements("td").Select(td => WebUtility.HtmlDecode(td.InnerText.Replace("'", "").Replace("&#039;", "").Trim())).ToList())
                .ToList();
        }
        #endregion
    }
}
