using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;



namespace ItemScraper
{
    class Program
    {
        static void Main(string[] args)
        {
            //these are just some special apostrophe characters that have been causing issues due to data discrepancies
            List<string> specChars = new List<string> { "&#8217;", "&#39;", "'", "’"  };


            //Returns the table from Many Sided Dice as an array of "table values" and "link values"
            List<string>[] masterRowsArray = GetTableAsync().Result;
           
            //strips out the escape characters for the various apostrophe-like 
            //marks and standardize them as ', or "&#39;" , for later filtering
            masterRowsArray[0] = SpecialCharacterSwap(masterRowsArray[0]);

            //this is a fix for known spelling errors/inconsistencies that break my 
            //functionality but aren't likely to see a fix by the publisher
            masterRowsArray[0] = stopGapSpellingFixer(masterRowsArray[0]);


            //hyperlinks need formatting to be usable for queries. 
            //This trims some prefix link info and trailing dynamic fields to just the raw hyperlink
            List<string> usableLinks = cleanHyperlinks(masterRowsArray[1]);
            
            //combines the two arrays so that the format is "link" followed by the table rows, cuts off the header
            //index 0 is first link, index 1 is first item name
            //start at 0, use every 6th to get the link, add +1 in loop to get item
            List<string> completeList = MergeList(masterRowsArray[0], masterRowsArray[1], 5);
            
            //query the url, find the node that contains the name field for the item, 
            //navigate up to the parent <Div> and take each descendent node.
            //find where the item text begins based on header and take all innertext until the next header or file end
            //some special character swapping is neccessary here as well
            List<List<string>> allItemTextLists = compileItemLists(completeList, specChars);

            string filePath = @"c:\Misc\myitems.txt";
            //prints the contents of the items from the site to a file
            printOutItemsToFile(allItemTextLists, filePath);

            Console.ReadLine();

        }




        private static List<string> stopGapSpellingFixer(List<string> items)
        {

            //there are data discrepancies that can't be rectified without the publisher interfering
            //this swaps out spelling and other errors to make the item searching feasible
            //this is only a subset of the 203 items listed
            List<string> _tempItems = new List<string>();

            for (int i = 0; i < items.Count; i++)
            {
                items[i] = items[i].Replace("Acccursed", "Accursed");
                items[i] = items[i].Replace("Aegian", "Aegiaen");
                items[i] = items[i].Replace("Sceptor", "Scepter");
                items[i] = items[i].Replace("Agate ", "Agate&nbsp;");
                items[i] = items[i].Replace("Bastard", "Bastard&nbsp;");
                items[i] = items[i].Replace("Princely", "Princely&nbsp;"); 
                items[i] = items[i].Replace("Ferranimus", "Feranimus");
                items[i] = items[i].Replace("Freell's", "Freell");
                items[i] = items[i].Replace("Kafk", "Kaft"); 
                items[i] = items[i].Replace("Korholt", "Kortholt");
                items[i] = items[i].Replace("Woodman", "Woodsman");
                items[i] = items[i].Replace("Pact Keeper", "Pactkeeper");
                items[i] = items[i].Replace("Sword-breaker", "Sword breaker");
                items[i] = items[i].Replace("Blood Jar", "The Blood Jar");
                _tempItems.Add(items[i]);
            }

            return _tempItems;
        }


