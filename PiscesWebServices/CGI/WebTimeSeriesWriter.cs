﻿using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Reclamation.TimeSeries;
using System.Linq;
using System.Web;
using System.IO;
using System.Data;
using Reclamation.Core;
using System.Net;

namespace PiscesWebServices.CGI
{
    /// <summary>
    ///  returns results from web query to timeseries data in pisces.
    /// "http://www.usbr.gov/pn-bin/instant.pl?list=boii ob,boii obx&start=2016-04-15&end=2016-04-20"
    /// "http://lrgs1/pn-bin/daily?list=jck fb, amf fb&start=2016-04-15&end=2016-04-20"
    /// 
    /// options :  
    ///      back=12  (12 hours for instant, 12 days for daily)
    ///      print_hourly=true (print hourly data)
    /// 
    /// Legacy Test Samples
    /// http://www.usbr.gov/pn-bin/instant.pl?station=ABEI&year=2016&month=1&day=1&year=2016&month=1&day=1&pcode=OB&pcode=OBX&pcode=OBM&pcode=TU&print_hourly=1
    /// http://www.usbr.gov/pn-bin/instant.pl?station=BOII&year=2016&month=1&day=1&year=2016&month=1&day=1&pcode=OB&pcode=OBX&pcode=OBN&pcode=TU
    /// http://www.usbr.gov/pn-bin/instant.pl?station=ABEI&year=2016&month=1&day=1&year=2016&month=1&day=1&pcode=OB&pcode=OBX&pcode=OBM&pcode=OBN&pcode=TUX&print_hourly=true
    /// </summary>
    public class WebTimeSeriesWriter
    {
        TimeSeriesDatabase db;
        DateTime start = DateTime.Now.AddDays(-1).Date;
        DateTime end  = DateTime.Now.Date;
        Formatter m_formatter ;
        string m_query = "";
        NameValueCollection m_collection;

        string[] supportedFormats =new string[] {"csv", // csv with headers
                                                "html", // basic html

                                                "2" // legacy csv
                                                }; 
       

       

        public WebTimeSeriesWriter(TimeSeriesDatabase db, TimeInterval interval, string query="")
        {
            this.db = db;
            m_query = query;
            InitFormatter(interval);

            
        }

        private void InitFormatter(TimeInterval interval)
        {
            if (m_query == "")
            {  
                m_query = HydrometWebUtility.GetQuery();
            }
            Logger.WriteLine("Raw query: = '" + m_query + "'");

            if (m_query == "")
            {
               StopWithError ("Error: Invalid query");
            }

            m_query = LegacyTranslation(m_query, interval);

            if (!ValidQuery(m_query))
            {
               StopWithError("Error: Invalid query");
            }

            m_collection = HttpUtility.ParseQueryString(m_query);
            if (!HydrometWebUtility.GetDateRange(m_collection, interval, out start, out end))
            {
                StopWithError("Error: Invalid dates");
            }


                

            string format = "2";
            if (m_collection.AllKeys.Contains("format"))
                format = m_collection["format"];

            if (Array.IndexOf(supportedFormats, format) < 0)
                StopWithError("Error: invalid format " + format);

            if (format == "csv")
                m_formatter = new CsvFormatter(interval, true);
            else if( format == "2")
                m_formatter = new LegacyCsvFormatter(interval, true);

            else
                m_formatter = new LegacyCsvFormatter(interval, true);

            if (m_collection.AllKeys.Contains("print_hourly"))
                m_formatter.HourlyOnly = m_collection["print_hourly"] == "true";


        }
        private void StopWithError(string msg)
        {
            Logger.WriteLine(msg);
            HydrometWebUtility.PrintHydrometTrailer(msg);
            throw new Exception(msg);
        }

        public void Run( string outputFile="")
        {
            StreamWriter sw = null;
            if (outputFile != "")
            {
                sw = new StreamWriter(outputFile);
                Console.SetOut(sw);
            }
             Console.Write("Content-type: text/html\n\n");
             HydrometWebUtility.PrintHydrometHeader();
           try 
             {
                 SeriesList list = CreateSeriesList();

                 if (list.Count == 0)
                 {
                     StopWithError("Error: list of series is empty");
                 }

                 WriteSeries(list);
             }
            finally
           {
               HydrometWebUtility.PrintHydrometTrailer();

               if (sw != null)
                   sw.Close();

           }
        //catch (Exception e)
        //{
        //    Logger.WriteLine(e.Message);
        //  Console.WriteLine("Error: Data");	
        //}

            StreamWriter standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);
        }

