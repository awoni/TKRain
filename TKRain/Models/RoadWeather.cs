// Copyright 2015 (c) Yasuhiro Niji
// Use of this source code is governed by the MIT License,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Xml;
using Newtonsoft.Json;
using System.Xml.Serialization;


namespace TKRain.Models
{
    class RoadWeather
    {
        const string RoadWeatherUrl = "http://www1.road.pref.tokushima.jp/c6/xml92100/00000_00000_00302.xml";
        const int SeriesNumber = 300;

        public RoadWeather()
        {
        }

        public int GetRoadWeatherData(DateTime prevObservationTime)
        {
            int number = 0;

            RoadDocd data = Observation.TgGetStream<RoadDocd>(RoadWeatherUrl, 0);
            if (data == null)
                return 0;

            string observationTime = data.oi[0].odd.wd.d10030_10m.ot;
            DateTime observationDateTime = observationTime.EndsWith("24:00") ?
                DateTime.Parse(observationTime.Substring(0, observationTime.Length - 6)).AddDays(1)
                : DateTime.Parse(observationTime);

            if (observationDateTime <= prevObservationTime)
                return 0;


            RoadDataList roadDataList = new RoadDataList {
                dt = observationDateTime,
                hr = new List<RoadData>()
            };

            //累積データを修正
            foreach (var oi in data.oi)
            {
                try
                {
                    /// ToDo 24:00 の例外処理が必要
                    DateTime doidt = observationDateTime;
                    if (observationTime != oi.odd.wd.d10030_10m.ot)
                    {
                        LoggerClass.NLogInfo("道路気象観測時間相違 観測所: " + oi.obn);
                        doidt = observationTime.EndsWith("24:00") ?
                            DateTime.Parse(observationTime.Substring(0, observationTime.Length - 6)).AddDays(1)
                            : DateTime.Parse(observationTime);
                    }

                    RoadSeries rs;
                    string sc = oi.ofc + "-" + oi.obc;
                    string path = Path.Combine("Data", "Road", sc + ".json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        rs = JsonConvert.DeserializeObject<RoadSeries>(json);
                        rs.obn = oi.obn;  //名称は毎回確認

                        DateTime rsdt = rs.ot[SeriesNumber - 1];
                        int nt = (int)((doidt - rsdt).Ticks / 6000000000);
                        for (int n = 0; n < SeriesNumber - nt; n++)
                        {
                            rs.ot[n] = rs.ot[n + nt];
                            rs.d10030_val[n] = rs.d10030_val[n + nt];
                            rs.d10030_si[n] = rs.d10030_si[n + nt];
                            rs.d10060_val[n] = rs.d10060_val[n + nt];
                            rs.d10060_si[n] = rs.d10060_si[n + nt];
                            rs.d10070_val[n] = rs.d10070_val[n + nt];
                            rs.d10070_si[n] = rs.d10070_si[n + nt];
                        }
                        DateTime dt = doidt.AddMinutes(-10 * nt);
                        for (int n = SeriesNumber - nt; n < SeriesNumber - 1; n++)
                        {
                            dt = dt.AddMinutes(10);
                            rs.ot[n] = dt;
                            rs.d10030_val[n] = null;
                            rs.d10030_si[n] = -1;
                            rs.d10060_val[n] = null;
                            rs.d10060_si[n] = -1;
                            rs.d10070_val[n] = null;
                            rs.d10070_si[n] = -1;
                        }
                    }
                    else
                    {
                        rs = new RoadSeries
                        {
                            mo = oi.ofc,
                            sc = sc,
                            obn = oi.obn,
                            ot = new DateTime[SeriesNumber],
                            d10030_val = new double?[SeriesNumber],
                            d10030_si = new int[SeriesNumber],
                            d10060_val = new int?[SeriesNumber],
                            d10060_si = new int[SeriesNumber],
                            d10070_val = new string[SeriesNumber],
                            d10070_si = new int[SeriesNumber],
                        };
                        DateTime dt = doidt.AddMinutes(-10 * SeriesNumber);
                        for (int n = 0; n < SeriesNumber; n++)
                        {
                            dt = dt.AddMinutes(10);
                            rs.ot[n] = dt;
                        }
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d10030_si[n] = -1;
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d10060_si[n] = -1;
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d10070_si[n] = -1;
                    }

                    rs.ot[SeriesNumber - 1] = doidt;
                    double dresult;
                    if (double.TryParse(oi.odd.wd.d10030_10m.ov, out dresult))
                        rs.d10030_val[SeriesNumber - 1] = dresult;
                    else
                        rs.d10030_val[SeriesNumber - 1] = null;
                    rs.d10030_si[SeriesNumber - 1] = oi.odd.wd.d10030_10m.osi;
                    int result;
                    if (int.TryParse(oi.odd.wd.d10060_10m.ov, out result))
                        rs.d10060_val[SeriesNumber - 1] = result;
                    else
                        rs.d10060_val[SeriesNumber - 1] = null;
                    rs.d10060_si[SeriesNumber - 1] = oi.odd.wd.d10060_10m.osi;
          
                    rs.d10070_val[SeriesNumber - 1] = oi.odd.wd.d10070_10m.ov;
                    rs.d10070_si[SeriesNumber - 1] = oi.odd.wd.d10070_10m.osi;

                    roadDataList.hr.Add(new RoadData
                    {
                        mo = rs.mo,
                        sc = sc,
                        obn = oi.obn,
                        lat = rs.lat,
                        lng = rs.lng,
                        d10030_val = rs.d10030_val[SeriesNumber - 1],
                        d10030_si = rs.d10030_si[SeriesNumber - 1],
                        d10060_val = rs.d10060_val[SeriesNumber - 1],
                        d10060_si = rs.d10060_si[SeriesNumber - 1],
                        d10070_val = rs.d10070_val[SeriesNumber - 1],
                        d10070_si = rs.d10070_si[SeriesNumber - 1],
                        dt = doidt
                    });

                    oi.lat = rs.lat;
                    oi.lng = rs.lng;

                    File.WriteAllText(path, JsonConvert.SerializeObject(rs));
                    number++;
                }
                catch (Exception e1)
                {
                    LoggerClass.NLogInfo("道路気象累積データ作成エラー 観測所: " + oi.obn + " メッセージ: " + e1.Message);
                }
            }
            File.WriteAllText(Path.Combine("Data", "Road", "RoadData.json"), JsonConvert.SerializeObject(roadDataList));
            Observation.SaveToXml(Path.Combine("Data", "Road", "RoadWeather.xml"), data, 0);
            File.WriteAllText(Path.Combine("Data", "Road", "RoadWeather.json"), JsonConvert.SerializeObject(data));
            File.WriteAllText(Path.Combine("Data", "RoadWeatherObservationTime.text"), observationDateTime.ToString());
            return number;
        }

