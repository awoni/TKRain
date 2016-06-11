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
    //雨量
    class Rainfall
    {
        const string RainfallUrl = "http://www1.road.pref.tokushima.jp/c6/xml92100/00000_00000_00001.xml";
        const int SeriesNumber = 300;

        private Dictionary<string, string> WeathrDic = new Dictionary<string, string> {
            { "1-14","1-1001" },  //徳島
            { "3-6","3-1002" },  //蒲生田 
            { "4-11","4-1003" },  //木頭
            { "5-8","5-1004" },  //日和佐
            { "7-2","7-1005" },  //穴吹
            { "8-3","8-1006" },  //池田
        };

        public Rainfall()
        {
        }

        public int GetRainfallData(DateTime prevObservationTime, List<WeatherRain> weatherRainList)
        {
            int number = 0;

            RainDocd data = Observation.TgGetStream<RainDocd>(RainfallUrl, 0);
            if (data == null)
                return 0;

            string observationTime = data.oi[0].odd.rd.d10_10m.ot;
            DateTime observationDateTime = observationTime.EndsWith("24:00") ?
                DateTime.Parse(observationTime.Substring(0, observationTime.Length - 6)).AddDays(1)
                : DateTime.Parse(observationTime);

            if (observationDateTime <= prevObservationTime)
                return 0;

            HourRainList hourRainList = new HourRainList
            {
                dt = observationDateTime,
                hr = new List<HourRain>(),
            };

            RainGeoJson geojson = new RainGeoJson {
                type = "FeatureCollection",
                features = new List<RainFeature>()
            };

            //累積データの修正
            foreach (var oi in data.oi)
            {
                try
                {
                    DateTime doidt = observationDateTime;
                    if (observationTime != oi.odd.rd.d10_10m.ot)
                    {
                        LoggerClass.LogInfo("雨量観測時間相違 観測所: " + oi.obn);
                        doidt = observationTime.EndsWith("24:00") ?
                            DateTime.Parse(observationTime.Substring(0, observationTime.Length - 6)).AddDays(1)
                            : DateTime.Parse(observationTime);
                    }

                    RainSeries rs;
                    string sc = oi.ofc + "-" + oi.obc;
                    string path = Path.Combine(AppInit.DataDir, "Rain", sc + ".json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        rs = JsonConvert.DeserializeObject<RainSeries>(json);
                        rs.obn = oi.obn;  //名称は毎回確認

                        DateTime rsdt = rs.ot[SeriesNumber - 1];
                        int nt = (int)((doidt - rsdt).Ticks / 6000000000);
                        for (int n = 0; n < SeriesNumber - nt; n++)
                        {
                            rs.ot[n] = rs.ot[n + nt];
                            rs.d10_10m_val[n] = rs.d10_10m_val[n + nt];
                            rs.d10_10m_si[n] = rs.d10_10m_si[n + nt];
                            rs.d70_10m_val[n] = rs.d70_10m_val[n + nt];
                            rs.d70_10m_si[n] = rs.d70_10m_si[n + nt];
                            rs.d10_1h_val[n] = rs.d10_1h_val[n + nt];
                            rs.d10_1h_si[n] = rs.d10_1h_si[n + nt];
                        }
                        if (nt > SeriesNumber)
                            nt = SeriesNumber;
                        DateTime dt = doidt.AddMinutes(-10 * nt);
                        for (int n = SeriesNumber - nt; n < SeriesNumber - 1; n++)
                        {
                            dt = dt.AddMinutes(10);
                            rs.ot[n] = dt;
                            rs.d10_10m_val[n] = null;
                            rs.d10_10m_si[n] = -1;
                            rs.d70_10m_val[n] = null;
                            rs.d70_10m_si[n] = -1;
                            rs.d10_1h_val[n] = null;
                            rs.d10_1h_si[n] = -1;
                        }
                    }
                    else
                    {
                        rs = new RainSeries
                        {
                            mo = oi.ofc,
                            sc = sc,
                            obn = oi.obn,
                            ot = new DateTime[SeriesNumber],
                            d10_10m_val = new int?[SeriesNumber],
                            d10_10m_si = new int[SeriesNumber],
                            d70_10m_val = new int?[SeriesNumber],
                            d70_10m_si = new int[SeriesNumber],
                            d10_1h_val = new int?[SeriesNumber],
                            d10_1h_si = new int[SeriesNumber],
                        };
                        DateTime dt = doidt.AddMinutes(-10 * SeriesNumber);
                        for (int n = 0; n < SeriesNumber; n++)
                        {
                            dt = dt.AddMinutes(10);
                            rs.ot[n] = dt;
                        }
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d10_10m_si[n] = -1;
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d70_10m_si[n] = -1;
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d10_1h_si[n] = -1;
                    }

                    int sn = SeriesNumber - 1;
                    rs.ot[sn] = doidt;
                    rs.d10_10m_val[sn] = Observation.StringToInt(oi.odd.rd.d10_10m.ov);
                    rs.d10_10m_si[sn] = oi.odd.rd.d10_10m.osi;
                    rs.d70_10m_val[sn] = Observation.StringToInt(oi.odd.rd.d70_10m.ov);
                    rs.d70_10m_si[sn] = oi.odd.rd.d70_10m.osi;

                    //10分毎の時間雨量の計算
                    if (rs.d70_10m_si[sn] != 0)
                    {
                        rs.d10_1h_val[sn] = null;
                        rs.d10_1h_si[sn] = rs.d70_10m_si[SeriesNumber - 1];
                    }
                    else if (rs.d70_10m_val[sn] == 0)
                    {
                        rs.d10_1h_val[sn] = 0;
                        rs.d10_1h_si[sn] = 0;
                    }
                    else if (rs.d70_10m_si[SeriesNumber - 7] == 0)
                    {
                        rs.d10_1h_val[sn] = rs.d70_10m_val[sn] - rs.d70_10m_val[SeriesNumber - 7];
                        if (rs.d10_1h_val[sn] < 0)
                            rs.d10_1h_val[sn] = 0;
                        rs.d10_1h_si[sn] = rs.d70_10m_si[SeriesNumber - 7];
                    }
                    else if (doidt.Minute == 0 && oi.odd.rd.d30_1h.osi == 0)
                    {
                        rs.d10_1h_val[sn] = Observation.StringToInt(oi.odd.rd.d30_1h.ov);
                        rs.d10_1h_si[sn] = 0;
                    }
                    else
                    {
                        rs.d10_1h_val[sn] = null;
                        rs.d10_1h_si[sn] = rs.d70_10m_si[SeriesNumber - 7];
                    }


                    //現況雨量一覧の作成
                    //正時しかデータを収集していない観測局があるので、正時まで有効なデータがないか検索している
                    DateTime odt = rs.ot[sn];
                    int nhour = doidt.Minute / 10;
                    bool flag = false;

                    for (; sn > SeriesNumber - 2 - nhour; sn--)
                    {
                        if (rs.d10_1h_si[sn] == 0)
                        {
                            flag = true;
                            break; ;
                        }
                    }

                    if (!flag)
                        sn = SeriesNumber - 1;

                    hourRainList.hr.Add(new HourRain
                    {
                        mo = rs.mo,
                        sc = sc,
                        obn = oi.obn,
                        lat = rs.lat,
                        lng = rs.lng,
                        d10_1h_val = rs.d10_1h_val[sn],
                        d10_1h_si = rs.d10_1h_si[sn],
                        d70_10m_val = rs.d70_10m_val[sn],
                        d70_10m_si = rs.d70_10m_si[sn],
                        dt = rs.ot[sn]
                    });

                    geojson.features.Add(new RainFeature {
                        type = "Feature",
                        geometry = new Geometry {
                            type = "Point",
                            coordinates = new double[]{rs.lng, rs.lat}
                        },
                        properties = new RainProperties
                        {
                            観測所 = oi.obn,
                            時間雨量 = rs.d10_1h_val[sn],
                            累計雨量 = rs.d70_10m_val[sn],
                            観測時間 = rs.ot[sn],
                            コード = sc
                        }
                    });

                    oi.lat = rs.lat;
                    oi.lng = rs.lng;

                    File.WriteAllText(path, JsonConvert.SerializeObject(rs));
                    
                    //道路気象で雨量データも表示するため
                    if(WeathrDic.ContainsKey(sc))
                    {
                        weatherRainList.Add(new WeatherRain {
                            sc = WeathrDic[sc],
                            d10_10m_val = rs.d10_10m_val[SeriesNumber - 1],
                            d10_10m_si = rs.d10_10m_si[SeriesNumber - 1],
                            d10_1h_val = rs.d10_1h_val[SeriesNumber - 1],
                            d10_1h_si = rs.d10_1h_si[SeriesNumber - 1],
                            d70_10m_val = rs.d70_10m_val[SeriesNumber - 1],
                            d70_10m_si = rs.d70_10m_si[SeriesNumber - 1],
                            ot = rs.ot[SeriesNumber - 1]
                        });
                    }
                    number++;
                }
                catch (Exception e1)
                {
                    LoggerClass.LogInfo("雨量累積データ作成エラー 観測所: " + oi.obn + " メッセージ: " + e1.Message);
                }
            }
            File.WriteAllText(Path.Combine(AppInit.DataDir, "Rain", "RainData.json"), JsonConvert.SerializeObject(hourRainList));
            Observation.SaveToXml(Path.Combine(AppInit.DataDir, "Rain", "Rainfall.xml"), data, 0);
            File.WriteAllText(Path.Combine(AppInit.DataDir, "Rain", "Rainfall.json"), JsonConvert.SerializeObject(data));
            File.WriteAllText(Path.Combine(AppInit.DataDir, "Rain", "Rainfall.geojson"), JsonConvert.SerializeObject(geojson));

            File.WriteAllText(Path.Combine(AppInit.DataDir, "RainfallObservationTime.txt"), observationDateTime.ToString());
            return number;
        }

        //累積データの観測所情報の更新
        public void SetRainInfo()
        {

            RainDocd data = Observation.TgGetStream<RainDocd>(RainfallUrl, 0);
            if (data == null)
                return;

            string j = File.ReadAllText(Path.Combine(AppInit.DataDir, "Config", "Rainfall.json"));
            RainStationList stationInfoList = JsonConvert.DeserializeObject<RainStationList>(j);

            //累積データヘッダー部分の修正
            foreach (var oi in data.oi)
            {
                try
                {
                    RainSeries rs;
                    string sc = oi.ofc + "-" + oi.obc;
                    string path = Path.Combine(AppInit.DataDir, "Rain", sc + ".json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        rs = JsonConvert.DeserializeObject<RainSeries>(json);

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

    public class HourRainList
    {
        public DateTime dt { get; set; }
        public List<HourRain> hr { get; set; }
    }

    public class HourRain
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
        /// 時間雨量
        public int? d10_1h_val { get; set; }
        /// 時間雨量ステータス
        public int d10_1h_si { get; set; }
        /// 累計雨量
        public int? d70_10m_val { get; set; }
        /// 累計雨量ステータス
        public int d70_10m_si { get; set; }
        /// 観測時間
        public DateTime dt { get; set; }
    }


    public class RainSeries
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
        public DateTime[] ot { get; set; }
        public int?[] d10_10m_val { get; set; }
        public int[] d10_10m_si { get; set; }
        public int?[] d10_1h_val { get; set; }
        public int[] d10_1h_si { get; set; }
        public int?[] d70_10m_val { get; set; }
        public int[] d70_10m_si { get; set; }
    }

    /// <remarks/>
    [XmlRoot("docd")]
    public class RainDocd
    {
        /// <remarks/>
        public string cd { get; set; }
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("oid")]
        public List<RainOid> oi { get; set; }
    }

    /// <remarks/>
    public class RainOid
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
        public RainOidOdd odd { get; set; }
    }

    /// <remarks/>
    /// 
    public class RainOidOdd
    {
        /// 雨量データ
        public RainOidOddRD rd { get; set; }
    }

    /// 雨量データ
    public partial class RainOidOddRD
    {
        /// 10分雨量
        public od d10_10m { get; set; }
        /// 1時間雨量 使われていない
        //public od d10_1h { get; set; }
        /// 10分累計雨量
        public od d70_10m { get; set; }
        /// 1時間累計雨量
        public od d70_1h { get; set; }
        /// 使われていない
        //public od d30_10m { get; set; }
        /// １時間雨量
        public od d30_1h { get; set; }
    }


    public class RainGeoJson
    {
        public string type { get; set; }
        public List<RainFeature> features { get; set; }
    }

    public class RainFeature
    {
        public string type { get; set; }
        public Geometry geometry { get; set; }
        public RainProperties properties { get; set; }
    }

    public class Geometry
    {
        public string type { get; set; }
        public double[] coordinates { get; set; }
    }

    public class RainProperties
    {
        public string 観測所 { get; set; }
        public double? 時間雨量 { get; set; }
        public double? 累計雨量 { get; set; }
        public DateTime 観測時間 { get; set; }
        public string コード { get; set; }
    }

}

