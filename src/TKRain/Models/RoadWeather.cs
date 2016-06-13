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

//観測所
//徳島 1-1001 1-14
//蒲生田 3-1002 3-6
//木頭 4-1003 4-11
//日和佐 5-1004 5-8
//穴吹 7-1005 7-2
//池田 8-1006 8-3

namespace TKRain.Models
{
    class RoadWeather
    {
        const string RoadWeatherUrl = "http://www1.road.pref.tokushima.jp/c6/xml92100/00000_00000_00302.xml";
        const int SeriesNumber = 300;

        public RoadWeather()
        {
        }

        public int GetRoadWeatherData(DateTime prevObservationTime, List<WeatherRain> weatherRainList, List<string> filenames)
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
            bool isMakeDaily = observationDateTime.Day != prevObservationTime.Day && prevObservationTime != default(DateTime);
            if(isMakeDaily)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }

            RoadDataList roadDataList = new RoadDataList {
                dt = observationDateTime,
                hr = new List<RoadData>()
            };

            RoadGeoJson geojson = new RoadGeoJson
            {
                type = "FeatureCollection",
                features = new List<RoadFeature>()
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
                        LoggerClass.LogInfo("道路気象観測時間相違 観測所: " + oi.obn);
                        doidt = observationTime.EndsWith("24:00") ?
                            DateTime.Parse(observationTime.Substring(0, observationTime.Length - 6)).AddDays(1)
                            : DateTime.Parse(observationTime);
                    }

