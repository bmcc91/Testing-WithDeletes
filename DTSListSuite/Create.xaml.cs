﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Net;
using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace DTSListSuite
{
    /// <summary>
    /// Interaction logic for Create.xaml
    /// </summary>
    public partial class Create : Page
    {
        public Create()
        {
            InitializeComponent();
        }
        private async void Compile_Click(object sender, RoutedEventArgs e)
        {
            /* Start compiling, will never be called multiple times at once */
            compileButton.IsEnabled = false;
            compileButton.Content = "Compiling...";
            bool check = await checkIntranet();
            // bool check = checkIntranet();
            if (check)
            {
                /* Check settings */
                string[] csvList = readData(DTSListSuite.App.mDtsListFile);
                string[] delList = readData(DTSListSuite.App.deleteListFile);
                string[] ccdPgrms = readData(DTSListSuite.App.ccdPgrmFile);
                string[] mu2Pgrms = readData(DTSListSuite.App.muPgrmFile);
                string[] changesT = readData(DTSListSuite.App.changeTFile);
                string[] changesL = readData(DTSListSuite.App.changeLFile);
                compileBar.Maximum = csvList.Length - 1;
                /* Compile and output */
                List<string[]> sheetData = await compileData(csvList, ccdPgrms, mu2Pgrms, changesT, changesL);
                outputData(sheetData, csvList, delList);
            }
            else
                MessageBox.Show("Failure to connect to Magic Web database", "Failure to Connect",
                    MessageBoxButton.OK, MessageBoxImage.Hand);
            compileButton.IsEnabled = true;
            compileButton.Content = "Compile";
        }



        private async Task<bool> checkIntranet()
        {
            /* Sorta slow but whatever */
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(DTSListSuite.App.magicWebPath);
            request.Proxy = null;   /* Prevents proxy auto-detect, slightly faster */
            request.AllowAutoRedirect = false;
            request.Method = "HEAD";
            try
            {
                WebResponse response = await request.GetResponseAsync();
            }
            catch (WebException)
            {
                return (false);
            }
            return (true);
        }


        private string[] readData(string input)
        {
            string[] dtss = File.ReadAllLines(input);
            return (dtss);
        }


        private string ParseForDeletes(string deletion)
        {
            string DtsConc = "";
            TextFieldParser parser = new TextFieldParser(new StringReader(deletion));
            parser.HasFieldsEnclosedInQuotes = true;
            parser.SetDelimiters("\t");
            string[] dtsArray = { "" };
            while (!parser.EndOfData)
                dtsArray = parser.ReadFields();
            // Find DTS number
            string dtsNum = dtsArray[3].Substring(dtsArray[3].IndexOf(dtsArray[2]) + dtsArray[2].Length + 1, dtsArray[3].Length - dtsArray[3].IndexOf(dtsArray[2]) - dtsArray[2].Length - 1);
            dtsNum = dtsNum.Substring(0, dtsNum.IndexOf("."));
            // Product + Appl + DTS Number
            DtsConc = dtsArray[1] + dtsArray[2] + dtsNum;
            return DtsConc;
        }

        private async Task<List<string[]>> compileData(string[] dtsList, string[] ccdPgrms, string[] mu2Pgrms,
            string[] changesT, string[] changesL)
        {
            List<string[]> returnList = new List<string[]> { };
            int i = 0;

            foreach (string dts in dtsList)
            {
                if (dts.Substring(0, 3) != "C/R")
                {
                    i++;
                    WebClient client = new WebClient();
                    client.DownloadStringCompleted += (s, e) =>
                    {
                        dtsSlot temp = new dtsSlot(e.Result, ccdPgrms, mu2Pgrms, changesT, changesL);
                        compileBar.Value = i;
                        /* Add to output list */
                        returnList.Add(temp.results);
                    };
                    //   string[] dtsArray = dts.Split('\t');
                    TextFieldParser parser = new TextFieldParser(new StringReader(dts));
                    parser.HasFieldsEnclosedInQuotes = true;
                    parser.SetDelimiters(",");
                    string[] dtsArray = { "" };
                    while (!parser.EndOfData)
                        dtsArray = parser.ReadFields();
                    var url = string.Format(DTSListSuite.App.magicWebPath + "dts/REQUESTS/{0}/{1}/{2}.htm",
                        dtsArray[1], dtsArray[2], dtsArray[3]);
                    await client.DownloadStringTaskAsync(new Uri(url));
                }
            }
            return (returnList);
        }

        

        private void outputData(List<string[]> sheetData, string[] dtsList, string[] delList)
        {
            int i = 1;
            List<string> printList = new List<string>();
            List<string> printRetire = new List<string>();

            List<string> dtsListToDel = new List<string>();
            foreach (string deletion in delList)
            {
                // Create list of DTS's to be deleted
                dtsListToDel.Add(ParseForDeletes(deletion));
            }
            
            foreach (string[] item in sheetData)
            {
                string dtsNum = item[3].Substring(item[3].IndexOf(item[2]) + item[2].Length + 1, item[3].Length - item[3].IndexOf(item[2]) - item[2].Length - 1);
                dtsNum = dtsNum.Substring(0, dtsNum.IndexOf("."));
                // Compare current DTS to delete list. If the current DTS list is in the delete list,
                // do not all it to the output files
                if (!dtsListToDel.Contains(item[1] + item[2] + dtsNum))
                {
                    if(item[23]=="D")
                        printRetire.Add(createList(item, dtsList, i));  /* Possible Delete List */
                    
                    // Always add to Master List
                    printList.Add(createList(item, dtsList, i));  /* Master List */
                    
                }
                i++;
            }
            /* File to appropriate list */
            File.WriteAllLines(DTSListSuite.App.outputFile, printList);
            File.WriteAllLines(DTSListSuite.App.retireFile, printRetire);
        }

        private string createList(string[] item, string[] dtsList, int i)
        {
            /* Start Temp Replacements */
            TextFieldParser parser = new TextFieldParser(new StringReader(dtsList[i]));
            parser.HasFieldsEnclosedInQuotes = true;
            parser.SetDelimiters(",");
            string[] tempLIST = { "" };
            while (!parser.EndOfData)
                tempLIST = parser.ReadFields();
            //temp
            // Pull C/R, Resp App, and Comments directly from csv
            item[0] = tempLIST[0];
            item[20] = tempLIST[20];
            item[21] = tempLIST[21];
            item[22] = tempLIST[22];
            if (item[5] == "Submitted")
                item[10] = tempLIST[10];
            if (item[1] == "FOC")
            {
                item[15] = tempLIST[15];
                item[16] = tempLIST[16];
            }
            if ((item[15] == "Y") || (item[16] == "Y"))
                item[14] = "";
            item[17] = tempLIST[17];
            if (tempLIST[18] == "Y")
                item[18] = tempLIST[18];
            if (tempLIST[19] == "Y")
                item[19] = tempLIST[19];
            /* End Temp Replacements */
            string tempItem = string.Join("\t", item);
            return (tempItem);
        }

    }


    public class dtsSlot
    {
        public string[] results = new string[24];

        public dtsSlot(string dtsHTML, string[] ccd, string[] mu2, string[] changesT, string[] changesL)
        {
            string date = DeleteDate(dtsHTML);
            
            
            DateTime dateTime = DateTime.Today;
            string[] basics = setupBasicInfo(dtsHTML);
        //    string dtsString = basics[0] + basics[1] + ;
    //        if(!delList.Contains(s)
            results[0] = ""; /* C/R */
            results[1] = basics[0];
            results[2] = basics[1];
            results[3] = basics[2];
            results[4] = description(dtsHTML);
            results[5] = status(dtsHTML);
            results[6] = priority(dtsHTML);
            results[7] = associated(dtsHTML);
            results[8] = release(dtsHTML);
            results[9] = ppack(dtsHTML);
            results[10] = program(dtsHTML);
            results[11] = dateTime.ToString("MM/dd/yyyy");
            results[12] = change(dtsHTML);
            results[13] = duedate(dtsHTML);
            results[14] = pushShip(dtsHTML, "6.07C", results[9]);
            results[15] = shipRing(results[12], changesT);
            results[16] = shipRing(results[12], changesL);
            results[17] = "";
            results[18] = compareList(ccd, results[10], dtsHTML);
            if (results[18] != "Y")
                results[19] = compareList(mu2, results[10], dtsHTML);
            results[20] = "";
            results[21] = "";
            results[22] = "";

            string MostRecent = "";
            if (results[5].IndexOf("Rejected")!=-1 || results[5].IndexOf("Reclass")!=-1)
                results[23] = "D";
            else if (results[5].IndexOf("Completed")!=-1)
            {
                MostRecent = DeleteDate(dtsHTML); // Returns most recent date

                string today = DateTime.Now.ToString("MM/dd/yyyy h:mm tt"); // Today's date and time
                today = today.Substring(0, 6) + today.Substring(8, 2); // Just today's date
                int yr = Convert.ToInt32(today.Substring(6, 2)) - 2; // Current year - 2
                string CompareDate = today.Substring(0, 6) + yr.ToString();  // Two years before today

                string year = CompareDateString(MostRecent.Substring(6, 2), CompareDate.Substring(6, 2));
                string month = CompareDateString(MostRecent.Substring(0, 2), CompareDate.Substring(0, 2));
                string day = CompareDateString(MostRecent.Substring(3, 2), CompareDate.Substring(3, 2));
                if (year != "")
                {
                    if (year == "C")
                        results[23] = "D";
                    else
                        results[23] = "";
                }
                else if (month != "")
                {
                    if (month == "C")
                        results[23] = "D";
                    else
                        results[23] = "";
                }
                else if (day != "")
                {
                    if (month == "C")
                        results[23] = "D";
                    else
                        results[23] = "";
                }
                else
                    results[23] = "";
            }
            else // If not complete, rejected, or recleassed, 
                results[23] = "";
            
        }

        private string DeleteDate(string dtsHTML)
        {
            /* Lots of magic numbers...will fix later */
            /* I hate parsing raw javascript */
            int i = dtsHTML.IndexOf("http://magicweb/dts/pipelines/");
            string current = dtsHTML;
            string date = "";
            DateTime today = DateTime.Now;
            while(i!=-1)
            {
                current = current.Substring(i,current.Length-i);
                // first data tag: status
                current = current.Substring(current.IndexOf("data") + 12, current.Length - current.IndexOf("data") - 12);
                // second data tag: date
                int j = current.IndexOf("data");
                if (j != -1)
                {

                    string datadate = current.Substring(j + 6, 8);
                    if (date == "")
                        date = datadate;
                    else
                    {
                        string year = CompareDateString(date.Substring(6,2),datadate.Substring(6,2));
                        string month = CompareDateString(date.Substring(0, 2), datadate.Substring(0, 2));
                        string day = CompareDateString(date.Substring(3, 2), datadate.Substring(3, 2));
                        if(year != "")
                        {
                            if (year == "C")
                                date = datadate;
                        }
                        else if (month != "")
                        {
                            if (month == "C")
                                date = datadate;
                        }
                        else if (day !="")
                        {
                            if (day == "C")
                                date = datadate;
                        }
                    }
                    //grab date
                    // if date nil, equal date
                    //otherwise, compare
                }
                
                i = current.IndexOf("http://magicweb/dts/pipelines/");
            }

            //check vs today's date


                return (date);
        }


        private string CompareDateString(string date, string datadate)
        {
            // C for data date (change/ delete)
            // S for date (stay the same/ keep)
            int ID = Convert.ToInt32(date);
            int IDD = Convert.ToInt32(datadate);
            if (IDD > ID)
                return "C";
            else if (ID > IDD)
                return "S";
            else
                return "";
        }

        private string parseTag(string tag, string dtsHTML)
        {
            /* Many properties can be parsed by the corresponding HTML header value
             * For those that can, no other methods are needed */
            int i = dtsHTML.IndexOf(tag);
            if (i != -1)
            {
                dtsHTML = dtsHTML.Substring(i);
                i = dtsHTML.IndexOf("content") + 9;
                dtsHTML = dtsHTML.Substring(i);
                int j = dtsHTML.IndexOf("\">");
                dtsHTML = dtsHTML.Substring(0, j);
                return (dtsHTML);
            }
            else
                return ("");
        }

        private string[] setupBasicInfo(string dtsHTML)
        {
            string product = parseTag("prodln", dtsHTML);
            string application = parseTag("appl", dtsHTML);
            string dtsNum = parseTag("DTSnumber", dtsHTML);
            /* Added quotations for automatic hyperlinking in google spreadsheets */
            dtsNum = "=hyperlink(\"http://magicweb/dts/REQUESTS/" + product +
                     "/" + application + "/" + dtsNum + ".htm\"," + dtsNum + ")";
            return (new string[] { product, application, dtsNum });
        }

        private string description(string dtsHTML)
        {
            string content = parseTag("description", dtsHTML);
            if (content != "")
            {
                /* Replace HTML quotes with ASCII */
                content = content.Substring(0, content.Length - 4);
                int i = content.IndexOf("&quot;");
                while (i != -1)
                {
                    string tempA = content.Substring(0, i) + "\"";
                    string tempB = content.Substring(i + 6);
                    content = tempA + tempB;
                    i = content.IndexOf("&quot;");
                }
                return (content);
            }
            else
                return ("");
        }

        private string status(string dtsHTML)
        {
            /* Can probably clean this up later */
            bool flag = false;
            string pri = parseTag("DTSreleasesVIEW", dtsHTML);
            int i = pri.IndexOf("6.07");
            if (i == -1)
            {
                i = pri.IndexOf("6.1");
                if (i == -1)
                    return ("");
                flag = true;
                int n;
                bool isNumeric = int.TryParse(pri.Substring(i + 3, 1), out n);
                if (isNumeric)
                    i++;
                pri = pri.Substring(i + 3, 1);
            }
            else
                pri = pri.Substring(i + 4, 1);
            switch (pri)
            {
                case "S":
                    pri = "Submitted";
                    break;
                case "D":
                    pri = "Draft";
                    break;
                case "T":
                    pri = "Testing";
                    break;
                case "Q":
                    pri = "Queued";
                    break;
                case "U":
                    pri = "United Tested";
                    break;
                case "P":
                    pri = "Production";
                    break;
                case "C":
                    pri = "Completed";
                    break;
                case "R":
                    pri = "Rejected";
                    break;
                case "r":
                    pri = "Reclass";
                    break;
                case "H":
                    pri = "Holding";
                    break;
                default:
                    break;
            }
            if (flag)
                pri = "6.1-" + pri;
            return (pri);
        }

        private string priority(string dtsHTML)
        {
            return (parseTag("DTSpriority", dtsHTML));
        }

        private string associated(string dtsHTML)
        {
            int i = dtsHTML.IndexOf("<th>Req'd/Link</th>");
            if (i == -1)
                return ("");
            dtsHTML = dtsHTML.Substring(i);
            i = dtsHTML.IndexOf("data\">") + 6;
            dtsHTML = dtsHTML.Substring(i, 1);
            if (dtsHTML == "Y")
                return (dtsHTML);
            return ("");
        }

        private string release(string dtsHTML)
        {
            string content = parseTag("DTSreleasesVIEW", dtsHTML);
            if (content != "")
            {
                int i = content.IndexOf("6.");
                if (i != -1)
                {
                    content = content.Substring(i);
                    return (content);
                }
                else
                    return ("");
            }
            else
                return ("");
        }

        private string ppack(string dtsHTML)
        {
            string content = parseTag("DTSSRnumbers", dtsHTML);
            if (content != "")
            {
                int i = dtsHTML.IndexOf("6.07SR");
                if (i != -1)
                {
                    dtsHTML = dtsHTML.Substring(i + 6, 1);
                    return (dtsHTML);
                }
                else
                    return ("");
            }
            else
                return ("");
        }

        private string program(string dtsHTML)
        {
            /* Lots of magic numbers...will fix later */
            /* I hate parsing raw javascript */
            int i = dtsHTML.IndexOf("ToggleAllCompDoc") + 151;
            dtsHTML = dtsHTML.Substring(i, 2000);
            i = dtsHTML.IndexOf("mySections[");
            if (i != -1)
            {
                int j = dtsHTML.IndexOf("(i=0") - 4;
                dtsHTML = dtsHTML.Substring(0, j);
                /* At this point, dtsHTML is a list of unformatted programs only */
                string[] delims = new string[] { "\n" };
                string[] dtsList = dtsHTML.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                List<string> returnList = new List<string>();
                foreach (string item in dtsList)
                {
                    if (item.Length > 2)
                        returnList.Add(item);
                }
                dtsHTML = "";
                foreach (string cell in returnList)
                {
                    string tempCell = cell;
                    tempCell = tempCell.Substring(18);
                    tempCell = tempCell.Substring(0, tempCell.Length - 7);
                    int tempCut = tempCell.IndexOf("\"") + 1;
                    tempCell = tempCell.Substring(tempCut);
                    dtsHTML += (tempCell + ", ");
                }
                if (dtsHTML.Length > 0)
                    dtsHTML = dtsHTML.Substring(0, dtsHTML.Length - 2);
                return (dtsHTML);
            }
            else
                return ("");
        }

        private string change(string dtsHTML)
        {
            string appl = parseTag("appl", dtsHTML);
            if (appl != "")
            {
                string content = parseTag("DTSRelCh607", dtsHTML);
                if (content != "")
                {
                    content = content.Substring(2);
                    return (appl + " " + content);
                }
                else
                    return ("");
            }
            else
                return ("");
        }

        private string duedate(string dtsHTML)
        {
            string content = parseTag("DTSduedate", dtsHTML);
            if (content != "")
                return (content);
            else
                return ("");
        }

        private string pushShip(string dtsHTML, string release, string ppack)
        {
            int i = dtsHTML.IndexOf(release);
            if ((i != -1) && (ppack == ""))
                return ("Y");
            else
                return ("");
        }

        private string compareList(string[] list, string programs, string dtsHTML)
        {
            string keywords = parseTag("DTSkeywords", dtsHTML);
            foreach (string line in list)
            {
                if (programs.IndexOf(line) != -1)
                    return ("Y");
                if (keywords.IndexOf(line) != -1)
                    return ("Y");
            }
            return ("");
        }

        private string shipRing(string change, string[] changeList)
        {
            if (change == "")
                return ("");
            string appl = change.Substring(0, 3);
            change = change.Substring(4);
            foreach (string line in changeList)
            {
                string tempAppl = line.Substring(0, 3);
                if (appl == tempAppl)
                {
                    string[] tempLine = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (Convert.ToInt32(change) <= Convert.ToInt32(tempLine[1]))
                        return ("Y");
                    foreach (string item in tempLine)
                    {
                        if (change == item)
                            return ("Y");
                    }
                }
            }
            return ("");
        }
    }
}
