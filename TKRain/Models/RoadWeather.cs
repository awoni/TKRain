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
        private StationInfoList stationInfoList;
        const string RoadWeatherUrl = "http://www1.road.pref.tokushima.jp/c6/xml92100/00000_00000_00302.xml";
        const string RoadWeatherStationsUrl = "http://www1.road.pref.tokushima.jp/a6/rasterxml/Symbol_01_302.xml";
        const int SeriesNumber = 300;

        public RoadWeather()
        {
            string path = Path.Combine("Config", "RoadWeatherStations.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                this.stationInfoList = JsonConvert.DeserializeObject<StationInfoList>(json);
                return;
            }

            SList sList = Observation.TgGetStream<SList>(RoadWeatherStationsUrl, 0);

            stationInfoList = new StationInfoList();

            foreach (var station in sList.Sym)
            {
                var ocb = station.Ocb.Split(',');
                var pt = station.Pt.Split(',');
                double lat, lng;
                XyToBl.Calcurate(4, double.Parse(pt[0]), double.Parse(pt[1]), out lat, out lng);
                stationInfoList.Add(new StationInfo
                {
                    ofc = int.Parse(ocb[0]),
                    obc = int.Parse(ocb[1]),
                    obn = station.Nm,
                    lat = lat,
                    lng = lng
                });
            }
            File.WriteAllText(path, JsonConvert.SerializeObject(stationInfoList));
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

            foreach (var stationData in data.oi)
            {
                var station = stationInfoList.Find(x => x.ofc == stationData.ofc && x.obc == stationData.obc);
                stationData.lat = station.lat;
                stationData.lng = station.lng;
            }
            Observation.SaveToXml(Path.Combine("Data", "Road", "RoadWeather.xml"), data, 0);
            File.WriteAllText(Path.Combine("Data", "Road", "RoadWeather.json"), JsonConvert.SerializeObject(data));


            RoadDataList roadDataList = new RoadDataList {
                dt = observationDateTime,
                hr = new List<RoadData>()
            };

            //累積データを作成
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
                    string path = Path.Combine("Data", "Road", oi.ofc + "-" + oi.obc + ".json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        rs = JsonConvert.DeserializeObject<RoadSeries>(json);
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
                            ofc = oi.ofc,
                            obc = oi.obc,
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
                        ofc = oi.ofc,
                        obc = oi.obc,
                        obn = oi.obn,
                        lat = oi.lat,
                        lng = oi.lng,
                        d10030_val = rs.d10030_val[SeriesNumber - 1],
                        d10030_si = rs.d10030_si[SeriesNumber - 1],
                        d10060_val = rs.d10060_val[SeriesNumber - 1],
                        d10060_si = rs.d10060_si[SeriesNumber - 1],
                        d10070_val = rs.d10070_val[SeriesNumber - 1],
                        d10070_si = rs.d10070_si[SeriesNumber - 1],
                        dt = doidt
                    });

                    File.WriteAllText(path, JsonConvert.SerializeObject(rs));
                    number++;
                }
                catch (Exception e1)
                {
                    LoggerClass.NLogInfo("道路気象累積データ作成エラー 観測所: " + oi.obn + " メッセージ: " + e1.Message);
                }
            }
            File.WriteAllText(Path.Combine("Data", "Road", "RoadData.json"), JsonConvert.SerializeObject(roadDataList));
            File.WriteAllText(Path.Combine("Data", "RoadWeatherObservationTime.text"), observationDateTime.ToString());
            return number;
        }
    }

    public class RoadDataList
    {
        public DateTime dt { get; set; }
        public List<RoadData> hr { get; set; }
    }

    public class RoadData
    {
        /// 事務所コード
        public int ofc { get; set; }
        /// 観測局コード
        public int obc { get; set; }
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
        /// 事務所コード
        public int ofc { get; set; }
        /// 観測局コード
        public int obc { get; set; }
        /// 観測局名称
        public string obn { get; set; }
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