        private static string LegacyTranslation(string query, TimeInterval interval)
        {

            var rval = query;

            //http://www.usbr.gov/pn-bin/webarccsv.pl?station=cedc&pcode=mx&pcode=mn&back=10&format=2
            //station=boii&year=2016&month=4&day=21&year=2016&month=4&day=21&pcode=OB&pcode=OBX&pcode=OBN&pcode=TU

            if (query.IndexOf("station=") >= 0 && query.IndexOf("pcode") >= 0)
            {
                rval = LegacyStationQuery(query, interval);
            }
            else if( query.IndexOf("parameter") >=0 )
            {
                rval = rval.Replace("parameter", "list");
            }

            Logger.WriteLine(rval);
            return rval;

        }

        private static string LegacyStationQuery(string query, TimeInterval interval)
        {
            string rval = "";
            var c = HttpUtility.ParseQueryString(query);

            var pcodes = c["pcode"].Split(',');
            var cbtt = c["station"];
            var back = "";
            var start = "";
            var end = "";
            var keys = c.AllKeys;
            if (keys.Contains("back"))
            {
                back = c["back"];
                DateTime t1, t2;
                HydrometWebUtility.GetDateRange(c,interval,out t1, out t2);

                start = t1.ToString("yyyy-M-d");
                end = t2.ToString("yyyy-M-d");
            }
            else if (keys.Contains("year") && keys.Contains("month") && keys.Contains("day"))
            {

                var years = c["year"].Split(',');
                var months = c["month"].Split(',');
                var days = c["day"].Split(',');

                start = years[0] + "-" + months[0] + "-" + days[0];
                end = years[1] + "-" + months[1] + "-" + days[1];
            }
            rval = "list=";
            //rval = rval.Replace("station=" + cbtt + "", "list=");
            for (int i = 0; i < pcodes.Length; i++)
            {
                var pc = pcodes[i];
                rval += cbtt + " " + pc;
                if (i != pcodes.Length - 1)
                    rval += ",";
            }
            rval += "&start=" + start + "&end=" + end;
            if( c.AllKeys.Contains("print_hourly") )
                rval += "&print_hourly=true";

            return rval.ToLower();
        }

        private static bool ValidQuery(string query)
        {
            if (query == "")
                return false;

            return Regex.IsMatch(query,"[^A-Za-z0-9=&%+-]");
        }


        /// <summary>
        /// Gets the queried series and generates simple text output
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private void WriteSeries(SeriesList list)
        {
            m_formatter.WriteSeriesHeader(list);

            int maxDaysInMemory = 30;

            if (m_formatter.Interval == TimeInterval.Daily)
                maxDaysInMemory = 3650; // 10 years

            // maxDaysIhn memory
            //   maxdays      list.Read()    REad()
            //   10
            //   
            var t2 = end.EndOfDay();
            var t = start;
            Performance p = new Performance();
            while (t<t2)
            {
                var t3 = t.AddDays(maxDaysInMemory).EndOfDay();  

                if (t3 > t2) 
                    t3 = t2;

                var tbl = Read(list, t, t3); // 0.0 seconds windows/linux
                var interval = m_formatter.Interval;
                bool printFlags = interval == TimeInterval.Hourly || interval == TimeInterval.Irregular;
                PrintDataTable( list,tbl,m_formatter.HourlyOnly,printFlags,interval);
                t = t3.NextDay();
            }

            m_formatter.WriteSeriesTrailer();
            
           // p.Report("done");
        }

        private DataTable Read(SeriesList list, DateTime t1, DateTime t2)
        {
            var sql = CreateSQL(list, t1, t2);
            var tbl = db.Server.Table("tbl", sql);
            return tbl;
        }


        /// <summary>
        /// Create a SQL command that performs UNION of multiple series
        /// so that can be queried in one round-trip to the server.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        private string CreateSQL(SeriesList list, DateTime t1, DateTime t2)
        {
            Logger.WriteLine("CreateSQL");
            Logger.WriteLine("list of " + list.Count + " series");
            var sql = "";
            for (int i = 0; i < list.Count; i++)
            {
                string tableName = list[i].Table.TableName;
                if (!db.Server.TableExists(tableName))
                {
                    //continue;
                    sql += "SELECT '" + tableName + "' as tablename , current_timestamp as datetime, null as value, '' as flag where 0=1 ";
                }
                else
                {
                    sql += "SELECT '" + tableName + "' as tablename, datetime,value,flag FROM " + tableName;
                    if (t1 != TimeSeriesDatabase.MinDateTime || t2 != TimeSeriesDatabase.MaxDateTime)
                    {
                        sql += " WHERE datetime >= " + db.Server.PortableDateString(t1, TimeSeriesDatabase.dateTimeFormat)
                            + " AND "
                            + " datetime <= " + db.Server.PortableDateString(t2, TimeSeriesDatabase.dateTimeFormat);
                    }
                }
                if (i != list.Count - 1)
                    sql += " UNION ALL \n";
            }

            sql += " \norder by datetime,tablename ";

            return sql;
        }

        

