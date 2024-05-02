
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XTAB
{
    public class ReportTabParts
        {
            public List<string> PreTableDivs { get; set; }
            public List<string> Captions { get; set; }
            public string TheadRows { get; set; }
            public string TBodyRows { get; set; }
            public string TFootRow { get; set; }
        }

        public class XtabFramework
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



            public class TabFileConfiguration
            {
                public TabFileConfiguration() { }
                public string State { get; set; }
                public string CurrentProcessingDate { get; set; }
                public string Title { get; set; }
                public string[] Captions { get; set; }
                public string VerticalKeyHead { get; set; } //"afd~asdf~"
                public string NumberOfDecimalsToUse { get; set; } //"F0"
                public int Filter_KeepRowsWIthTotalsGreaterThan { get; set; }
                public string Filter_ExcludeRowIfKeyContains { get; set; }
                public bool ShowOnlyKeys { get; set; }
            }

            public class HtmlTableConfig
            {
                public string tabFileFullPath;

                public HtmlTableConfig()
                {
                }

                public int[] KeyColumnsWidths { get; set; }
                public string[] KeyColumnsAlignment { get; set; }
                public bool SortAllowed { get; set; }
                public bool DisplayCaptions { get; set; }
                public bool DisplayTHead { get; set; }
                public bool DisplayTFoot { get; set; }
                public int percentRowsToCreate { get; set; }
                public bool linkRule { get; set; }
            }

            //var title = "Statewide Detail Voters With Age at Registration under 17yo";
            //var captions = CommonConstants.PrepareTableCaptions(new string[] { title });
            //var justFn = title.Replace(" ", "");
            //var verticalKeyHead = "REG~DOB~Age@Reg~Age@Last~LastVote~County~ID~FN~LN";
            //var numberOfDecimalsToUse = "F0";
            //var filer_keepRowsWithTotalsGreaterThan = 0;
            //var filter_excludeRowIfKeyContains = "";
            //var showOnlyKeys = true;




            // ///////////////////////////////////////////////////////////////////////////////////////////
            // ///////////////////////////////////////////////////////////////////////////////////////////
            // ///////////////////////////////////////////////////////////////////////////////////////////
            // ///////////////////////////////////////////////////////////////////////////////////////////


            public static ReportTabParts XtabTable(TabFileConfiguration tabFileConfig)

            //public static ReportTabParts XtabTable(List<string> captions, string verticalKeyHead, string numberSpecifier, double keepRowsWithTotalGreaterThan=0d, string excludeRowIfKeyContains ="", bool showOnlyKeys=false )
            {

                //scan entire dictionary for all the necessary columnsSequence, this is the template for the sequence the columnsSequence will be displayed
                //because the are rows that don't have all the columnsSequence or have the columnsSequence in a different sequence, 
                var columnsSequence = new List<string>();
                foreach (KeyValuePair<string, Dictionary<string, double>> entry in _xtab)
                {
                    foreach (KeyValuePair<string, double> kv in entry.Value)
                    {
                        var columnExists = columnsSequence.FindIndex(x => x == kv.Key);
                        if (columnExists == -1)
                        {
                            columnsSequence.Add(kv.Key);
                        }
                    }
                }

                var tab = "\t";
                var lf = "\n";
                //var crlf = Environment.NewLine;
                var xtabSb = new StringBuilder();

                var xtabOrderedKeys = _xtab.Keys.OrderBy(x => x.ToString()); //order the keys by key name
                columnsSequence = columnsSequence.OrderBy(q => q).ToList(); //order the columnsSequence by column name
                var controlTotal = 0d;

                var rtp = new ReportTabParts();

                // //////////////////////////////////////
                // captions

                var captions = new List<string>();
                captions.Add(tabFileConfig.State);
                captions.Add("Database: " + tabFileConfig.CurrentProcessingDate);
                captions.Add(tabFileConfig.Title);
                for (var i = 0; i < tabFileConfig.Captions.Length; i++)
                {
                    captions.Add(tabFileConfig.Captions[i]);
                }
                rtp.Captions = captions;


                // //////////////////////////////////////
                // theadRow
                rtp.TheadRows = PrepareTHeadRow(tabFileConfig.VerticalKeyHead, tabFileConfig.ShowOnlyKeys, columnsSequence, tab);

                // ////////////////////////////////////////////////
                //rows by row this is tbody
                //
                var tuple = PrepareTBodyRows(tabFileConfig.NumberOfDecimalsToUse, tabFileConfig.Filter_KeepRowsWIthTotalsGreaterThan, tabFileConfig.Filter_ExcludeRowIfKeyContains, tabFileConfig.ShowOnlyKeys, columnsSequence, tab, lf, xtabOrderedKeys, ref controlTotal);
                var verticalTots = tuple.Item1;
                rtp.TBodyRows = tuple.Item2;


                // ////////////////////////////////////////////////
                // verticalTots at the very bottom of the table
                rtp.TFootRow = PrepareTFootRow(tabFileConfig.NumberOfDecimalsToUse, tabFileConfig.ShowOnlyKeys, tab, lf, controlTotal, verticalTots);

                return rtp;
            }

            private static (double[], string) PrepareTBodyRows(string numberSpecifier, double keepRowsWithTotalGreaterThan, string excludeRowIfKeyContains, bool showOnlyKeys, List<string> columns, string tab, string lf, IOrderedEnumerable<string> xtabOrderedKeys, ref double controlTotal)
            {
                var verticalTots = new double[columns.Count];  //keeps track of the column totals as we go along
                var rowSb = new StringBuilder();

                foreach (var k in xtabOrderedKeys)
                {

                    if (excludeRowIfKeyContains.Length > 0)
                    {
                        if (!k.Contains(excludeRowIfKeyContains))
                        {
                            continue;  // skip this row
                        }
                    }


                    rowSb.Append(k).Append(tab); //row key always displayed

                    if (showOnlyKeys) { }
                    else
                    {

                        var row = new double[columns.Count];

                        //arrange the columuns of this row by the column template
                        foreach (KeyValuePair<string, double> kv in _xtab[k])
                        {
                            var idx = columns.FindIndex(x => x == kv.Key);
                            row[idx] = kv.Value;
                        }

                        var horizontaTot = 0d;

                        for (var x = 0; x < row.Length; x++)
                        {
                            var columnValue = row[x];
                            rowSb.Append(columnValue.ToString(numberSpecifier)).Append(tab);
                            horizontaTot = horizontaTot + columnValue;
                        }

                        if (keepRowsWithTotalGreaterThan > 0d)
                        {
                            if (horizontaTot > keepRowsWithTotalGreaterThan)
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


                        rowSb.Append(horizontaTot.ToString(numberSpecifier));
                        controlTotal = controlTotal + horizontaTot;

                    }
                    rowSb.Append(lf);
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
                        grandTotal = grandTotal + verticalTots[v];
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

            private static string PrepareTHeadRow(string verticalKeyHead, bool showOnlyKeys, List<string> columns, string tab)
            {
                var thead = new StringBuilder();

                //column Headings
                thead.Append(verticalKeyHead).Append(tab); //this tab is for the column taken up by the keys of each row

                if (showOnlyKeys) { }
                else
                {
                    foreach (var k in columns)
                    {
                        thead.Append(k).Append(tab);
                    }
                    thead.Append("TOTALS");
                }

                return thead.ToString();
            }
        }

    }