        static void printOutItemsToFile(List<List<string>> itemList, string filePath)
        {


            //this will print the contents out to a texzt file in a set design
            //string filePath = @"c:\Misc\myitems.txt";
            string printoutString = "";

            for (int itemsMasters = 0; itemsMasters < itemList.Count; itemsMasters++)
            {
                printoutString =        "Name:      " + itemList[itemsMasters][0] + "\n" +
                                        "Type:      " + itemList[itemsMasters][1] + "\n" +
                                        "Utility:   " + itemList[itemsMasters][2] + "\n" +
                                        "Class A:   " + itemList[itemsMasters][3] + "\n" +
                                        "Class B:   " + itemList[itemsMasters][4] + "\n" +
                                        "Description------\n";

                for (int itemLines = 5; itemLines < itemList[itemsMasters].Count; itemLines++)
                {
                    printoutString += itemList[itemsMasters][itemLines] + "\n";
                }
                printoutString += "\n-------------------------------------------------------------\n";
                if (!File.Exists(filePath))
                {
                    string introText = "Marvelous-ish Magic Items\n";
                    File.WriteAllText(filePath, introText);
                }
                else 
                {
                    File.AppendAllText(filePath, printoutString);
                }
            }
        }

        private static List<List<string>> compileItemLists(List<string> completeList, List<string> specChars)
        {
            List<List<string>> allItemForms = new List<List<string>>();
            

            //assembles the fields in to a cohesive list for each item that can then have the text appended 
            //for easier formatting afterwards
            for (int itemRecordIndex = 0; itemRecordIndex < completeList.Count ; itemRecordIndex += 6)
            {
                List<string> _itemFields = new List<string>();
                List<string> _textList = new List<string>();
                _textList = GetNarrowItemText(completeList[itemRecordIndex], completeList[itemRecordIndex + 1], specChars).Result;
                //add the item's field info at start of a list
                _itemFields.Add(completeList[itemRecordIndex + 1]); //name
                _itemFields.Add(completeList[itemRecordIndex + 2]); //type
                _itemFields.Add(completeList[itemRecordIndex + 3]); //power
                _itemFields.Add(completeList[itemRecordIndex + 4]); //primary class
                _itemFields.Add(completeList[itemRecordIndex + 5]); //seconday class


                //add each line of the item's text to that same list of fields
                foreach (string textLine in _textList)
                {
                    _itemFields.Add(textLine);
                }

                List<string> cleanedLines = SpecialCharacterSwap(_itemFields);

                allItemForms.Add(cleanedLines);
                
            }

            return allItemForms;
        }

        private static async Task<List<string>[]> GetTableAsync()
        {
            //var url = "https://manysideddice.com/2015/03/10/a-table-of-contents-thats-better-than-nothing/";

            //this google doc is the iframe where the links are actually housed
            var url = "https://docs.google.com/spreadsheets/d/1qDcN0GszW8VZVOhuh5zqMdKPby4rZ6TA5zEipD3nsxU/pubhtml/sheet?headers=false&gid=0";

            //get the html as a string
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(url);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            List<string> tableValues = new List<string>();
            List<string> linkValues = new List<string>();

            //get inner text for all cell values
            foreach (HtmlNode nodeish in htmlDocument.DocumentNode.SelectNodes("//table[@class='waffle']/tbody/tr/td"))
            {
                string nodeText = (nodeish.InnerText);
                tableValues.Add(nodeText);
            }

            //get the hyperlink value for each row
            foreach (HtmlNode link in htmlDocument.DocumentNode.SelectNodes("//a[@href]"))
            {
                string linkText = link.Attributes["href"].Value;
                linkValues.Add(linkText);
            }

            //return the contents as an array
            List<string>[] array1 = new List<string>[2] { tableValues, linkValues };

            return array1;
        }

        public static List<List<T>> SplitList<T>(List<T> allElements, int chunk)
        {

            //takes in the original rows of the table and groups them in to 
            //arrays that can be dilimeted for better formating later.
            var list = new List<List<T>>();
            for (int i = 0; i < allElements.Count; i += chunk)
                list.Add(allElements.GetRange(i, Math.Min(chunk, allElements.Count - i)));
            return list;
        }

