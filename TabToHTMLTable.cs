using CommonVoters;
using Newtonsoft.Json;
using System;
using System.Text;

namespace XTAB
{

    
    public class TabToHTML
    {

        public static void MakeHTMLTableFileFromTabFile(XTAB.HtmlTableConfig htmlTableConfig, TabDelimitedFileParts rpt)
        {
            try
            {

                if (rpt.TBodyRows.Length > 0)  // if there anything in tbodyrows, it doesn't matter how many
                {
                    var tblHtml = XTAB.TabToHTML.Table(rpt, htmlTableConfig);
                    var tblfn = htmlTableConfig.tabFileFullPath.Replace(".tab.csv", ".tbl.html");
                    CommonVoters.Functions.WriteAllText(tblfn, tblHtml);

                }
            }
            catch (Exception ee)
            {
                Functions.ERRORLOG("MakeHTMLTableFileFromTabFile, tabfile: " + htmlTableConfig.tabFileFullPath);
            }
        }

        public static void MakeHTMLTableFileFromTabFile(XTAB.HtmlTableConfig htmlTableConfig)
        {
            try
            {

                var jsonTabDelimitedStr = System.IO.File.ReadAllText(htmlTableConfig.tabFileFullPath);
                var rpt = JsonConvert.DeserializeObject<XTAB.TabDelimitedFileParts>(jsonTabDelimitedStr);
                if (rpt.TBodyRows.Length > 0)  // if there anything in tbodyrows, it doesn't matter how many
                {
                    var tblHtml = XTAB.TabToHTML.Table(rpt, htmlTableConfig);
                    var tblfn = htmlTableConfig.tabFileFullPath.Replace(".tab.csv", ".tbl.html");
                    CommonVoters.Functions.WriteAllText(tblfn, tblHtml);

                }
            }
            catch (Exception ee)
            {
                Functions.ERRORLOG("MakeHTMLTableFileFromTabFile, tabfile: " + htmlTableConfig.tabFileFullPath);
            }
        }

