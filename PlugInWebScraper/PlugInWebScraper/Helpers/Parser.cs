using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlugInWebScraper.Helpers
{
    /// <summary>
    /// Summary description for Parser.
    /// </summary>
    public class Parser
    {
        public string txt;
        private string server;
        private string db;
        public string tempHtml;
        private string defPath;
        private ArrayList tds;
        private ArrayList al;
        private string err;
        private string url;
        private int id;
        bool err_flag;
        bool single_parse;
        string user;

        private enum Type
        {
            MULT,
            ERR
        }

        public Parser(string s, string d, int id,
            string txt, string url, string def, string user)
        {
            this.id = id;
            this.server = s;
            this.db = d;
            this.url = url;
            this.tds = null;
            this.defPath = def;
            this.txt = txt;
            this.err = "";
            this.err_flag = false;
            this.tempHtml = removeSpaces(removeTags(txt, "font"));
            this.al = new ArrayList();
            this.tds = new ArrayList();
            this.single_parse = false;
            this.user = user;

            //al.Add("url");
            //al.Add(url);
        }

        public string makeSql()
        {
            string strSql = "";
            string strVals = "";
            int cnt = this.al.Count;
            int inc = this.single_parse ? 1 : 2;

            // build Sql
            for (int i = 0; i < cnt; i += inc)
            {
                string temp = (string)al[i];
                // skip blank rows or removes with [remove]
                if (temp == "[remove]" || temp == "") continue;
                // save rows with [save] and convert to blank
                if (temp == "[save]") temp = "";
                if (temp != "") strVals += removeQuotes(temp);
                if (!single_parse)
                {
                    if ((i + 1) >= cnt) continue;
                    if ((string)this.al[i + 1] == "[blank]") this.al[i + 1] = "";
                    else if (temp != "") strVals += ": ";
                    strVals += removeQuotes((string)this.al[i + 1]) + "\r\n";
                }
                else strVals += "\r\n";
            }

            strSql = "insert into outgoing (id, l_incoming, note, url, fetch_date) " +
                "values ('" + this.user + "'," + this.id + ",'" + AddHeader(strVals) + "','" + url +
                "', GetDate())";
            strSql = strSql.Replace("::", ":");
            return strSql;
        }

        private string AddHeader(string str)
        {
            DateTime currDate = DateTime.Now;
            string d = String.Format("{0:G}", currDate);
            string head = "***  EchoNet - Electronic Query Results  ***\r\n\r\n";
            head += "Query Date: " + d;
            head += "\r\nData Source: " + this.url + "\r\n\r\nResults:\r\n";
            return head + str;
        }

        public void readDefFile()
        {
            parseTd();
            TdGetAll();
        }

        public bool parseDef(string line)
        {
            if (line == null) return false;
            // parse commands in def file
            if (line == "parsetd")
            {
                parseTd();
            }
            else if (line.StartsWith("rmtagrow"))
            {
                // remove tag
                line = line.Substring(9);
                removeTagRow(line);
            }
            else if (line.StartsWith("rmtag"))
            {
                // remove tag
                line = line.Substring(6);
                this.tempHtml = removeTags(this.tempHtml, line);
            }
            else if (line.StartsWith("addrowat"))
            {
                // remove tag
                line = line.Substring(9);
                // split tags
                string[] s = line.Split('=');
                try
                {
                    int i = int.Parse(s[1].Trim('"'));
                    if (i > 0 && i < al.Count)
                    {
                        al.Insert(i, s[0].Trim('"'));
                    }
                }
                catch (Exception) { }
            }
            else if (line.StartsWith("addrow"))
            {
                int i = 0;
                if (line.StartsWith("addrowa"))
                {
                    i = 1;
                }
                line = line.Substring(8);
                // split tags
                string[] s = line.Split('=');
                AddRow(s[0].Trim('"'), s[1].Trim('"'), i);
            }
            else if (line.StartsWith("add"))
            {
                // remove tag
                line = line.Substring(4);
                Add(line);
            }
            else if (line.StartsWith("rmcomment"))
            {
                // remove tag
                removeComments();
            }
            else if (line.StartsWith("rmbefore"))
            {
                // remove tag
                line = line.Substring(9);
                // remove tag
                RemoveBefore(line.Trim('"'));
            }
            else if (line.StartsWith("rmtextf"))
            {
                // remove tag
                line = line.Substring(8);
                // remove tag
                RemoveTextFront(line);
            }
            else if (line.StartsWith("rmafter"))
            {
                // remove tag
                line = line.Substring(8);
                // remove tag
                RemoveAfter(line.Trim('"'));
            }
            else if (line.StartsWith("grab"))
            {
                // remove tag
                line = line.Substring(5);
                grab(line);
            }
            else if (line == "makern")
            {
                MakeNewlines();
            }
            else if (line.StartsWith("tddef"))
            {
                // remove tag
                line = line.Substring(6);
                // split tags
                string[] s = line.Split('=');
                // add pair to list
                this.al.Add(s[0].Trim('"'));
                this.al.Add(getRow(s[1]));
            }
            else if (line.StartsWith("replaceinrow"))
            {
                // remove tag
                line = line.Substring(13);
                // split tags
                string[] s = line.Split('`');
                if (s.Length == 3)
                {
                    try
                    {
                        int i = int.Parse(s[2]);
                        if (i < al.Count)
                        {
                            string tt = (string)al[i];
                            al[i] = tt.Replace(s[0].Trim('"'), s[1].Trim('"').Replace("\\r\\n", "\r\n"));
                        }
                    }
                    catch (Exception) { }
                }
            }
            else if (line == "tdall")
            {
                TdGetAll();
            }
            else if (line.StartsWith("rmrow"))
            {
                // remove tag
                line = line.Substring(6);
                // split tags
                string[] s = line.Split('=');
                // add pair to list
                RemoveRow(s[0].Trim('"'), s[1].Trim('"'));
            }
            else if (line.StartsWith("split"))
            {
                // remove tag
                line = line.Substring(6);
                // split tags
                SplitData(line.Trim('"'));
            }
            else if (line.StartsWith("namerow"))
            {
                // remove tag
                line = line.Substring(8);
                // split tags
                string[] s = line.Split('=');
                NameRow(s[0].Trim('"'), s[1]);
            }
            else if (line == "singleparse")
            {
                // remove tag
                this.single_parse = true;
            }
            else if (line.StartsWith("rmblank"))
            {
                // remove blank rows
                bool b = false;
                if (line.EndsWith("1")) b = true;
                RemoveBlankRows(b);
            }
            else if (line.StartsWith("swaprow"))
            {
                // remove tag
                line = line.Substring(8);
                // split tags
                string[] s = line.Split('=');
                SwapRow(s[0].Trim('"'), s[1]);
            }
            else if (line.StartsWith("rmrowat"))
            {
                // remove tag
                line = line.Substring(8);
                // split tags
                try
                {
                    int i = int.Parse(line);
                    if (i > 0 && i < al.Count)
                    {
                        al.RemoveAt(i);
                    }
                }
                catch (Exception) { }
            }
            else if (line.StartsWith("replacerow"))
            {
                // remove tag
                line = line.Substring(11);
                // split tags
                string[] s = line.Split('`');
                ReplaceRows(s[0].Trim('"'), s[1].Trim('"'));
            }
            else if (line.StartsWith("replace"))
            {
                // remove tag
                line = line.Substring(8);
                // split tags
                string[] s = line.Split('`');
                Replace(s[0].Trim('"'), s[1].Trim('"'));
            }
            else if (line == "saveall")
            {
                SaveAll();
            }
            else if (line.StartsWith("trimf"))
            {
                int i = 6;
                int tag = 1;
                if (line.StartsWith("trimftag"))
                {
                    i = 9;
                    tag = 0;
                }
                line = line.Substring(i);
                // split tags
                string[] s = line.Split('=');
                // add pair to list
                TrimFront(s[0].Trim('"'), s[1], tag);
            }
            else if (line.StartsWith("trimchar"))
            {
                bool back = line.Substring(8, 1) == "b";
                line = line.Substring(10);
                // split tags
                string[] s = line.Split('=');
                if (back)
                    RemoveBackChar(s[0].Trim('"'), s[1].Trim('"'));
                else
                    RemoveFrontChar(s[0].Trim('"'), s[1].Trim('"'));
            }
            else if (line.StartsWith("trimb"))
            {
                int i = 6;
                int tag = 1;
                if (line.StartsWith("trimbtag"))
                {
                    i = 9;
                    tag = 0;
                }
                line = line.Substring(i);
                // split tags
                string[] s = line.Split('=');
                // add pair to list
                TrimBack(s[0].Trim('"'), s[1], tag);
            }

            else if (line.StartsWith("mult"))
            {
                // remove tag
                line = line.Substring(5);
                checkError(line, Type.MULT, false);
            }
            else if (line.StartsWith("!err"))
            {
                // remove tag
                line = line.Substring(5);
                checkError(line, Type.ERR, true);
            }
            else if (line.StartsWith("err"))
            {
                // remove tag
                line = line.Substring(4);
                checkError(line, Type.ERR, false);
            }
            return true;
        }

        private void MakeNewlines()
        {
            this.tempHtml = this.tempHtml.Replace("<rn>", "\r\n");
        }

        private void SplitData(string sp)
        {
            string[] arr = this.tempHtml.Split(sp.ToCharArray());
            foreach (string s in arr)
            {
                if (s == "") this.al.Add("[remove]");
                else this.al.Add(" ");
                this.al.Add(removeSpaces(s));
            }
        }

        private void NameRow(string t, string a)
        {
            int tv = int.Parse(a);
            if ((tv * 2) >= al.Count) return;
            al[tv * 2] = t;
        }

        private void RemoveTextFront(string s)
        {
            int tv = int.Parse(s);
            this.tempHtml = this.tempHtml.Substring(tv);
        }

        private void RemoveBackChar(string t, string rm)
        {
            for (int i = 0; i < al.Count; i += 2)
            {
                if ((i + 1) >= al.Count) break;
                if ((string)al[i] == t)
                {
                    string str = (al[i + 1].ToString()).Trim();
                    if (str.EndsWith(rm))
                    {
                        al[i + 1] = str.Substring(0, str.Length - rm.Length);
                    }
                    break;
                }
            }
        }

        private void RemoveFrontChar(string t, string rm)
        {
            for (int i = 0; i < al.Count; i += 2)
            {
                if ((i + 1) >= al.Count) break;
                if ((string)al[i] == t)
                {
                    string str = (al[i + 1].ToString()).Trim();
                    if (str.StartsWith(rm))
                    {
                        al[i + 1] = str.Substring(rm.Length);
                    }
                    break;
                }
            }
        }

        private void RemoveBlankRows(bool b)
        {
            for (int i = al.Count - 1; i > -1; i--)
            {
                if ((string)al[i] == "")
                {
                    al.RemoveAt(i);
                    if (b) break;
                }
            }
        }

        private void ReplaceRows(string f, string r)
        {
            for (int i = 0; i < al.Count; i++)
            {
                if ((string)al[i] == f)
                {
                    al[i] = r;
                    break;
                }
            }
        }

        private void TrimFront(string t, string a, int tag)
        {
            int tv = int.Parse(a);
            for (int i = 0; i < al.Count; i += 2)
            {
                if ((i + tag) >= al.Count) break;
                if ((string)al[i] == t)
                {
                    al[i + tag] = ((string)al[i + tag]).Remove(0, tv);
                    break;
                }
            }
        }

        private void TrimBack(string t, string a, int tag)
        {
            int tv = int.Parse(a);
            for (int i = 0; i < al.Count; i += 2)
            {
                if ((i + tag) >= al.Count) break;
                if ((string)al[i] == t)
                {
                    string temp = (string)al[i + tag];
                    al[i + tag] = temp.Substring(0, temp.Length - tv);
                    break;
                }
            }
        }

        private void SaveAll()
        {
            //al.Add("[save]");
            al.Add(this.tempHtml);
        }

        private void RemoveRow(string a, string b)
        {

            for (int i = 0; i < al.Count; i += 2)
            {
                if ((i + 1) >= al.Count) break;
                if ((string)al[i] == a && (string)al[i + 1] == b)
                {
                    al[i] = "[remove]";
                }
            }
        }

        private void AddRow(string a, string b, int w)
        {

            for (int i = 0; i < al.Count; i++)
            {
                if ((string)al[i] == a)
                {
                    al.Insert(i + w, b);
                    break;
                }
            }
        }

        private void SwapRow(string a, string s)
        {

            int w = int.Parse(s);

            for (int i = 0; i < al.Count; i++)
            {
                if ((i + w) >= al.Count) break;
                if ((string)al[i] == a)
                {
                    string temp = (string)al[i];
                    al[i] = (string)al[i + w];
                    al[i + w] = temp;
                    break;
                }
            }
        }

        private void grab(string line)
        {
            string[] arr = line.Split('=');
            arr[0] = arr[0].Trim('"');
            arr[1] = arr[1].Trim('"');

            string pattern = @"" + escapeRegEx(arr[0]) + ".*?" + escapeRegEx(arr[1]);
            this.tempHtml = Regex.Matches(this.tempHtml, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline)[0].ToString();
        }

        private void checkError(string line, Type type, bool opp)
        {
            string[] arr = line.Split('=');
            string errMsg = arr[1].Trim('"');
            arr[0] = arr[0].Trim('"');
            arr = arr[0].Split(' ');
            string pattern = "";
            // handle internal new lines
            foreach (string s in arr)
            {
                pattern += s + "\\s+";
            }
            pattern = @"" + pattern.Substring(0, pattern.Length - 3);
            int cnt = Regex.Matches(this.tempHtml, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline).Count;
            if ((type == Type.MULT && cnt > 1) ||
                (!opp && type == Type.ERR && cnt > 0) ||
                (opp && type == Type.ERR && cnt < 1))
            {

                this.err = errMsg;
                //al.Add("Error");
                //al.Add(errMsg);
                throw new Exception();
            }
        }

        private string escapeRegEx(string line)
        {
            line = line.Replace("*", "[*]");
            line = line.Replace("?", "[?]");
            line = line.Replace(".", "[.]");
            line = line.Replace("+", "[+]");
            line = line.Replace(")", "[)]");
            line = line.Replace("(", "[(]");
            return line;
        }

        private void TdGetAll()
        {
            int cnt = this.tds.Count;
            for (int i = 0; i < cnt; i++)
            {
                string row = getRow(i + "");
                al.Add(getRow(i + ""));
            }
            // if count is odd, add blank column before last
            /*if(al.Count % 2 != 0) {
                al.Insert(al.Count, "[save]");
            }*/
        }

        private string getRow(string row)
        {
            if (this.tds == null) return "";

            int i = int.Parse(row);
            return removeSpaces(removeTags((string)this.tds[i], "a"));
        }

        public void parseTd()
        {

            if (this.tds.Count > 0) this.tds.Clear();
            string pattern = @"<td.*?</td>";
            MatchCollection mc = Regex.Matches(this.tempHtml, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (Match m in mc)
            {
                string temp = removeTags(m.ToString(), "td");
                this.tds.Add(temp);
            }
        }

        private string removeTags(string txt, string tag)
        {
            // remove opening tags: <x ...> 
            string pattern = @"<" + tag + ".*?>";
            txt = Regex.Replace(txt, pattern, "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            // remove closing tags: </x> tags
            pattern = @"</" + tag + ">";
            txt = Regex.Replace(txt, pattern, "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return txt;
        }
        private void removeTagRow(string tag)
        {
            for (int i = 0; i < al.Count; i++)
            {
                string txt = (string)al[i];
                // remove opening tags: <x ...> 
                string pattern = @"<" + tag + ".*?>";
                txt = Regex.Replace(txt, pattern, "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                // remove closing tags: </x> tags
                pattern = @"</" + tag + ">";
                al[i] = Regex.Replace(txt, pattern, "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                al[i] = ((string)al[i]).Trim();
            }
        }

        private void removeComments()
        {
            // remove opening tags: <x ...> 
            string pattern = @"<!--.*?-->";
            this.tempHtml = Regex.Replace(this.tempHtml, pattern, "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        private string removeQuotes(string txt)
        {

            return txt.Replace("'", "''");
        }

        private string removeSpaces(string txt)
        {

            txt = txt.Trim();
            txt = txt.Replace("&nbsp;", " ");
            txt = txt.Replace("&nbsp", " "); // in case they forgot the semicolon			
            string pattern = @"\s+";
            return Regex.Replace(txt, pattern, " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            //return txt;
        }

        private string sqlTidy(string txt)
        {

            return removeQuotes(removeSpaces(txt));
        }

        private void Replace(string a, string b)
        {

            this.tempHtml = this.tempHtml.Replace(a, b);
        }

        private void Add(string a)
        {
            this.tempHtml = a + this.tempHtml;
        }

        private void RemoveBefore(string s)
        {

            int i = this.tempHtml.IndexOf(s);
            if (i > 0) this.tempHtml = this.tempHtml.Substring(i);
        }

        private void RemoveAfter(string s)
        {

            int i = this.tempHtml.IndexOf(s);
            if (i > 0) this.tempHtml = this.tempHtml.Substring(0, i);
        }

        private bool IsError(string s)
        {
            return s.Substring(0, 5) == "Error";
        }
    }

}