                    RoadSeries rs;
                    string sc = oi.ofc + "-" + oi.obc;
                    string path = Path.Combine(AppInit.DataDir, "Road", sc + ".json");
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
                            rs.d10_10m_val[n] = rs.d10_10m_val[n + nt];
                            rs.d10_10m_si[n] = rs.d10_10m_si[n + nt];
                            rs.d10_1h_val[n] = rs.d10_1h_val[n + nt];
                            rs.d10_1h_si[n] = rs.d10_1h_si[n + nt];
                            rs.d70_10m_val[n] = rs.d70_10m_val[n + nt];
                            rs.d70_10m_si[n] = rs.d70_10m_si[n + nt];
                        }
                        if (nt > SeriesNumber)
                            nt = SeriesNumber;
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
                            rs.d10_10m_val[n] = null;
                            rs.d10_10m_si[n] = -1;
                            rs.d10_1h_val[n] = null;
                            rs.d10_1h_si[n] = -1;
                            rs.d70_10m_val[n] = null;
                            rs.d70_10m_si[n] = -1;
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
                            d10_10m_val = new double?[SeriesNumber],
                            d10_10m_si = new int[SeriesNumber],
                            d10_1h_val = new double?[SeriesNumber],
                            d10_1h_si = new int[SeriesNumber],
                            d70_10m_val = new double?[SeriesNumber],
                            d70_10m_si = new int[SeriesNumber],
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
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d10_10m_si[n] = -1;
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d10_1h_si[n] = -1;
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d70_10m_si[n] = -1;
                    }

                    int sn = SeriesNumber - 1;
                    rs.ot[sn] = doidt;
                    rs.d10030_val[sn] = Observation.StringToDouble(oi.odd.wd.d10030_10m.ov);
                    rs.d10030_si[sn] = oi.odd.wd.d10030_10m.osi;
                    rs.d10060_val[sn] = Observation.StringToInt(oi.odd.wd.d10060_10m.ov);
                    rs.d10060_si[sn] = oi.odd.wd.d10060_10m.osi;
          
                    rs.d10070_val[sn] = oi.odd.wd.d10070_10m.ov;
                    rs.d10070_si[sn] = oi.odd.wd.d10070_10m.osi;

                    //雨量データの計算
        var wr = weatherRainList.Find(x => x.sc == sc);
                    if(wr != null && wr.ot == doidt)
                    {
                        rs.d10_10m_val[sn] = wr.d10_10m_val;
                        rs.d10_10m_si[sn] = wr.d10_10m_si;
                        rs.d10_1h_val[sn] = wr.d10_1h_val;
                        rs.d10_1h_si[sn] = wr.d10_1h_si;
                        rs.d70_10m_val[sn] = wr.d70_10m_val;
                        rs.d70_10m_si[sn] = wr.d70_10m_si;
                    }
                    else
                    {
                        rs.d10_10m_val[sn] = null;
                        rs.d10_10m_si[sn] = -1;
                        rs.d10_1h_val[sn] = null;
                        rs.d10_1h_si[sn] = -1;
                        rs.d70_10m_val[sn] = null;
                        rs.d70_10m_si[sn] = -1;
                    }

                    roadDataList.hr.Add(new RoadData
                    {
                        mo = rs.mo,
                        sc = sc,
                        obn = oi.obn,
                        lat = rs.lat,
                        lng = rs.lng,
                        d10030_val = rs.d10030_val[sn],
                        d10030_si = rs.d10030_si[sn],
                        d10060_val = rs.d10060_val[sn],
                        d10060_si = rs.d10060_si[sn],
                        d10070_val = rs.d10070_val[sn],
                        d10070_si = rs.d10070_si[sn],
                        d10_10m_val = rs.d10_10m_val[sn],
                        d10_10m_si = rs.d10_10m_si[sn],
                        d10_1h_val = rs.d10_1h_val[sn],
                        d10_1h_si = rs.d10_1h_si[sn],
                        d70_10m_val = rs.d70_10m_val[sn],
                        d70_10m_si = rs.d70_10m_si[sn],
                        dt = doidt
                    });

                    geojson.features.Add(new RoadFeature
                    {
                        type = "Feature",
                        geometry = new Geometry
                        {
                            type = "Point",
                            coordinates = new double[] { rs.lng, rs.lat }
                        },
                        properties = new RoadProperties
                        {
                            観測所 = oi.obn,
                            気温 = rs.d10030_val[sn],
                            風速 = rs.d10060_val[sn],
                            風向 = rs.d10070_val[sn],
                            時間雨量 = (int?) rs.d10_1h_val[sn] ,
                            観測時間 = doidt,
                            コード = sc
                        }
                    });

                    oi.lat = rs.lat;
                    oi.lng = rs.lng;

                    File.WriteAllText(path, JsonConvert.SerializeObject(rs));
                    number++;
                    //日が変わったら日次ファイルを更新
                    if (isMakeDaily)
                    {
                        MakeDailyData(rs, prevObservationTime, filenames);
                    }
                }
                catch (Exception e1)
                {
                    LoggerClass.LogInfo("道路気象累積データ作成エラー 観測所: " + oi.obn + " メッセージ: " + e1.Message);
                }
            }
            File.WriteAllText(Path.Combine(AppInit.DataDir, "Road", "RoadData.json"), JsonConvert.SerializeObject(roadDataList));
            Observation.SaveToXml(Path.Combine(AppInit.DataDir, "Road", "RoadWeather.xml"), data, 0);
            File.WriteAllText(Path.Combine(AppInit.DataDir, "Road", "RoadWeather.json"), JsonConvert.SerializeObject(data));
            File.WriteAllText(Path.Combine(AppInit.DataDir, "Road", "RoadWeather.geojson"), JsonConvert.SerializeObject(geojson));
            File.WriteAllText(Path.Combine(AppInit.DataDir, "RoadWeatherObservationTime.txt"), observationDateTime.ToString());

            return number;
        }

        private void MakeDailyData(RoadSeries rs, DateTime dt, List<string> filenames)
        {
            DateTime dt0 = new DateTime(dt.Year, dt.Month, dt.Day);
            WeatherDaily wd = new WeatherDaily();
            wd.temperature = new double? [144];
            wd.wind_speed = new int?[144];
            wd.wind_direction = new string[144];
            wd.ten_minutes_rainfall = new double?[144];
            wd.hourly_rainfall = new double?[144];
            wd.total_rainfall = new double?[144];
            wd.observation_time = new DateTime[144];
            StringBuilder csv = new StringBuilder();
            csv.Append("時間, 気温, 風速, 風向, 10分雨量, 時間雨量, 累計雨量\r\n");
            int offset = (int)((dt0 - rs.ot[0]).Ticks / 6000000000) + 1;
            for (int n = offset; n < offset + 144; n++)
            {
                    wd.temperature[n - offset] = rs.d10030_val[n];
                    wd.wind_speed[n - offset] = rs.d10060_val[n];
                    wd.wind_direction[n - offset] = rs.d10070_val[n];
                    wd.ten_minutes_rainfall[n - offset] = rs.d10_10m_val[n];
                    wd.hourly_rainfall[n - offset] = rs.d10_1h_val[n];
                    wd.total_rainfall[n - offset] = rs.d70_10m_val[n];
                    wd.observation_time[n - offset] = rs.ot[n];
                    csv.Append($"{rs.ot[n].ToString("HH:mm")}, {rs.d10030_val[n]}, {rs.d10060_val[n]}, {rs.d10070_val[n]}, {rs.d10_10m_val[n]}, {rs.d10_1h_val[n]}, {rs.d70_10m_val[n]}\r\n");
            }

            string jsonFilename = rs.sc + "-" + dt0.ToString("yyyyMMdd") + ".json";
            File.WriteAllText(Path.Combine(AppInit.DataDir, "RoadDaily", jsonFilename), JsonConvert.SerializeObject(wd));
            filenames.Add(jsonFilename);

            string csvFileName = rs.sc + "-" + dt0.ToString("yyyyMMdd") + ".csv";
            File.WriteAllText(Path.Combine(AppInit.DataDir, "RoadDaily", csvFileName), csv.ToString(), Encoding.GetEncoding(932));
            filenames.Add(csvFileName);
        }

        //累積データの観測所情報の更新
        public void SetRoadInfo()
        {

            RoadDocd data = Observation.TgGetStream<RoadDocd>(RoadWeatherUrl, 0);
            if (data == null)
                return;

            string j = File.ReadAllText(Path.Combine(AppInit.DataDir, "Config", "RoadWeather.json"));
            RoadStationList stationInfoList = JsonConvert.DeserializeObject<RoadStationList>(j);

            //累積データヘッダー部分の修正
            foreach (var oi in data.oi)
            {
                try
                {
                    RoadSeries rs;
                    string sc = oi.ofc + "-" + oi.obc;
                    string path = Path.Combine(AppInit.DataDir, "Road", sc + ".json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        rs = JsonConvert.DeserializeObject<RoadSeries>(json);

                        var si = stationInfoList.Find(x => x.sc == sc);
                        if (si == null)
                        {
                            LoggerClass.LogInfo("該当の観測所情報がない 観測所: " + oi.obn);
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
                    LoggerClass.LogInfo("雨量観測所情報修正エラー 観測所: " + oi.obn + " メッセージ: " + e1.Message);
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
        ///10分雨量
        public double? d10_10m_val { get; set; }
        ///10分雨量ステータス
        public int d10_10m_si { get; set; }
        ///時間雨量
        public double? d10_1h_val { get; set; }
        ///時間雨量ステータス
        public int d10_1h_si { get; set; }
        ///累計雨量
        public double? d70_10m_val { get; set; }
        ///累計雨量ステータス
        public int d70_10m_si { get; set; }
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
        //10分雨量
        public double?[] d10_10m_val { get; set; }
        public int[] d10_10m_si { get; set; }
        //時間雨量
        public double?[] d10_1h_val { get; set; }
        public int[] d10_1h_si { get; set; }
        //累計雨量
        public double?[] d70_10m_val { get; set; }
        public int[] d70_10m_si { get; set; }
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


    public class RoadGeoJson
    {
        public string type { get; set; }
        public List<RoadFeature> features { get; set; }
    }

    public class RoadFeature
    {
        public string type { get; set; }
        public Geometry geometry { get; set; }
        public RoadProperties properties { get; set; }
    }

    public class RoadProperties
    {
        public string 観測所 { get; set; }
        public double? 気温 { get; set; }
        public int? 風速 { get; set; }
        public string 風向 { get; set; }
        public int? 時間雨量 { get; set; }
        public DateTime 観測時間 { get; set; }
        public string コード { get; set; }
    }

    public class WeatherDaily
    {
        public double?[] temperature { get; set; }
        public int?[] wind_speed { get; set; }
        public string[] wind_direction { get; set; }
        public double?[] ten_minutes_rainfall { get; set; }
        public double?[] hourly_rainfall { get; set; }
        public double?[] total_rainfall { get; set; }
        public DateTime[] observation_time { get; set; }
    }
    public class WeatherRain
    {
        /// 観測局コード
        public string sc { get; set; }
        public int? d10_10m_val { get; set; }
        public int d10_10m_si { get; set; }
        public int? d10_1h_val { get; set; }
        public int d10_1h_si { get; set; }
        public int? d70_10m_val { get; set; }
        public int d70_10m_si { get; set; }
        // 観測時間
        public DateTime ot { get; set; }
    }
}