        //累積データの観測所情報の更新
        public void SetRoadInfo()
        {

            RoadDocd data = Observation.TgGetStream<RoadDocd>(RoadWeatherUrl, 0);
            if (data == null)
                return;

            string j = File.ReadAllText(Path.Combine("Config", "RoadWeather.json"));
            RoadStationList stationInfoList = JsonConvert.DeserializeObject<RoadStationList>(j);

            //累積データヘッダー部分の修正
            foreach (var oi in data.oi)
            {
                try
                {
                    RoadSeries rs;
                    string sc = oi.ofc + "-" + oi.obc;
                    string path = Path.Combine("Data", "Road", sc + ".json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        rs = JsonConvert.DeserializeObject<RoadSeries>(json);

                        var si = stationInfoList.Find(x => x.sc == sc);
                        if (si == null)
                        {
                            LoggerClass.NLogInfo("該当の観測所情報がない 観測所: " + oi.obn);
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
                    LoggerClass.NLogInfo("雨量観測所情報修正エラー 観測所: " + oi.obn + " メッセージ: " + e1.Message);
                }
            }
        }
    }

    public class RoadDataList
    {
        public DateTime dt { get; set; }
        public List<RoadData> hr { get; set; }
    }

    public class RoadData
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
        /// 気温
        public double? d10030_val { get; set; }
        /// 気温ステータス
        public int d10030_si { get; set; }
        /// 風速
        public int? d10060_val { get; set; }
        /// 風速ステータス
        public int d10060_si { get; set; }
        /// 風向
        public string d10070_val { get; set; }
        /// 風向ステータス
        public int d10070_si { get; set; }
        /// 観測時間
        public DateTime dt { get; set; }
    }


    public class RoadSeries
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
        public string obl { get; set; }
        // 観測時間
        public DateTime[] ot { get; set; }
        // 気温
        public double?[] d10030_val { get; set; }
        public int[] d10030_si { get; set; }
        // 風速
        public int?[] d10060_val { get; set; }
        public int[] d10060_si { get; set; }
        // 風向
        public string[] d10070_val { get; set; }
        public int[] d10070_si { get; set; }
    }

    /// 道路気象データ
    [XmlRoot("docd")]
    public class RoadDocd
    {
        /// 更新日時
        public string cd { get; set; }
        /// 観測データのリスト
        [XmlArrayItemAttribute("oid")]
        public List<RoadOid> oi { get; set; }
    }

    /// 観測データ
    public class RoadOid
    {
        /// 事務所コード
        public int ofc { get; set; }
        /// 事務所名称
        public string ofn { get; set; }
        /// 観測局コード
        public int obc { get; set; }
        /// 観測局名称
        public string obn { get; set; }

        /// 緯度
        public double lat { get; set; }
        /// 経度
        public double lng { get; set; }
        /// 観測詳細データ
        public RoadOidOdd odd { get; set; }
    }

    /// 観測詳細データ
    public class RoadOidOdd
    {
        /// データ
        public RoadOidOddRD wd { get; set; }
    }

    /// データ
    public partial class RoadOidOddRD
    {
        /// 10分気温
        public od d10030_10m { get; set; }
        /// 時間気温
        public od d10030_1h { get; set; }
        /// 10分路面温度 現時点ではデータなし
        //public Roadod d10040_10m { get; set; }
        /// 時間路面温度 現時点ではデータなし
        //public Roadod d10040_1h { get; set; }
        /// 10分路面状況 現時点ではデータなし
        //public Roadod d10050_10m { get; set; }
        /// 時間路面状況 現時点ではデータなし
        //public Roadod d10050_1h { get; set; }
        /// 10分風速
        public od d10060_10m { get; set; }
        /// 時間風速
        public od d10060_1h { get; set; }
        /// 10分風向
        public od d10070_10m { get; set; }
        /// 時間風向
        public od d10070_1h { get; set; }
    }
}

