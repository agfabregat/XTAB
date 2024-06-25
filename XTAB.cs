
using CommonVoters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XTAB { 


    public class TabDelimitedFileConfiguration
    {
        public TabDelimitedFileConfiguration() { }
        public string State { get; set; }
        public string CurrentProcessingDate { get; set; }
        //public string Title { get; set; }
        public string VerticalKeyHead { get; set; } //"afd~asdf~"
        public string NumberOfDecimalsToUse { get; set; } //"F0"
        public int Filter_KeepRowsWithTotalsGreaterThan { get; set; }
        public string Filter_ExcludeRowIfKeyContains { get; set; }
        public bool ShowOnlyKeys { get; set; }
        public KeyLinkConfiguration KeyLinkConfiguration { get; set; }
        public string Delim { get; set; } = CommonConstants.tab;
        public string LineFeed { get; set; } = "\n";

    }

    public class KeyLinkConfiguration
    {
        public Func<string, int, bool> DisplayHyperlinkInRow { get; set; }
        public int WithThisConstantPredicate { get; set; }
        public int CompareValueInColumn { get; set; }
        public string FileNameCode { get; set; }
        public int NumberOfKeyColumnsUsedForQueryStringInVariablePartOfFileName { get; set; }
        public string Folder { get; set; }
        public bool LinkCol0 { get; set; }
    }
    public class HtmlTableConfig
    {
        public string tabFileFullPath;

        public char Delim { get; set; } = '\t';
        public char LineFeed { get; set; } = '\n';
        public int[] KeyColumnsWidths { get; set; }
        public string[] KeyColumnsAlignment { get; set; }
        public bool SortAllowed { get; set; }
        public bool DisplayCaption { get; set; }
        public bool DisplayTHead { get; set; }
        public bool DisplayTFoot { get; set; }
        public int percentRowsToCreate { get; set; } = 100;
        public KeyLinkConfiguration KeyLinkConfiguration { get; set; }
        public string[] Captions { get; set; }
        public SearchLinksConfiguration SearchLinksConfig { get; set; }

        public HtmlTableConfig()
        {
        }
    }
    public class TabDelimitedFileParts
    {
        //public List<string> PreTableDivs { get; set; }
        public string TheadRows { get; set; }
        public string TBodyRows { get; set; }
        public string TFootRow { get; set; }



        public static string MakeOneToManyKeysOnlyTabDelimitedFile(XTAB.TabDelimitedFileConfiguration tabFileConfiguration, List<string> theRows)
        {           
           
                var rtp = XTAB.CrossTab.OneToMany(tabFileConfiguration, theRows);
                var tabFilePath = SaveTabDelimitedFile(tabFileConfiguration, rtp);
                return tabFilePath;      

        }



        public static string MakeCrosstabBasedTabDelimitedFile(XTAB.TabDelimitedFileConfiguration tabFileConfiguration)
        {
            var rtp = XTAB.CrossTab.XtabTable(tabFileConfiguration);
            var tabFilePath = SaveTabDelimitedFile(tabFileConfiguration, rtp);
            return tabFilePath;
        }


        public static string SaveTabDelimitedFile(XTAB.TabDelimitedFileConfiguration tabFileConfiguration, TabDelimitedFileParts rtp)
        {

            var jsonTabDelimitedStr = JsonConvert.SerializeObject(rtp);
            jsonTabDelimitedStr = jsonTabDelimitedStr.Replace("~", tabFileConfiguration.Delim);    // this is needed to separate the key columns        
            string fullFilePath = MakeTabFileName(tabFileConfiguration, CommonConstants.tabFileExt);

            var goodResult = CommonVoters.Functions.WriteAllText(fullFilePath, jsonTabDelimitedStr);

            return goodResult ? fullFilePath : "";

        }

        public static string MakeTabFileName(TabDelimitedFileConfiguration tabFileConfiguration, string tabFileExtension)
        {
            var justFn = tabFileConfiguration.KeyLinkConfiguration.FileNameCode;
            var folder = string.IsNullOrEmpty(tabFileConfiguration.KeyLinkConfiguration.Folder) ? "" : "\\" + tabFileConfiguration.KeyLinkConfiguration.Folder;
            var fullFilePath = CommonVoters.CommonConstants.MakeFileName("REPORTS" + folder, justFn.ToLower() + tabFileExtension.ToLower()).Replace(" ", "-");
            return fullFilePath;
        }
    }

    public class CrossTab
    {
        public static Dictionary<string, Dictionary<string, double>> _xtab = new Dictionary<string, Dictionary<string, double>>();
        public static void xTabInitialize()
        {
            _xtab.Clear();
        }

        public static void XtabCountAggregator(string keyString, string colHead, double itemValue)
        {
            if (_xtab.ContainsKey(keyString))
            {
                if (_xtab[keyString].ContainsKey(colHead))
                {
                    _xtab[keyString][colHead] = _xtab[keyString][colHead] + itemValue;
                }
                else
                {
                    _xtab[keyString].Add(colHead, itemValue);
                }
            }
            else
            {
                _xtab.Add(keyString, new Dictionary<string, double>());
                _xtab[keyString].Add(colHead, itemValue);
            }

        }




        // ///////////////////////////////////////////////////////////////////////////////////////////
        // ///////////////////////////////////////////////////////////////////////////////////////////
        // ///////////////////////////////////////////////////////////////////////////////////////////
        // ///////////////////////////////////////////////////////////////////////////////////////////
        public static TabDelimitedFileParts OneToMany(TabDelimitedFileConfiguration tabFileConfiguration, List<string> theRows)
        {
            try
            {
                var rtp = new TabDelimitedFileParts();

                var tbody = new StringBuilder();
                foreach (var r in theRows)
                {
                    tbody.Append(r.ToString()).Append(tabFileConfiguration.LineFeed);
                }

                rtp.TBodyRows = tbody.ToString().Replace("~", tabFileConfiguration.Delim);

                rtp.TheadRows = tabFileConfiguration.VerticalKeyHead;
                rtp.TFootRow = "";
                return rtp;
            }
            catch 
            { 
                return null; 
            }
        }

        public static TabDelimitedFileParts XtabTable(TabDelimitedFileConfiguration tabFileConfig)
        {
            var rtp = new TabDelimitedFileParts();
            var xtabSb = new StringBuilder();
            var totColumnsInSequence = new List<string>();

            //scan entire dictionary for all the necessary totColumnsInSequence, this is the template for the sequence the totColumnsInSequence will be displayed
            //because the are rows that don't have all the totColumnsInSequence or have the totColumnsInSequence in a different sequence, 

            if (tabFileConfig.ShowOnlyKeys) { }
            else
            {
                foreach (KeyValuePair<string, Dictionary<string, double>> entry in _xtab)
                {
                    foreach (KeyValuePair<string, double> kv in entry.Value)
                    {
                        var columnExists = totColumnsInSequence.FindIndex(x => x == kv.Key);
                        if (columnExists == -1)
                        {
                            totColumnsInSequence.Add(kv.Key);
                        }
                    }
                }
                totColumnsInSequence = totColumnsInSequence.OrderBy(q => q).ToList(); //order the totColumnsInSequence by column name
            }

            var xtabOrderedKeys = _xtab.Keys.OrderBy(x => x.ToString()); //order the keys by key name
            var controlTotal = 0d;



            // //////////////////////////////////////
            // theadRow
            rtp.TheadRows = PrepareTHeadRow(tabFileConfig.VerticalKeyHead, tabFileConfig.ShowOnlyKeys, totColumnsInSequence, tabFileConfig.Delim, tabFileConfig.LineFeed);

            // ////////////////////////////////////////////////
            //rows by row this is tbody
            //
            var tuple = PrepareTBodyRows(tabFileConfig, totColumnsInSequence, xtabOrderedKeys, ref controlTotal);
            var verticalTots = tuple.Item1;
            rtp.TBodyRows = tuple.Item2;


            // ////////////////////////////////////////////////
            // verticalTots at the very bottom of the table
            rtp.TFootRow = PrepareTFootRow(tabFileConfig.NumberOfDecimalsToUse, tabFileConfig.ShowOnlyKeys, tabFileConfig.Delim, tabFileConfig.LineFeed, controlTotal, verticalTots);

            return rtp;
        }

        private static (double[], string) PrepareTBodyRows(TabDelimitedFileConfiguration tabFileConfig,
            List<string> totColumnHeadsOrdered,
            IOrderedEnumerable<string> xtabOrderedKeys, ref double controlTotal)
        {
            var verticalTots = new double[totColumnHeadsOrdered.Count];  //keeps track of the column totals as we go along
            var rowSb = new StringBuilder();

            foreach (var k in xtabOrderedKeys)
            {

                if (tabFileConfig.Filter_ExcludeRowIfKeyContains.Length > 0)
                {
                    if (!k.Contains(tabFileConfig.Filter_ExcludeRowIfKeyContains))
                    {
                        continue;  // skip this row
                    }
                }


                rowSb.Append(k).Append(tabFileConfig.Delim); //row key always displayed

                if (tabFileConfig.ShowOnlyKeys) { }
                else
                {

                    var row = new double[totColumnHeadsOrdered.Count];

                    //arrange the columuns of this row by the column template
                    foreach (KeyValuePair<string, double> kv in _xtab[k])
                    {
                        var idx = totColumnHeadsOrdered.FindIndex(x => x == kv.Key);
                        row[idx] = kv.Value;
                    }

                    var horizontaTot = 0d;

                    for (var x = 0; x < row.Length; x++)
                    {
                        var columnValue = row[x];
                        rowSb.Append(columnValue.ToString(tabFileConfig.NumberOfDecimalsToUse)).Append(tabFileConfig.Delim);
                        horizontaTot += columnValue;
                    }

                    if (tabFileConfig.Filter_KeepRowsWithTotalsGreaterThan > 0d)
                    {
                        if (horizontaTot > tabFileConfig.Filter_KeepRowsWithTotalsGreaterThan)
                        {
                            // keep row
                        }
                        else
                        {
                            //skip this row
                            continue;
                        }
                    }
                    else
                    {
                        // keep row
                    }

                    // loop again to calculate the vertical totals
                    for (var x = 0; x < row.Length; x++)
                    {
                        var columnValue = row[x];
                        verticalTots[x] = verticalTots[x] + columnValue;

                    }


                    rowSb.Append(horizontaTot.ToString(tabFileConfig.NumberOfDecimalsToUse));
                    controlTotal += horizontaTot;

                }
                rowSb.Append(tabFileConfig.LineFeed);
            }

            return (verticalTots, rowSb.ToString());
        }

        private static string PrepareTFootRow(string numberSpecifier, bool showOnlyKeys, string tab, string lf, double controlTotal, double[] verticalTots)
        {
            var sb = new StringBuilder();
            if (showOnlyKeys) { }
            else
            {
                //sb.Append("TOTAL").Append(tab); //key total column, just send the totals let the report add the total label
                var grandTotal = 0d;
                for (var v = 0; v < verticalTots.Count(); v++)
                {
                    sb.Append(verticalTots[v].ToString(numberSpecifier)).Append(tab);
                    grandTotal += verticalTots[v];
                }
                if (grandTotal == controlTotal)
                {
                    sb.Append(grandTotal.ToString(numberSpecifier)).Append(lf);
                }
                else
                {
                    throw new Exception($"Vertical [{grandTotal}] Horizontal: [{controlTotal}] don't match.");
                }
            }
            return sb.ToString();
        }

        private static string PrepareTHeadRow(string verticalKeyHead, bool showOnlyKeys, List<string> totColumnsInSequence, string tab, string lf)
        {
            var thead = new StringBuilder();

            //column Headings
            thead.Append(verticalKeyHead); //this tab is for the column taken up by the keys of each row

            if (showOnlyKeys) { }
            else
            {
                thead.Append(tab);
                foreach (var k in totColumnsInSequence)
                {
                    thead.Append(k).Append(tab);
                }
                thead.Append("TOTALS");
            }
            thead.Append(lf);
            return thead.ToString();
        }
    }

}