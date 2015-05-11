﻿// Copyright 2015 (c) Yasuhiro Niji
// Use of this source code is governed by the MIT License,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace TKRain.Models
{
    // 潮位情報
    // http://www1.road.pref.tokushima.jp/a6/rasterxml/Symbol_01_12.xml
    class TideLevel
    {
        TideStationList stationInfoList;
        const string TideLebelUrl = "http://www1.road.pref.tokushima.jp/a6/rasterxml/Symbol_01_12.xml";
        const int SeriesNumber = 3000;

        public TideLevel()
        {
            string path = Path.Combine("Config", "TideLevel.json");
            if (!File.Exists(path))
                Stations.GetStationInformations();
            string json = File.ReadAllText(path);
            this.stationInfoList = JsonConvert.DeserializeObject<TideStationList > (json);
        }

        public int GetTideLevelData(DateTime prevObservationTime)
        {
            int number = 1;

            TideSList data = Observation.TgGetStream<TideSList>(TideLebelUrl, 0);
            if (data == null)
                return 0;

            //観測時間のデータがないので10分遅れで表示されていると仮定

            DateTime now = DateTime.Now.AddMinutes(-10);
            DateTime observationDateTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute /10 *10, 0);

            if (observationDateTime <= prevObservationTime)
                return 0;

            TideDataList tideDataList = new TideDataList
            {
                dt = observationDateTime,
                hr = new List<TideData>()
            };
         
            //累積データを作成
            foreach (var oi in data.Sym)
            {
                try
                {
                    string[] ocb = oi.Ocb.Split(',');
                    int mo = int.Parse(ocb[0]);
                    string sc = ocb[0] + "-" + ocb[1];
              
                    /// ToDo 24:00 の例外処理が必要

                    TideSeries ts;
                    string path = Path.Combine("Data", "Tide", sc + ".json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        ts = JsonConvert.DeserializeObject<TideSeries>(json);
                        if(ts.ot.Length != SeriesNumber)
                        {
                            if(ts.ot.Length < SeriesNumber)
                            {
                                DateTime[] ot2 = new DateTime[SeriesNumber];
                                double?[] level2 = new double?[SeriesNumber];
                                DateTime tm = observationDateTime.AddMinutes(-10 * SeriesNumber);
                                int offset = SeriesNumber - ts.ot.Length;
                                for (int n = 0; n < offset; n++)
                                {
                                    tm.AddMinutes(10);
                                    ot2[n] = tm;
                                }
                                for (int n = 0; n < ts.ot.Length; n++)
                                {
                                    ot2[n + offset] = ts.ot[n];
                                    level2[n + offset] = ts.level[n];
                                }
                                ts.ot = ot2;
                                ts.level = level2;
                            }
                        }
                        DateTime rsdt = ts.ot[SeriesNumber - 1];
                        int nt = (int)((observationDateTime - rsdt).Ticks / 6000000000);
                        for (int n = 0; n < SeriesNumber - nt; n++)
                        {
                            ts.ot[n] = ts.ot[n + nt];
                            ts.level[n] = ts.level[n + nt];
                        }
                        DateTime dt = observationDateTime.AddMinutes(-10 * nt);
                        for (int n = SeriesNumber - nt; n < SeriesNumber - 1; n++)
                        {
                            dt = dt.AddMinutes(10);
                            ts.ot[n] = dt;
                            ts.level[n] = null;
                        }
                    }
                    else
                    {
                        ts = new TideSeries
                        {
                            mo = mo,
                            sc = sc,
                            obn = oi.Nm,
                            ot = new DateTime[SeriesNumber],
                            level = new double?[SeriesNumber],
                        };
                        DateTime dt = observationDateTime.AddMinutes(-10 * SeriesNumber);
                        for (int n = 0; n < SeriesNumber; n++)
                        {
                            dt = dt.AddMinutes(10);
                            ts.ot[n] = dt;
                        }
                    }

                    ts.ot[SeriesNumber - 1] = observationDateTime;
                    string slevel = oi.Data.Split(',')[0];
                    ts.level[SeriesNumber - 1] = Observation.StringToDouble(slevel);
                    string[] pt = oi.Pt.Split(',');

                    tideDataList.hr.Add(new TideData
                    {
                        mo = mo,
                        sc = sc,
                        obn = oi.Nm,
                 
                        level = Observation.StringToDouble(slevel),
                        dt = observationDateTime
                    });

                    File.WriteAllText(path, JsonConvert.SerializeObject(ts));
                    number++;
                }
                catch (Exception e1)
                {
                    LoggerClass.NLogInfo("潮位データ作成エラー 観測所: " + oi.Nm + " メッセージ: " + e1.Message);
                }
            }
            File.WriteAllText(Path.Combine("Data", "Tide", "TideData.json"), JsonConvert.SerializeObject(tideDataList));

            File.WriteAllText(Path.Combine("Data", "TideLevelObservationTime.text"), observationDateTime.ToString());
            return number;
        }
        
    }

    public class TideDataList
    {
        public DateTime dt { get; set; }
        public List<TideData> hr { get; set; }
    }

    public class TideData
    {
        /// 管理事務所コード
        public int mo { get; set; }
        /// 観測局コード
        public string sc { get; set; }
        /// 観測局名称
        public string obn { get; set; }
        /// 緯度
        public double lat { get; set; }
        /// 経度
        public double lng { get; set; }
        /// 潮位
        public double? level { get; set; }
        /// 観測時間
        public DateTime dt { get; set; }
    }


    public class TideSeries
    {
        /// 管理事務所コード
        public int mo { get; set; }
        /// 観測局コード
        public string sc { get; set; }
        /// 観測局名称
        public string obn { get; set; }
        // 観測時間
        public DateTime[] ot { get; set; }
        // 潮位
        public double?[] level { get; set; }
    }

    /*
    <Data>の最初が10分データ
    <Ocb>が事務所コード,観測所コード
    <Sym>
    <Cd>01_12</Cd>
    <Ocb>5,2</Ocb>
    <Itm>12</Itm>
    <Nm>浅川港</Nm>
    <Data>1.13,---,null,---,1.10,---,00,---</Data>
    <Pt>79986.65,69822.695</Pt>
    <Eki>0</Eki>
    </Sym>
    */

    /// 潮位情報
    [XmlRoot("SList")]
    public class TideSList {
        [XmlElementAttribute("Sym")]
        public List<TideSym> Sym { get; set; }
    }

    /// <remarks/>
    public class TideSym
    {
        /// <remarks/>
        public string Cd { get; set; }

        /// <remarks/>
        public string Ocb { get; set; }

        /// <remarks/>
        public byte Itm { get; set; }

        /// <remarks/>
        public string Nm { get; set; }

        /// <remarks/>
        public string Data { get; set; }

        /// <remarks/>
        public string Pt { get; set; }

        /// <remarks/>
        public byte Eki { get; set; }
    }    
}