// Copyright 2015 (c) Yasuhiro Niji
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
    // http://www.road.pref.tokushima.jp/a6/rasterxml/Symbol_01_12.xml
    class TideLevel
    {
        readonly string _tideLevelUrl;
        const int SeriesNumber = 3000;

        public TideLevel()
        {
            _tideLevelUrl = AppInit.Host + "/a6/rasterxml/Symbol_01_12.xml";
        }

        public int GetTideLevelData(DateTime prevObservationTime)
        {
            int number = 1;

            TideSList data = Observation.TgGetStream<TideSList>(_tideLevelUrl, 0);
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
         
            //累積データの修正
            foreach (var oi in data.Sym)
            {
                try
                {
                    string[] ocb = oi.Ocb.Split(',');
                    int mo = int.Parse(ocb[0]);
                    string sc = ocb[0] + "-" + ocb[1];

                    TideSeries ts;
                    string path = Path.Combine(AppInit.DataDir, "Tide", sc + ".json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        ts = JsonConvert.DeserializeObject<TideSeries>(json);
                        ts.obn = oi.Nm;  //名称は毎回確認

                        if (ts.ot.Length != SeriesNumber)
                        {
                            if(ts.ot.Length < SeriesNumber)
                            {
                                DateTime[] ot2 = new DateTime[SeriesNumber];
                                double?[] level2 = new double?[SeriesNumber];
                                DateTime tm = observationDateTime.AddMinutes(-10 * SeriesNumber);
                                int offset = SeriesNumber - ts.ot.Length;
                                for (int n = 0; n < offset; n++)
                                {
                                    tm = tm.AddMinutes(10);
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
                        if (nt > SeriesNumber)
                            nt = SeriesNumber;
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
                        lat = ts.lat,
                        lng = ts.lng,
                        level = Observation.StringToDouble(slevel),
                        dt = observationDateTime
                    });

                    File.WriteAllText(path, JsonConvert.SerializeObject(ts));
                    number++;
                }
                catch (Exception e1)
                {
                    LoggerClass.LogInfo("潮位データ作成エラー 観測所: " + oi.Nm + " メッセージ: " + e1.Message);
                }
            }
            File.WriteAllText(Path.Combine(AppInit.DataDir, "Tide", "TideData.json"), JsonConvert.SerializeObject(tideDataList));

            File.WriteAllText(Path.Combine(AppInit.DataDir, "TideLevelObservationTime.txt"), observationDateTime.ToString());
            return number;
        }

        public void SetTideInfo()
        {

            TideSList data = Observation.TgGetStream<TideSList>(_tideLevelUrl, 0);
            if (data == null)
                return;

            string j = File.ReadAllText(Path.Combine(AppInit.DataDir, "Config", "TideLevel.json"));
            TideStationList stationInfoList = JsonConvert.DeserializeObject<TideStationList>(j);

            //累積データヘッダー部分の修正
            foreach (var oi in data.Sym)
            {
                try
                {
                    TideSeries rs;
                    string[] ocb = oi.Ocb.Split(',');
                    string sc = ocb[0] + "-" + ocb[1];
             
                    string path = Path.Combine(AppInit.DataDir, "Tide", sc + ".json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        rs = JsonConvert.DeserializeObject<TideSeries>(json);

                        var si = stationInfoList.Find(x => x.sc == sc);
                        if (si == null)
                        {
                            LoggerClass.LogInfo("該当の観測所情報がない 観測所: " + oi.Nm);
                            continue;
                        }

                        rs.mo = si.mo;
                        rs.sc = si.sc;
                        rs.lat = si.lat;
                        rs.lng = si.lng;
                        rs.obl = si.obl;
                        File.WriteAllText(path, JsonConvert.SerializeObject(rs));
                    }
                }
                catch (Exception e1)
                {
                    LoggerClass.LogInfo("雨量観測所情報修正エラー 観測所: " + oi.Nm + " メッセージ: " + e1.Message);
                }
            }
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
        /// 緯度
        public double lat { get; set; }
        /// 経度
        public double lng { get; set; }
        /// 所在地
        public string obl { get; set; } // 観測時間
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
