using System;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using PlugIn4_5;
using System.Data;
using System.Web;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;

namespace DPFRPlugIn
{
	/// <summary>
	/// Echo PSV Plugin for Department of Professional and Financial Regulation
	/// https://pfr.informe.org/almsonline/almsquery/SearchIndividual.aspx
	/// 
	/// For Regulators:
	/// Alcohol & Drug Counselors                     
	///	Boilers and Pressure Vessels                     
	///	Chiropractic                                               
	///	Complementary Health Care                       
	///	Counseling Professionals                            
	///	Dental Examiners                                      
	///	Dietetic Practice  
	///	Physical Therapy                                         
	///	Psychologists                    
	///	Occupational Therapy                          
	///	Optometry  
	//	Pharmacy  
	//	Podiatric Medicine  
	//	Respiratory Care Practitioners  
	//	Social Workers   
	//	Speech, Audiology and Hearing            
	//	Electricians                                     
	//	Massage Therapy          
	//	Plumbers               
	//	Radiologic Technology               
    //  Fuel
    //  Nursing Home Administrators
	/// </summary>

	public class PlugInClass : IPlugIn
	{
        private string regExSpace = "( |\t|\r|\v|\f|\n)";

		public override string Fetch(DataRow dr) 
		{
  
            Initialize(dr, true, new SPMConfig()
            {
                AcceptAllCertificatePolicy = true,
                SecurityProtocol = new SecurityProtocol()
            });


            // lic_no
            string lic_no = dr["lic_no"].ToString().Trim().Replace("-", "").Replace(" ", "").Replace(".", "").ToUpper();
            string siteName = dr["orgName"].ToString().Trim();
			string regulatorID = "";
			string queryString = "";
			
			//Check if license number is empty
			if (lic_no == "" || lic_no.ToUpper() == "PENDING")
				return ErrorMsg.InvalidLicense;
			
			
			//Find Regulator and part of queryString
            Match m;
            string prof = "ctl00%24ctl00%24mainContent%24mainContent%24scProfessionCategory=&";
            string authSpecQual = "ctl00%24ctl00%24mainContent%24mainContent%24ctl91=ALL&";

			switch(siteName)
			{
				case "1":   // Testing Nurses
				{
					regulatorID = "1310";
					queryString = prof + authSpecQual;
					break;
				}
                case "Maine Board of Licensure in Medicine":   // Medicine
                {
                    regulatorID = "376";
                    queryString = prof + authSpecQual;
                    break;
                }
                case "Maine State Board of Alcohol & Drug Counselors": // Alcohol and Drug Couselor
				{
					regulatorID = "4350";
					queryString = prof;
					break;
				}
                case "Maine State Board of Boilers and Pressure Vessels": // Boilers and Pressure Vessels
				{
					regulatorID = "4520";
					queryString = prof + authSpecQual;
					break;
				}
                case "Maine State Board of Chiropractic": // Chiropractic
				{
					regulatorID = "4180";
					queryString = prof;
					break;
				}
                case "Maine State Board of Complementary Health Care": // Complementary Health Care
				{
					regulatorID = "4450";
					break;
				}
                case "Maine State Board of Counseling Professionals": // Counseling Professionals
				{
					regulatorID = "4646";
					break;
				}
                case "Maine State Board of Dental Examiners": // Dental Examiners
				{
					regulatorID = "384";
					queryString = prof + authSpecQual;
					break;
				}
                case "Maine State Board of Dietetic Practice": // Dietetic Practice 
				{
					regulatorID = "4240";
					break;
				}
                case "Maine State Board of Physical Therapy": // Physical Therapy
				{
					regulatorID = "4390";
					break;
				}
                case "Maine State Board of Psychology": // Psychologists
				{
					regulatorID = "4410";
					queryString = prof;
					break;
				}
                case "Maine State Board of Occupational Therapy": // Occupational Therapy
				{
					regulatorID = "4440";
					break;
				}
                case "Maine State Board of Optometry": // Optometry
				{
					regulatorID = "385";
					queryString = authSpecQual;
					break;
				}
                case "Maine State Board of Pharmacy": // Pharmacy
				{
					regulatorID = "4380";
					queryString = prof;
					break;
				}
                case "Maine State Board of Podiatric Medicine": // Podiatric Medicine
				{
					regulatorID = "4400";
					break;
				}
                case "Maine State Board of Respiratory Care": // Respiratory Care Practitioners
				{
					regulatorID = "4260";
					break;
				}
                case "Maine State Board of Social Workers": // Social Workers
				{
					regulatorID = "4420";
					break;
				}
                case "Maine State Board of Speech Audiology and Hearing": // Speech, Audiology and Hearing
				{
					regulatorID = "4170";
					break;
				}
                case "Maine State Board of Electricians": // Electricians
				{
					regulatorID = "4220";
					queryString = prof + authSpecQual;
					break;
				}
                case "Maine State Board of Massage Therapy": // Massage Therapy
				{
					regulatorID = "4078";
					break;
				}
                case "Maine State Board of Plumbers": // Plumbers
				{
					regulatorID = "4460";
					queryString = prof;
					break;
				}
                case "Maine State Board of Radiologic Technology": // Radiologic Technology
				{
					regulatorID = "4430";
					queryString = authSpecQual;
					break;
				}
                case "Maine Registry of Certified Nurse Assistants": // Registry of Certified Nurse Assistants (CNA and Direct Care Registry)
                {
                    regulatorID = "6719";
                    break;
                }
                case "Maine Board of Osteopathic Licensure": //  Osteopathic Licensure 
                {
                    regulatorID = "383";
                    break;
                }
                case "Maine State Board of Fuel": // Fuel
                {
                    regulatorID = "4320"; 
                    break;
                }
                case "Maine State Board of Nursing": // Nursing
                {
                    regulatorID = "1310";
                    break;
                }
                case "Maine State Board of Nursing Home Administrators":
                    regulatorID = "4290";
                    break;
				default:
				{
					return ErrorMsg.SiteUnavailabe;
				}
			}

		    if (regulatorID != "376")
		    {
		        //Check if license number valid
		        m = Regex.Match(lic_no, "(?<PREF>[a-z]+?)(?<SUFF>[0-9]+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
		        if (!m.Success)
		            return ErrorMsg.InvalidLicense;
		    }


		    string result;
			string detailResult = "";
			bool sancResult = false;
			string expDate = "";
			HttpWebResponse resp = null;
			HttpWebRequest req = null;
            string url = "https://www.pfr.maine.gov/almsonline/almsquery/SearchIndividual.aspx?Board=" + regulatorID;

            string mainPage = "";
		    CookieCollection cookies = new CookieCollection();


            // Get SearchIndividual.aspx page and cookies
            mainPage = WebFetch.ProcessGetRequest2(ref req, ref resp, url);
            if (WebFetch.IsError(mainPage))
            {
                try
                {
                    System.IO.File.WriteAllText(@"C:\EchoPSVServer\DPFRLog\Log.txt", DateTime.Now.ToString() + " - " + resp.StatusCode.ToString() + "\r\n");
                }
                catch (Exception ex)
                {

                }
                System.Threading.Thread.Sleep(180000);
                resp = null;
                req = null;
                mainPage = WebFetch.ProcessGetRequest2(ref req, ref resp, url);
                if (WebFetch.IsError(mainPage))
                {
                    return ErrorMsg.CannotAccessSite;
                }
            }
		    cookies = resp.Cookies;


			string viewStateInputs = getViewstateInputs(mainPage);	
			string theEventValidation = getEVENTVALIDATION(mainPage);
			
			queryString += "__LASTFOCUS="
				+ "&__EVENTTARGET=" + getElementValueByName(mainPage, "__EVENTTARGET")
				+ "&__EVENTARGUMENT="
				+ viewStateInputs
				+ "&__EVENTVALIDATION=" + theEventValidation
                + "&ctl00%24ctl00%24mainContent%24mainContent%24scDepartment="
                + "&ctl00%24ctl00%24mainContent%24mainContent%24scAgency="
                + "&ctl00%24ctl00%24mainContent%24mainContent%24scRegulator=" + regulatorID
                + "&ctl00%24ctl00%24mainContent%24mainContent%24scLastName="
                + "&ctl00%24ctl00%24mainContent%24mainContent%24scFirstName="
                + "&ctl00%24ctl00%24mainContent%24mainContent%24scLicenseNo=" + lic_no
                + "&ctl00%24ctl00%24mainContent%24mainContent%24iShowAdditionalOptions=false"
				+ "&ctl00%24ctl00%24mainContent%24mainContent%24iAdditionalOptionsSaved=false"
                + "&ctl00%24ctl00%24mainContent%24mainContent%24scCountry="
                + "&ctl00%24ctl00%24mainContent%24mainContent%24scCity="
                + "&ctl00%24ctl00%24mainContent%24mainContent%24scState="
                + "&ctl00%24ctl00%24mainContent%24mainContent%24scCounty="
                + "&ctl00%24ctl00%24mainContent%24mainContent%24scZip="
                + "&ctl00%24ctl00%24mainContent%24mainContent%24btnSearch=Search";

			result = ProcessPostRequest2(ref req, ref resp, url, queryString, url, null, cookies, null);
			if (!WebFetch.IsError(result))
			{
				result = WebFetch.ProcessPostResponse(ref resp, ref req);
			}
			else
				return ErrorMsg.CannotAccessSearchResultsPage;

			// Parse search results
			if (Regex.IsMatch(result,("No records found"), RegexOptions.Singleline | RegexOptions.IgnoreCase))
			{
                return ErrorMsg.NoResultsFound;
			}

            string pattern = @"Search Result.*?<p class=""topcenter"">(?<RECCOUNT>.*?)records found.*?</p>";
			m = Regex.Match(result, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

			if (!m.Success)
			{
				return ErrorMsg.CannotAccessSearchResultsPage;
			}

			//Find the count of returned records
			string recCount = m.Groups["RECCOUNT"].ToString().Trim();
		
			if(recCount != "1") 
			{
				return ErrorMsg.MultipleProvidersFound;
			}

			//result contains html of summary results
			//find the link which will give details about this provider
			pattern = @"TOKEN=(?<TOKENKEY>[A-Za-z0-9]+)\"">";

			m = Regex.Match(result, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

			if (!m.Success)
			{
				return ErrorMsg.CannotAccessDetailsPage;
			}

			//Create the details page url
			string tokenKey = m.Groups["TOKENKEY"].ToString().Trim();

            string detailUrl = "https://www.pfr.maine.gov/almsonline/almsquery/ShowDetail.aspx?TOKEN=" + tokenKey;
			
			// Get results of detail page ... It is a GET request
			result = WebFetch.ProcessGetRequest(detailUrl);
			if (WebFetch.IsError(result))
				return ErrorMsg.CannotAccessDetailsPage;

            result = Regex.Replace(result, "<img src=\"images/state_seal_bw.gif\"", "<img src=\"https://www.pfr.maine.gov/ALMSOnline/ALMSQuery/images/state_seal_bw.gif\"", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            pdf.Html = result;
            //pdf.PreProcessLinks(new string[] { @"([^'])+\.jpg" }, @"https://pfr.informe.org"); 
            pdf.ConvertToImage("https://www.pfr.maine.gov"); 

			// ****************************************************************************************************
			// PARSE DETAIL PAGE AND SEND TO OUTGOING TABLE
			// ****************************************************************************************************


            string patternFullName = @"<h2 class=""Name"">(?<FULLNAME>.*?)</h2>";
			m = Regex.Match(result, patternFullName, RegexOptions.IgnoreCase | RegexOptions.Singleline);
			if(m.Success) 
			{
				detailResult = AddTDpair("Name", m.Groups["FULLNAME"].ToString().Trim());
				result = result.Substring(m.Index + m.Length);
			}
            
            // Get notices
            string noteTblPattern = @"Notices\s*?</h3>.*?(?<NOTICES><thead[^>]*?>.*?((Detail\s*?Additional Text\s*?)|Detailed Information).*?</table>)";
            Match noticeM = Regex.Match(result, noteTblPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (noticeM.Success)
            {
                string noticeString = noticeM.Groups["NOTICES"].ToString();
                detailResult += AddTDpair("Notices", " ");
                string noticeSection = addTable(noticeString);
                if (Regex.Match(noticeSection, @"board\s+action", RegexOptions.Singleline | RegexOptions.IgnoreCase).Success)
                    sancResult = true;
                detailResult += noticeSection;
            }
           
			// Get license details and general information
			// There could be multiple licenses listed for the same provider, but with different license numbers
			// Should show all of them, but only record sanctions and expiration date of input lic_no
            string licPattern = @"<h2>(?<PROFESSION>.*?)</h2>(?<ATTR>.*?(</div>|/>))\s*</div>\s*</div>\s*<div(?<SECTIONS>.*?)(?=<h2>)";
            string noLicPattern = @"<h2 class=""regulatorName.*?>.*?</h2>.*?</div>.*?<h2>(?<ATTR>.*?(</div>|/>))\s*</div>\s*</div>\s*<div(?<SECTIONS>.*?)(?=<h2>)";
			string generalPattern = @"<h2>GENERAL INFO.*?</h2>(?<ATTR>.*?)((?=<h3 class=""SectionHeader)(?<SECTIONS><h3 class=""SectionHeader.*?)|(?<SECTIONS>))</div>\s*(<div>)?<p";

			MatchCollection genColl = Regex.Matches(result, generalPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
			if (genColl.Count > 0)
				result = result.Substring(0, genColl[0].Index) + "<h2>";

			MatchCollection licColl = Regex.Matches(result, licPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
			if(licColl.Count == 0)
				licColl  = Regex.Matches(result, noLicPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);


			detailResult += addSection(licColl, "ATTR", "SECTIONS", lic_no, ref expDate, ref sancResult, true, regulatorID);
			detailResult += addSection(genColl, "ATTR", "SECTIONS", "", ref expDate, ref sancResult, false, regulatorID);
			
			detailResult = Regex.Replace(detailResult, "&amp;", "&", RegexOptions.Singleline | RegexOptions.IgnoreCase);

			// Handle expirables and sanctions
			if(this.expirable)
				checkExpirableDPFR(expDate, dr["lic_exp"].ToString().Trim());
			setSanction(sancResult);
			
			return detailResult;
		}
		

		protected string getVIEWSTATE(string htmlpage)
		{
			string theViewState = "";
			string pattern = "name=\"__VIEWSTATE\".*?value=\"(?<VSTATE>.*?)\"";
			Match m1 = Regex.Match(htmlpage, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
			if (m1.Success)
				theViewState = m1.Groups["VSTATE"].ToString();
			
			return HttpUtility.UrlEncode(theViewState);
		}

		protected string getEVENTVALIDATION(string htmlpage)
		{
			string theEventValidation = "";
			string pattern = "name=\"__EVENTVALIDATION.*?value=\"(?<EVTVALID>.*?)\"";
			Match m1 = Regex.Match(htmlpage, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
			if (m1.Success)
				theEventValidation = m1.Groups["EVTVALID"].ToString();
			
			return HttpUtility.UrlEncode(theEventValidation);
		}

		public static string ProcessPostRequest2(ref HttpWebRequest req, 
			ref HttpWebResponse resp,
			string url, string post, string referer, NetworkCredential ntwrkCred, 
			CookieCollection cookieColl, WebHeaderCollection headerColl) 
		{
			string result = "";
			StreamWriter sw = null;
			// setup request			
			req = (HttpWebRequest)WebRequest.Create(url);
			req.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.0;)";
			req.CookieContainer = new CookieContainer();
			if(cookieColl != null)
			{
				req.CookieContainer.Add(cookieColl);
			}
			else if(resp != null)
				req.CookieContainer.Add(resp.Cookies);
			
			req.KeepAlive = true;

			
			if(referer != "")
				req.Referer = referer;

			req.KeepAlive = true;
			req.AllowAutoRedirect = true;
			req.KeepAlive = true;
			req.Method = "POST";
			req.ContentLength = post.Length;
			long x = req.ContentLength;
			req.ContentType = "application/x-www-form-urlencoded";
			req.ServicePoint.Expect100Continue = false;

			req.Accept = "text/html, application/xhtml+xml, */*";
			req.AllowAutoRedirect = true;

			// do post
			try 
			{
				Stream tmpStr = req.GetRequestStream();
				sw = new StreamWriter(tmpStr);
				sw.Write(post);
			}
			catch(WebException we) 
			{
				result =  "\n\nException = " 
					+ "\nException = " + we.Message.ToString()
					+ "\nStatus = " + we.Status.ToString()
					+ "\nResponse = " +we.Response.ToString();
			}
			catch(Exception e) 
			{
				result = ErrorMsg.CustomException(e);
			}
			finally 
			{
				if(sw != null) sw.Close();
			}
			return result;
		}
			
		private string addSection(MatchCollection coll, string ATTR, string SECTIONS, string lic_no, ref string expDate, ref bool sancResult, bool isLicense, string regulator)
		{
			string tResults = "";
			string secName = "";
			string secText = "";
			string tableData = "";
					
			bool isInputLicense;

            string attrPattern = @"class=""attributeCell.*?>\s*<div>(?<ATTRLABEL>.*?)</div>\s*</div>\s*<.*?class=""attributeCell.*?(>(<a href.*?>)?(?<ATTRVALUE>.*?)(</a>)?</div>|[^<]*?(?<ATTRVALUE>)/>)";
            string sectionPattern = @"<h3 class=""SectionHeader.*?>(?<SECNAME>.*?)</h3.*?(class=""SectionText.*?>(?<SECTEXT>.*?)</p>.*?|(?<SECTEXT>))</caption>(?<TABLEDATA>.*?)</table>\s*</div>"; 
	
			foreach(Match m in coll) 
			{
				isInputLicense = false;

				if(!isLicense)
					tResults += AddTDpair("GENERAL INFORMATION:", "");
				else if (m.Groups["PROFESSION"].ToString().Trim() != "")
					tResults += AddTDpair("PROFESSION", m.Groups["PROFESSION"].ToString().Trim());

				tResults += addAttributes(m.Groups[ATTR].ToString(), attrPattern, "ATTRLABEL", "ATTRVALUE", lic_no, ref expDate, ref sancResult, ref isInputLicense, isLicense, regulator);

				// Get license sections information
				string sectionsHTML = m.Groups[SECTIONS].ToString();
				MatchCollection secColl = Regex.Matches(sectionsHTML, sectionPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

				foreach(Match mSec in secColl)
				{
					secName = mSec.Groups["SECNAME"].ToString().Trim() + ":";
					secText = mSec.Groups["SECTEXT"].ToString().Trim();
					// Remove hyperlinks from section text
					secText = Regex.Replace(secText, "<a href.*?>", "");
					secText = Regex.Replace(secText, "</a>", ""); 
					if (secText != "")
						secText = "(" + secText + ")";
					tResults += AddTDpair(secName, secText);
					tableData = mSec.Groups["TABLEDATA"].ToString();

					if (lic_no == "") //General Information
					{
						tableData = Regex.Replace(tableData, "<div[^>]*?>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);
						tableData = Regex.Replace(tableData, "</div>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);
					}
					tResults += addTable(tableData);

					// If input lic_no is this record, store sanction information
					if(isInputLicense && secName.IndexOf("Disciplin") > -1)
					{
						if(mSec.Groups["TABLEDATA"].ToString().ToUpper().IndexOf("NONE") < 0)
							sancResult = true;
					}
					if(isInputLicense && secName.IndexOf("Suspension") > -1)
					{
						if(mSec.Groups["TABLEDATA"].ToString().ToUpper().IndexOf("NONE") < 0)
							sancResult = true;
					}
				}
			}
			return tResults;
		}
	
		private string addAttributes(string htmlData, string pattern, string keyGroup, string valueGroup, string lic_no, ref string expDate, ref bool sancResult, ref bool isInputLicense, bool setLicNo, string regulator)
		{
			string tResults = "";
			string key;
			string val;

			Match m = Regex.Match(htmlData, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

			while(m.Success)
			{
				key = m.Groups[keyGroup].ToString().Trim();
				key = Regex.Replace(key, @"\s+", " ");
				key = Regex.Replace(key, @"\s+:", ":");
				val = m.Groups[valueGroup].ToString().Trim();
				val = Regex.Replace(val, @"\s+", " ");
				tResults += AddTDpair(key, val);
				m = m.NextMatch();

				// If input lic_no matches current license record, sanction and expiration information can be stored
				// do not store this information for other licenses or general discipline indicator
				if (!setLicNo)
					continue;
                
			    if(key.IndexOf("Number") > -1)
                {
                    if (regulator == "376")
			        {
			            lic_no = Regex.Replace(lic_no.ToUpper(), @"^[A-Za-z0]+", "",RegexOptions.IgnoreCase | RegexOptions.Singleline);
			            val = Regex.Replace(val.ToUpper(), @"^[A-Za-z0]+", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			        }
                    if (lic_no.ToUpper() == val.ToUpper())
					    isInputLicense = true;
                }
				if(isInputLicense)
				{
					if(Regex.Match(key, @"Expir[^<>]*?Date", RegexOptions.Singleline | RegexOptions.IgnoreCase).Success)
						expDate = val;	
					if(key.IndexOf("Status")>-1)
					{
						val = val.ToUpper();
						if(val.IndexOf("CANCEL")>-1 
							|| val.IndexOf("DENIED")>-1 
							|| val.IndexOf("INACT")>-1 
							|| val.IndexOf("REVOKE")>-1 
							|| val.IndexOf("SUSPEND")>-1 
							|| val.IndexOf("SUSPENDED FOR NON-RENEWAL")>-1 
							|| val.IndexOf("VOLUNTARY SURR")>-1 )
							sancResult = true;
					}
				}
			}
			return tResults;
		}
	
		private string addTable(string tableData)
		{
			string tResults = "";
			string key;
			string val;
			string theadPattern = @"<thead.*?>\s*<tr>(?<THEADPT>.*?)</tr>\s*</thead>";
            string tbodyPattern = @"<tbody.*?>(?<TBODYPT>.*?)</tbody>";
			string theadData = "";
			string tbodyData = "";

			Match mTHEAD = Regex.Match(tableData, theadPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
			Match mTBODY = Regex.Match(tableData, tbodyPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

			if(mTHEAD.Success)
				theadData = mTHEAD.Groups["THEADPT"].ToString();
			if(mTBODY.Success)
				tbodyData = mTBODY.Groups["TBODYPT"].ToString();

			string headerPattern = "<th.*?>(<a href.*?>)?(?<thDATA>.*?)(</a>)?</th>";
			string trPattern = "<tr.*?>(<a href.*?>)?(?<trDATA>.*?)(</a>)?</tr>";
			string tdPattern = "<td.*?(?<COLSPAN>colspan.*?)?(>(?<tdDATA>.*?)</td>|[^<]*?(?<tdDATA>)/>)";
			string divPattern = @"class=""AttributeCell.*?>(<a href.*?>)?(?<ATTRLABEL>.*?)(</a>)?</div>.*?class=""AttributeCell.*?(>(?<ATTRVALUE>.*?)</div>|[^<]*?(?<ATTRVALUE>)/>)";

			string hyperlinkOpenPattern = "<a href.*?>"; 
			string hyperlinkClosePattern = "</a>"; 

			MatchCollection headColl = Regex.Matches(theadData, headerPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
			int totLbl = headColl.Count;
			int lblNum;

			Match mTR = Regex.Match(tbodyData, trPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
			while(mTR.Success)
			{
				lblNum = 0;

				Match mTD = Regex.Match(mTR.Groups["trDATA"].ToString(), tdPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
				while(mTD.Success)
				{
					if(lblNum >= totLbl)
						lblNum = 0;
					
					// See if div tags exist
					Match mDIV = Regex.Match(mTD.Groups["tdDATA"].ToString(), divPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
					if(!mDIV.Success) // go through non-div-containing td tags, add them to results table with corresponding headers
					{
						if(headColl.Count == 0)
							key = "-";
						else
						{
							key = headColl[lblNum].Groups["thDATA"].ToString().Trim();
							key = Regex.Replace(key, @"\s+", " ");
							key = Regex.Replace(key, @"\s+:", ":");
						}
						val = mTD.Groups["tdDATA"].ToString().Trim();
						if(Regex.Match(val, hyperlinkOpenPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline).Success)
						{
							val = Regex.Replace(val, hyperlinkOpenPattern, "");
							val = Regex.Replace(val, hyperlinkClosePattern, "");
							val += "  (Additional details on website)";
						}
						val = Regex.Replace(val, @"\s+", " ");

						tResults += AddTDpair(key, val);
						lblNum++;
						mTD = mTD.NextMatch();
						continue;
					}
					// else, go through div tags and add to results table
					while(mDIV.Success)
					{
						key = mDIV.Groups["ATTRLABEL"].ToString().Trim();
						key = Regex.Replace(key, @"\s+", " ");
						key = Regex.Replace(key, @"\s+:", ":");
						
						val = mDIV.Groups["ATTRVALUE"].ToString().Trim();
						if(Regex.Match(val, hyperlinkOpenPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline).Success)
						{
							val = Regex.Replace(val, hyperlinkOpenPattern, "");
							val = Regex.Replace(val, hyperlinkClosePattern, "");
							val += "  (Additional details on website)";
						}
						val = Regex.Replace(val, @"\s+", " ");

						tResults += AddTDpair(key, val);

						mDIV = mDIV.NextMatch();
					}
					mTD = mTD.NextMatch();
				}

				mTR = mTR.NextMatch();
			}

			return tResults;
		}

		protected string getElementValueByName(string htmlpage, string elemName)
		{
			//$MT - Get ViewState and Action values from response
			string theElemVal = "";
			string pattern = "name\\s*=\\s*((\"" +elemName+ "\")|('" +elemName+ "')|(" +elemName+ "))\\s+[^>]*value\\s*=\\s*((\"(?<ELEMVAL>.*?)\")|('(?<ELEMVAL>.*?)')|((?<ELEMVAL>[^>]*?)))\\s*((/\\s*>)|>)";
			Match m1 = Regex.Match(htmlpage, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
			if (m1.Success)
				theElemVal = m1.Groups["ELEMVAL"].ToString();
			
			return HttpUtility.UrlEncode(theElemVal);
		}

		protected string getViewstateInputs(string htmlpage)
		{

			string post = "";
			MatchCollection mc = Regex.Matches(htmlpage, @"<input[^>]*?name\s*=\s*""(?<ELEMNAME>__VIEWSTATE[^""]*?)""[^>]*?value\s*=\s*""(?<ELEMVAL>[^""]*?)""[^>]*?>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
			foreach (Match m in mc)
			{
				post += "&" + m.Groups["ELEMNAME"].ToString() + "=" + HttpUtility.UrlEncode(m.Groups["ELEMVAL"].ToString());
			}
			return post;
		}

		protected void checkExpirableDPFR(string expirationDate, string dr_lic_exp)
		{	
			DateTime pg_expDT;	//this stores exp. date from the detail results page
			try
			{
				pg_expDT = DateTime.Parse(expirationDate);
				try
				{
					DateTime cli_expDT = DateTime.Parse(dr_lic_exp);
					checkExpirable(cli_expDT, pg_expDT);
				}
				catch (Exception e1)
				{	//fromClient dateTime is blank/invalid, so send some really old fake value in its place
					checkExpirable((new DateTime(1492, 1, 1)), pg_expDT);
				}
			}
			catch (Exception e2) //fromSite dateTime is blank/invalid; do nothing
			{
				string msg = ErrorMsg.CustomException(e2);
			}
		}

		private string AddTDpair(string key, string val)
		{
			key = Regex.Replace(key, "<.*?>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			val = Regex.Replace(val, "<.*?>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			if (key == "")
				return "<tr><td>" + val + "</td><td></td></tr>";
			return "<tr><td>" + key + "</td><td>" + val + "</td></tr>"; 
		}

		public class AcceptAllCertificatePolicy : ICertificatePolicy
		{
			public AcceptAllCertificatePolicy()
			{
				// Nothing to do.
			}

			public bool CheckValidationResult(ServicePoint srvPoint,
				X509Certificate certificate, WebRequest request,
				int certificateProblem)
			{
				string hexstring = certificateProblem.ToString("X");
				//hexstring == CERT_E_UNTRUSTEDROOT == 0x800B0109 == -2146762487 
				
				// Just accept
				return true;
 
			}
		}
	}
}