        public static string Table(TabDelimitedFileParts rpt, HtmlTableConfig htmlTableConfig)
        {

            // /////////////////////////////
        var jsLink = @" class=""ptr"" onclick=""fnp(this);""";
        var linkArrow = "↖";

        var bingColTemplate = "<td onclick=\"B(this)\">bing</td>";
            var googleColTemplate = "<td onclick=\"G(this)\">google</td>";
            var bingColStyle = $"width: {TableColWidths.bingColumn}px; text-align:center; cursor:pointer; font-size:10px;";
            var googleColStyle = $"width: {TableColWidths.googleColumn}px; text-align:center; cursor:pointer; font-size:10px;";

            var SearchDataAttributes = String.Empty;
            var SearchKeyColumns = String.Empty;
            var SearchColumnsBingStyle = String.Empty;
            var SearchColumnsGoogleStyle = String.Empty;

            var SearchColumnsRowTD = String.Empty;
            if (htmlTableConfig.SearchLinksConfig.SearchProviders == CommonVoters.SearchProviders.NONE)
            { }
            else
            {
                SearchDataAttributes = $"data-SearchAttr=\"{htmlTableConfig.SearchLinksConfig.SearchLinkDataAttributes.Trim()}\"" + " " +
                $"data-SearchKeys=\"{htmlTableConfig.SearchLinksConfig.KeyColumnsToUseInProviderUrl}\"";

                SearchColumnsRowTD = bingColTemplate + googleColTemplate;
                if (htmlTableConfig.SearchLinksConfig.SearchProviders == CommonVoters.SearchProviders.BOTH)
                {
                    SearchColumnsRowTD = bingColTemplate + googleColTemplate;
                    SearchColumnsBingStyle = bingColStyle;
                    SearchColumnsGoogleStyle = googleColStyle;
                }
                else
                {
                    if (htmlTableConfig.SearchLinksConfig.SearchProviders == CommonVoters.SearchProviders.BING)
                    {
                        SearchColumnsRowTD = bingColTemplate;
                        SearchColumnsBingStyle = bingColStyle;

                    }
                    else
                    {
                        if (htmlTableConfig.SearchLinksConfig.SearchProviders == CommonVoters.SearchProviders.GOOGLE)
                        {
                            SearchColumnsRowTD = googleColTemplate;
                            SearchColumnsGoogleStyle = googleColStyle;
                        }
                    }
                }
            }

            
            var rowOpen = "<tr>";
            var rowClose = "</tr>";

            // /////////////////////////////
            // calculate number of columns
            var tbodyRows = rpt.TBodyRows;
            string[] allTbodyRows = tbodyRows.Split(new char[] { htmlTableConfig.LineFeed }, StringSplitOptions.RemoveEmptyEntries);
            int numberOfRowsToCreate = (allTbodyRows.Length * htmlTableConfig.percentRowsToCreate) / 100;

            
            string[] totCellsInRow = allTbodyRows[0].Split(new char[] { htmlTableConfig.Delim }, StringSplitOptions.RemoveEmptyEntries);
            var numberOfKeyColumns = htmlTableConfig.KeyColumnsWidths.Length;
            var totTableWidth = 0;
            for (var i = 0; i < htmlTableConfig.KeyColumnsWidths.Length; i++)
            {
                totTableWidth += htmlTableConfig.KeyColumnsWidths[i];
            }
            for (var i = 0; i < totCellsInRow.Length - numberOfKeyColumns; i++)
            {
                totTableWidth += TableColWidths.numericColumn;
            }

            // ///////////////////////////////////////////////////////////////
            // captions
            var captionsStr = new StringBuilder();
            if (htmlTableConfig.Captions.Length > 0)
            {
                captionsStr.Append("<caption>");
                captionsStr.Append(htmlTableConfig.Captions[0]);
                for (var c=1; c<htmlTableConfig.Captions.Length; c++)
                {
                    captionsStr.Append("<br>").Append(htmlTableConfig.Captions[c]);
                }
                captionsStr.Append("</caption>");
            }

            // ///////////////////////////////////////////////////////////////
            // build style element with table width and numeric cellsInRow alighment
            var styleStr = new StringBuilder();
            //if (totCellsInRow.Length > numberOfKeyColumns)
            //{
            styleStr.Append("<style>").Append(htmlTableConfig.LineFeed);

            var textAlign = "";
            for (var i = 0; i < numberOfKeyColumns; i++)
            {
                textAlign = "";
                if (htmlTableConfig.KeyColumnsAlignment[i] == "R")
                {
                    textAlign = "right";
                }
                else
                {
                    if (htmlTableConfig.KeyColumnsAlignment[i] == "L")
                    {
                        textAlign = "left";

                    }
                    else
                    {
                        if (htmlTableConfig.KeyColumnsAlignment[i] == "C")
                        {
                            textAlign = "center";

                        }
                        else
                        {
                            throw new Exception("Number of KeyColumnAlignment defined is not R C or L");
                        }
                    }
                }
                styleStr.Append($@"#tbl td:nth-child({i + 1}) {{width:{htmlTableConfig.KeyColumnsWidths[i]}px; text-align:{textAlign};}}").Append(htmlTableConfig.LineFeed);

            }

            // all the remaining columns are result columns and are numeric and align Right
            for (var i = numberOfKeyColumns; i < totCellsInRow.Length; i++)
            {
                var col = $@"#tbl td:nth-child({i + 1}) {{ text-align:right; width:{TableColWidths.numericColumn}px;}}";
                styleStr.Append(col).Append(htmlTableConfig.LineFeed);
            }

            // if Bing/Google search link columns are defined, add them!
            if (htmlTableConfig.SearchLinksConfig.SearchProviders == CommonVoters.SearchProviders.NONE)
            {
                // do nothing
            }
            else
            {
                var idx = numberOfKeyColumns+1;
                if (SearchColumnsBingStyle != null)
                {
                    styleStr.Append($@"#tbl td:nth-child({idx}) {{ {SearchColumnsBingStyle} }}").Append(htmlTableConfig.LineFeed);
                    idx++;
                }
                if (SearchColumnsGoogleStyle != null)
                {
                    styleStr.Append($@"#tbl td:nth-child({idx}) {{ {SearchColumnsGoogleStyle} }}").Append(htmlTableConfig.LineFeed);
                    idx++;
                }
            }


            // ///////////////////////////////////////////////////
            // the style of the top line of the captions                           
            styleStr.Append(@"#cl {float: left; padding-left:5px; cursor:pointer;}").Append(htmlTableConfig.LineFeed);
            styleStr.Append(@"#cc {text-align:center;}").Append(htmlTableConfig.LineFeed);
            styleStr.Append(@"#cr {float: right; padding-right: 5px; cursor:pointer;}").Append(htmlTableConfig.LineFeed);

            styleStr.Append("</style>").Append(htmlTableConfig.LineFeed);

            //
            // //////////////////////////////////////////
            // build the colgroup
            //var colgroup = new StringBuilder();
            //colgroup.Append("<colgroup>");
            //for (int i = 0; i < htmlTableConfig.KeyColumnsWidths.Length; i++)
            //{
            //    colgroup.Append($@"<col style=""width: {htmlTableConfig.KeyColumnsWidths[i]}px;"">").Append(htmlTableConfig.LineFeed);
            //}
            //for (var i = numberOfKeyColumns; i < totCellsInRow.Length; i++)
            //{
            //    colgroup.Append($@"<col style=""width: {numericColumnPixWidth}px;"">").Append(htmlTableConfig.LineFeed);

            //}
            //colgroup.Append("</colgroup>").Append(htmlTableConfig.LineFeed);


            // ///////////////////////////////////
            // thead rows
            //<thead>
            //    <tr>
            //      <th> Month </th>
            //      < th > Savings </th>
            //    </tr>
            //  </thead>
            var theadTabs = rpt.TheadRows.Split('~');
            var thead = new StringBuilder();
            thead.Append("<thead>").Append(rowOpen);
            if (htmlTableConfig.SortAllowed)
            {
                for (int i = 0; i < htmlTableConfig.KeyColumnsWidths.Length; i++)
                {
                    var alpha = (htmlTableConfig.KeyColumnsAlignment[i] == "R") ? "false" : "true"; //(i < numberOfKeyColumns)?"true":"false";
                    thead.Append($"<th onclick=\"javascript:Sort(this,{alpha} );\">").Append("&#8597;" + theadTabs[i]).Append("</th>");

                }
                for (var i = numberOfKeyColumns; i < totCellsInRow.Length; i++)
                {
                    var alpha = "false"; //(i < numberOfKeyColumns)?"true":"false";
                    thead.Append($"<th onclick=\"javascript:Sort(this,{alpha} );\">").Append("&#8597;" + theadTabs[i]).Append("</th>");
                }
            }
            else
            {
                for (int i = 0; i < htmlTableConfig.KeyColumnsWidths.Length; i++)
                {
                    thead.Append($"<th>").Append(theadTabs[i]).Append("</th>");
                }
                for (var i = numberOfKeyColumns; i < totCellsInRow.Length; i++)
                {
                    thead.Append($"<th>").Append(theadTabs[i]).Append("</th>");
                }
            }
            thead.Append(rowClose).Append("</thead>").Append(htmlTableConfig.LineFeed);


            // //////////////////////////////////
            // the tbody rows

            var tbody = new StringBuilder();
            tbody.Append("<tbody>");
            for (var row = 0; row < numberOfRowsToCreate; row++)
            {
                tbody.Append($@"<tr>");
                string[] cellsInRow = allTbodyRows[row].Split(new char[] { htmlTableConfig.Delim }, StringSplitOptions.RemoveEmptyEntries);
                if (htmlTableConfig.KeyLinkConfiguration.DisplayHyperlinkInRow(cellsInRow[htmlTableConfig.KeyLinkConfiguration.CompareValueInColumn], htmlTableConfig.KeyLinkConfiguration.WithThisConstantPredicate))
                { tbody.Append($"<td{jsLink}>{linkArrow}{cellsInRow[0]}</td>"); }
                else
                {
                    tbody.Append($"<td>{cellsInRow[0]}</td>");
                }
                // the rest of the cells in the row do not have a link, or all if x = 0
                for (var c = 1; c < cellsInRow.Length; c++)
                {
                    tbody.Append($"<td>{cellsInRow[c]}</td>");
                }
                tbody.Append(SearchColumnsRowTD);
                tbody.Append("</tr>");
            }
            tbody.Append("</tbody>").Append(htmlTableConfig.LineFeed);

            // ////////////////////////////////////////
            // tfoot
            // <tfoot>
            //    <tr>
            //      <td> Sum </td>
            //      <td>$180 </td>
            //    </tr>
            //  </tfoot >

            var tfootTabs = rpt.TFootRow.Split(htmlTableConfig.Delim);
            var tfoot = new StringBuilder();
            tfoot.Append("<tfoot>").Append(rowOpen);
            tfoot.Append("<td>").Append("TOTALS").Append("</td>");
            for (var i = 1; i < numberOfKeyColumns; i++)
            {
                tfoot.Append("<td>").Append(" ").Append("</td>");
            }
            for (var i = 0; i < tfootTabs.Length; i++)
            {
                tfoot.Append("<td>").Append(tfootTabs[i]).Append("</td>");
            }
            tfoot.Append(rowClose).Append("</tfoot>").Append(htmlTableConfig.LineFeed);



            // ////////////////////////////////////////
            // put the table pieces together

            var sb = new StringBuilder();
            sb.Append(styleStr.ToString());
            var dataLinks = $@"data-linkcode=""{htmlTableConfig.KeyLinkConfiguration.FileNameCode}"" data-folder=""{htmlTableConfig.KeyLinkConfiguration.Folder}"" data-linkCol0=""{htmlTableConfig.KeyLinkConfiguration.LinkCol0}"" data-linkkeys=""{htmlTableConfig.KeyLinkConfiguration.NumberOfKeyColumnsUsedForQueryStringInVariablePartOfFileName}""";               
            sb.Append($@"<table id=""tbl"" {dataLinks} {SearchDataAttributes}>");

            if (htmlTableConfig.DisplayCaption)
            {
                sb.Append(captionsStr.ToString());
            }
            //sb.Append(colgroup.ToString());
            if (htmlTableConfig.DisplayTHead)
            {
                sb.Append(thead.ToString());
            }
            sb.Append(tbody.ToString());
            if (htmlTableConfig.DisplayTFoot)
            {
                sb.Append(tfoot.ToString());
            }


            sb.Append("</table>");
            return sb.ToString();
        }

    }
}
