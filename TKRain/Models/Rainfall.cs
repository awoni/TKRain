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
        private StationInfoList stationInfoList;
        const string RainfallUrl = "http://www1.road.pref.tokushima.jp/c6/xml92100/00000_00000_00001.xml";
        const string RainfallStationsUrl = "http://www1.road.pref.tokushima.jp/a6/rasterxml/Symbol_01_1.xml";
        const int SeriesNumber = 300;

        public Rainfall()
        {
            string path = Path.Combine("Config", "RainfallStations.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                this.stationInfoList = JsonConvert.DeserializeObject<StationInfoList>(json);
                return;
            }
          
            SList sList = Observation.TgGetStream<SList>(RainfallStationsUrl, 0);

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

        public int GetRainfallData(DateTime prevObservationTime)
        {
            int number = 0;

            RainDocd data = Observation.TgGetStream<RainDocd>(RainfallUrl, 0);
            if (data == null)
                return 0;

            string observationTime = data.oi[0].odd.rd.d10_10m.ot;
            DateTime observationDateTime = observationTime.EndsWith("24:00")?
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
            Observation.SaveToXml(Path.Combine("Data", "Rain", "Rainfall.xml"), data, 0);
            File.WriteAllText(Path.Combine("Data", "Rain", "Rainfall.json"), JsonConvert.SerializeObject(data));


            HourRainList hourRainList = new HourRainList
            {
                dt = observationDateTime,
                hr = new List<HourRain>(),
            };
            //累積データを作成
            foreach (var oi in data.oi)
            {
                try {
                    DateTime doidt = observationDateTime;
                    if (observationTime != oi.odd.rd.d10_10m.ot)
                    {
                        LoggerClass.NLogInfo("雨量観測時間相違 観測所: " + oi.obn);
                        doidt = observationTime.EndsWith("24:00") ?
                            DateTime.Parse(observationTime.Substring(0, observationTime.Length - 6)).AddDays(1)
                            : DateTime.Parse(observationTime);
                    }

                    RainSeries rs;
                    string path = Path.Combine("Data", "Rain", oi.ofc.ToString() + "-" + oi.obc.ToString() + ".json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        rs = JsonConvert.DeserializeObject<RainSeries>(json);
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
                            ofc = oi.ofc,
                            obc = oi.obc,
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

                    rs.ot[SeriesNumber - 1] = doidt;
                    int result;
                    if (int.TryParse(oi.odd.rd.d10_10m.ov, out result))
                        rs.d10_10m_val[SeriesNumber - 1] = result;
                    else
                        rs.d10_10m_val[SeriesNumber - 1] = null;
                    rs.d10_10m_si[SeriesNumber - 1] = oi.odd.rd.d10_10m.osi;
                    if (int.TryParse(oi.odd.rd.d70_10m.ov, out result))
                        rs.d70_10m_val[SeriesNumber - 1] = result;
                    else
                        rs.d70_10m_val[SeriesNumber - 1] = null;
                    rs.d70_10m_si[SeriesNumber - 1] = oi.odd.rd.d70_10m.osi;

                    //10分毎の時間雨量の計算
                    if (rs.d70_10m_si[SeriesNumber - 1] != 0)
                    {
                        rs.d10_1h_val[SeriesNumber - 1] = null;
                        rs.d10_1h_si[SeriesNumber - 1] = rs.d70_10m_si[SeriesNumber - 1];
                    }
                    else if(rs.d70_10m_val[SeriesNumber - 1] == 0)
                    {
                        rs.d10_1h_val[SeriesNumber - 1] = 0;
                        rs.d10_1h_si[SeriesNumber - 1] = 0;
                    }
                    else if(rs.d70_10m_si[SeriesNumber - 7] == 0)
                    {
                        rs.d10_1h_val[SeriesNumber - 1] = rs.d70_10m_val[SeriesNumber - 1] - rs.d70_10m_val[SeriesNumber - 7];
                        if (rs.d10_1h_val[SeriesNumber - 1] < 0)
                            rs.d10_1h_val[SeriesNumber - 1] = 0;
                        rs.d10_1h_si[SeriesNumber - 1] = rs.d70_10m_si[SeriesNumber - 7];
                    }
                    else
                    {
                        rs.d10_1h_val[SeriesNumber - 1] = null;
                        rs.d10_1h_si[SeriesNumber - 1] = rs.d70_10m_si[SeriesNumber - 7];
                    }


                    //現況雨量一覧の作成
                    //正時しかデータを収集していない観測局があるので、正時まで有効なデータがないか検索している

                    int sn = SeriesNumber - 1;
                    DateTime odt = rs.ot[sn];
                    int nhour = doidt.Minute / 10;
                    bool flag = false;

                    for(; sn > SeriesNumber - 2 - nhour; sn--)
                    {
                        if (rs.d70_10m_si[sn] == 0)
                        {
                            flag = true;
                            break; ;
                        }
                    }

                    if (!flag)
                        sn = SeriesNumber - 1;

                    hourRainList.hr.Add(new HourRain {
                        ofc = oi.ofc,
                        obc = oi.obc,
                        obn = oi.obn,
                        lat = oi.lat,
                        lng = oi.lng,
                        d10_1h_val = rs.d10_1h_val[sn],
                        d10_1h_si = rs.d10_1h_si[sn],
                        d70_10m_val = rs.d70_10m_val[sn],
                        d70_10m_si = rs.d70_10m_si[sn],
                        dt = rs.ot[sn]
                    });

                    File.WriteAllText(path, JsonConvert.SerializeObject(rs));
                    number++;
                }
                catch(Exception e1)
                {
                    LoggerClass.NLogInfo("雨量累積データ作成エラー 観測所: " + oi.obn +" メッセージ: " + e1.Message);
                }
            }
            File.WriteAllText(Path.Combine("Data", "Rain", "RainData.json"), JsonConvert.SerializeObject(hourRainList));

            File.WriteAllText(Path.Combine("Data", "RainfallObservationTime.text"), observationDateTime.ToString());           
            return number;
        }
    }

    public class HourRainList
    {
        public DateTime dt { get; set; }
        public List<HourRain> hr{ get; set; }
    }

    public class HourRain
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
        /// 事務所コード
        public int ofc { get; set; }
        /// 観測局コード
        public int obc { get; set; }
        /// 観測局名称
        public string obn { get; set; }
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
}