        public static List<string> MergeList(List<string> tableRows, List<string> linkValues, int stepCount)
        {

            //this is the new list that will be format of link, name, type, utility, class a, class b
            List<string> cleanList = new List<string>();

            int linkStep = 0;

            //go through the provided lists and add the value from the link list, 
            //then the next <stepcount> values from the rows list, repeat
            //this will add the item's hyperlink before the item info
            for (int i = 5; i < tableRows.Count; i++)
            {

                if ((i == 0 || i % stepCount == 0) && linkStep < linkValues.Count)
                {
                    cleanList.Add(linkValues[linkStep]);
                    linkStep++;
                }

                cleanList.Add(tableRows[i]);
            }

            return cleanList;

        }


        public static List<string> cleanHyperlinks(List<string> hyperlinkList)
        {
            //hyperlinks from iframe have incompatible formatting to query. They need to be cleaned up before the dom can be selected
            //somethimes this is http to https, but also strip preface that causes redirection issues
            List<string> improvedLinks = new List<string>();

            string linkMark = new string("/&");

            for (int i = 0; i < hyperlinkList.Count; i++)
            {

                if (hyperlinkList[i].StartsWith("http"))
                {
                    hyperlinkList[i] = hyperlinkList[i].Replace("https://www.google.com/url?q=", "");
                    hyperlinkList[i] = hyperlinkList[i].Replace("https", "http");
                    hyperlinkList[i] = hyperlinkList[i].Replace("http", "https");
                    hyperlinkList[i] = hyperlinkList[i].Trim('"');
                    hyperlinkList[i] = cleanEnd(hyperlinkList[i], linkMark);
                    improvedLinks.Add(hyperlinkList[i]);
                }

            }

            return improvedLinks;

        }


        public static List<string> SpecialCharacterSwap(List<string> fullTable)
        {
            //there is a massive list of special characters that need to be swapped out
            //they are seen as codes and for readability need to be swapped to the text
            //there is also a strange suffix that needs to be removed from the links
            string linkMark = new string("/&");

            for (var i = 0; i < fullTable.Count; i++)
            {

                fullTable[i] = fullTable[i].Replace("&#39;", "'");
                fullTable[i] = fullTable[i].Replace("&#8220;", "“");
                fullTable[i] = fullTable[i].Replace("&#8217;", "’");
                fullTable[i] = fullTable[i].Replace("&#8221;", "”");
                fullTable[i] = fullTable[i].Replace("&#8212;", "—");
                fullTable[i] = fullTable[i].Replace("&#8211;", "–");
                //fullTable[i] = fullTable[i].Replace("", "");
                fullTable[i] = fullTable[i].Replace("&#8230;", "…");
                fullTable[i] = fullTable[i].Replace("&#8243;", "″");
                fullTable[i] = fullTable[i].Replace("&#8216;", "‘");
                fullTable[i] = fullTable[i].Replace("&#215;", "×");
                fullTable[i] = fullTable[i].Replace("&#8242;", "′");
                
                if (fullTable[i].StartsWith("https"))
                {
                    fullTable[i] = fullTable[i].Replace("https://www.google.com/url?q=", "");
                    fullTable[i] = fullTable[i].Trim('"');
                    fullTable[i] = cleanEnd(fullTable[i], linkMark);
                }
            }

            return fullTable;
        }