        private SeriesList CreateSeriesList()
        {
            var interval = m_formatter.Interval;
            TimeSeriesName[] names = GetTimeSeriesName(m_collection, interval);

            var tableNames = (from n in names select n.GetTableName()).ToArray();

            var sc = db.GetSeriesCatalog("tablename in ('" + String.Join("','", tableNames) + "')");

            SeriesList sList = new SeriesList();
            foreach (var tn in names)
            {
                Series s = new Series();

                s.TimeInterval = interval;
                if (sc.Select("tablename = '" + tn.GetTableName() + "'").Length == 1)
                {
                    s = db.GetSeriesFromTableName(tn.GetTableName());
                }
                s.Table.TableName = tn.GetTableName();
                sList.Add(s);
            }
            return sList;
        }

        /// <summary>
        /// Print DataTable composed of tablename,datetime,value[,flag]
        /// with columns for each tablename
        /// </summary>
        /// <param name="list"></param>
        /// <param name="table"></param>
        private static void PrintDataTable(SeriesList list, DataTable table, 
            bool printHourly, bool printFlags, TimeInterval interval)
        {
            var t0 = "";

            if (table.Rows.Count > 0)
                t0 = FormatDate(table.Rows[0][1],interval);

            var vals = new string[list.Count];
            var flags = new string[list.Count];
            var dict = new Dictionary<string, int>();
            for (int i = 0; i < list.Count; i++)
            {
                dict.Add(list[i].Table.TableName, i);
            }

            string t="";
            bool printThisRow = false;
            for (int i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
               
                t = FormatDate(row[1],interval);

               if( t!= t0)
                {
                   if (printThisRow)
                    PrintRow(t0,vals,flags,printFlags);
                    vals = new string[list.Count];
                    flags = new string[list.Count];
                    t0 = t;
                }

                vals[dict[row[0].ToString()]] =  FormatNumber(row[2]);
                flags[dict[row[0].ToString()]] = FormatFlag(row[3]);

                DateTime date = Convert.ToDateTime(row[1]);
                bool topOfHour = date.Minute == 0;
                printThisRow = printHourly == false || (printHourly && topOfHour);

            }
            if (printThisRow)
            PrintRow(t, vals, flags,printFlags);
        }

        private static void PrintRow(string t0, string[] vals, string[] flags, bool printFlags)
        {
            var  s = t0+ ",";
            for (int i = 0; i < vals.Length; i++)
            {
                s += vals[i];
                if (printFlags)
                    s += flags[i];
                ///s += vals[i] + flags[i];
                ///
                if (i != vals.Length - 1)
                    s += ",";
            }
            Console.WriteLine(s);

        }

        /// <summary>
        /// format like this: 04/01/2015 18:00
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private static string FormatDate( object o, TimeInterval interval)
        {
            var rval = "";
            var t = Convert.ToDateTime(o);
            if (interval == TimeInterval.Irregular || interval == TimeInterval.Hourly)
                rval = t.ToString("MM/dd/yyyy HH:mm");
            else
                rval = t.ToString("MM/dd/yyyy");
            return rval;
        }

        private static string FormatFlag( object o)
        {
            if (o == DBNull.Value)
                return "";
            else
                return o.ToString();

        }

        private static string FormatNumber(object o)
        {
            var rval = "";
            if (o == DBNull.Value || o.ToString() == "")
                rval = "";//.PadLeft(11);
            else
                rval = Convert.ToDouble(o).ToString("F02").PadLeft(11) ;
            return rval;
        }

        private static TimeSeriesName[] GetTimeSeriesName(NameValueCollection query, TimeInterval interval)
        {
            List<TimeSeriesName> rval = new List<TimeSeriesName>();

            var sites = HydrometWebUtility.GetParameter(query,"list");

            Logger.WriteLine("GetTimeSeriesName()");
            Logger.WriteLine(query.ToString());
            
            var siteCodePairs = sites.Split(',');

            foreach (var item in siteCodePairs)
            {
                var tokens = item.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 2)
                {
                    TimeSeriesName tn = new TimeSeriesName(tokens[0] + "_" + tokens[1] , interval);
                    rval.Add(tn);
                }
            }
            return rval.ToArray();
        }
       


    }
}