        private static async Task<List<string>> GetNarrowItemText(string itemUrl, string itemName, List<string> specChars)
        {

            //navigate to page and return dom structure
            HttpClientHandler handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            var httpClient = new HttpClient(handler);

            //some links have reidrects, this handles them.
            HttpResponseMessage response = await httpClient.GetAsync(itemUrl);
            int responseCode = (int)response.StatusCode;
            if ( responseCode == 301  || responseCode == 307)
            {
                itemUrl = response.Headers.Location.ToString();
            }

            //this acquires the true pages contents
            var html = await httpClient.GetStringAsync(itemUrl);
            

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            HtmlNode startNode = null;
            string xpathSelect = "";        //will be a concatenated xpath selector
            string aposSwapItemName = "";   //holds the final 
            string finalItemName = "";      //used to find the subsection start of the item 

            //check if the itemName contains an apostrophe. This is an intermittent problem that needs checking
            //Has Apostrophe: loop through the list of special characters to guarantee a valid node is found
            //then select the parent <p> for the node that matches the text
            //if 'whitespace': truncate the item name some additionally.
            //there is also a general limit on the other items being no longer than 6 characters as there are mysteriously some
            //where the inner text is split due. this is attempting to avoid that in an artificial way.
            //no apostrophe: find the item's node and select it's ancestor <p> and save off the item name for other processing
            if ( itemName.Contains("'") )
            {
                
                foreach (string aposItem in specChars)
                {
                    
                    aposSwapItemName = itemName.Replace("'", aposItem);                  
                    xpathSelect = "//*[contains(text(),'" + aposSwapItemName + "')]/ancestor::p";
                    startNode = htmlDocument.DocumentNode.SelectSingleNode(xpathSelect);

                    if (startNode != null)
                    {
                        finalItemName = aposSwapItemName;
                        break;
                    }
                }
            }
            else if (itemName.Contains("&nbsp;") )
            {
                aposSwapItemName = itemName.Substring(0, 4);
                xpathSelect = "//*[contains(text(),'" + aposSwapItemName + "')]/ancestor::p";
                startNode = htmlDocument.DocumentNode.SelectSingleNode(xpathSelect);
                finalItemName = aposSwapItemName;
            }
            else
            {
                int end = 0;
                if(itemName.Length < 6)
                {
                    end = itemName.Length;
                }
                else
                {
                    end = 6;
                }
                aposSwapItemName = itemName.Substring(0, end);
                xpathSelect = "//*[contains(text(),'" + aposSwapItemName + "')]/ancestor::p";
                startNode = htmlDocument.DocumentNode.SelectSingleNode(xpathSelect);
                finalItemName = itemName;
               
            }
            Console.WriteLine(xpathSelect);
            Console.WriteLine(finalItemName);

            //get the parent of the node that has the item title (this is likely the div containing all text)
            HtmlNode textParent = startNode.ParentNode;


            //gets all the proper nodes from the parent <div> downwards
            var itemNodeTopDown = textParent.SelectNodes("p | ul | li");
           
            //get an index to better narrow down which parts of the dom contain the relevant text
            //which will be from a listified collection
            int id = itemNodeTopDown.ToList().FindIndex(x => x.InnerText.Contains(aposSwapItemName));

            //make a full copy of the dom as a list
            List<HtmlNode> fullListCopy = new List<HtmlNode>();
            fullListCopy = itemNodeTopDown.ToList();
            List<HtmlNode> narrowListCopy = new List<HtmlNode>();

            Console.WriteLine("id " + finalItemName + id);

            //start on the dom list at the index of the item name +1 and copy over the remaining dom
            for (int z = id + 1; z < fullListCopy.Count; z++)
            {
                narrowListCopy.Add(fullListCopy[z]);
            }

            List<string> itemSpecificText = new List<string>();

            //iterate through this shortened list to find the inner text and append to a
            //return variable. If the text has the markers of another item header or end of the dom, break
            foreach (HtmlNode node in narrowListCopy)
            {
                
                
                if (node.OuterHtml.Contains("text-decoration:underline;") || node.OuterHtml.Contains("wpcnt"))
                {
                    break;
                }
                else
                {
                    itemSpecificText.Add(node.InnerText);
                }
            }


            //clean up the innerText by putting in proper Special Characters
            itemSpecificText = SpecialCharacterSwap(itemSpecificText);

            return itemSpecificText;
            
        }

        


        public static string cleanEnd(string strSource, string strStart)
        {
            //this is used to clean some extra text on the end of the links that needs to be removed
            //for proper navigation to work
            int Start, End;

            if (strSource.Contains(strStart))
            {
                Start = strSource.IndexOf(strStart, 0);
                End = strSource.Length - 1;

                return strSource.Remove(Start);
            }
            else
            {
                return "cleanEnd Failure";
            }
        }


    }
}