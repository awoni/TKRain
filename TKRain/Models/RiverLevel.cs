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
    class RiverLevel
    {
        readonly string _riverLevelUrl;
        const int SeriesNumber = 300;

        public RiverLevel()
        {
            _riverLevelUrl = AppInit.Host + "/c6/xml92100/00000_00000_00004.xml";
        }

        public int GetRiverLevelData(DateTime prevObservationTime)
        {
            int number = 0; 

            RiverDocd data = Observation.TgGetStream<RiverDocd>(_riverLevelUrl, 0);
            if (data == null)
                return 0;

            string observationTime = data.oi[0].odd.wd.d10_10m.ot;
            DateTime observationDateTime = observationTime.EndsWith("24:00") ?
                DateTime.Parse(observationTime.Substring(0, observationTime.Length - 6)).AddDays(1)
                : DateTime.Parse(observationTime);

            if (observationDateTime <= prevObservationTime)
                return 0;

            RiverDataList riverDataList = new RiverDataList
            {
                dt = observationDateTime,
                hr = new List<RiverData>()
            };

            RiverGeoJson geojson = new RiverGeoJson
            {
                type = "FeatureCollection",
                features = new List<RiverFeature>()
            };

            //累積データの修正
            foreach (var oi in data.oi)
            {
                try
                {
                    DateTime doidt = observationDateTime;
                    if (observationTime != oi.odd.wd.d10_10m.ot)
                    {
                        LoggerClass.LogInfo("水位観測時間相違 観測所: " + oi.obn);
                        doidt = observationTime.EndsWith("24:00") ?
                            DateTime.Parse(observationTime.Substring(0, observationTime.Length - 6)).AddDays(1)
                            : DateTime.Parse(observationTime);
                    }

                    RiverSeries rs;
                    string sc = oi.ofc + "-" + oi.obc;
                    string path = Path.Combine(AppInit.DataDir, "River", sc + ".json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        rs = JsonConvert.DeserializeObject<RiverSeries>(json);
                        rs.obn = oi.obn;  //名称は毎回確認

                        DateTime rsdt = rs.ot[SeriesNumber - 1];
                        int nt = (int)((doidt - rsdt).Ticks / 6000000000);
                        for (int n = 0; n < SeriesNumber - nt; n++)
                        {
                            rs.ot[n] = rs.ot[n + nt];
                            rs.d10_val[n] = rs.d10_val[n + nt];
                            rs.d10_si[n] = rs.d10_si[n + nt];
                        }
                        if (nt > SeriesNumber)
                            nt = SeriesNumber;
                        DateTime dt = doidt.AddMinutes(-10 * nt);
                        for (int n = SeriesNumber - nt; n < SeriesNumber - 1; n++)
                        {
                            dt = dt.AddMinutes(10);
                            rs.ot[n] = dt;
                            rs.d10_val[n] = null;
                            rs.d10_si[n] = -1;
                        }
                    }
                    else
                    {
                        rs = new RiverSeries
                        {
                            mo = oi.ofc,
                            sc = sc,
                            obn = oi.obn,
                            ot = new DateTime[SeriesNumber],
                            d10_val = new double?[SeriesNumber],
                            d10_si = new int[SeriesNumber],
                        };
                        DateTime dt = doidt.AddMinutes(-10 * SeriesNumber);
                        for (int n = 0; n < SeriesNumber; n++)
                        {
                            dt = dt.AddMinutes(10);
                            rs.ot[n] = dt;
                        }
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d10_si[n] = -1;
                    }

                    double? plaw = Observation.StringToDouble(oi.plaw);
                    double? danw = Observation.StringToDouble(oi.danw);
                    double? spcw = Observation.StringToDouble(oi.spcw);
                    double? cauw = Observation.StringToDouble(oi.cauw);
                    double? spfw = Observation.StringToDouble(oi.spfw);

                    rs.ot[SeriesNumber - 1] = doidt;
                    rs.d10_val[SeriesNumber - 1] = Observation.StringToDouble(oi.odd.wd.d10_10m.ov);
                    rs.d10_si[SeriesNumber - 1] = oi.odd.wd.d10_10m.osi;
                    rs.plaw = plaw;
                    rs.danw = danw;
                    rs.spcw = spcw;
                    rs.cauw = cauw;
                    rs.spfw = spfw;

                    //現況水位一覧の作成
                    //正時しかデータを収集していない観測局があるので、正時まで有効なデータがないか検索している
                    int sn = SeriesNumber - 1;
                    DateTime odt = rs.ot[sn];
                    int nhour = doidt.Minute / 10;
                    bool flag = false;

                    for (; sn > SeriesNumber - 2 - nhour; sn--)
                    {
                        if (rs.d10_si[sn] == 0)
                        {
                            flag = true;
                            break; ;
                        }
                    }

                    if (!flag)
                        sn = SeriesNumber - 1;

                    riverDataList.hr.Add(new RiverData
                    {
                        mo = rs.mo,
                        sc = sc,
                        obn = oi.obn,
                        plaw = plaw,
                        danw = danw,
                        spcw = spcw,
                        cauw = cauw,
                        spfw = spfw,
                        lat = rs.lat,
                        lng = rs.lng,
                        rn = rs.rn,
                        d10_val = rs.d10_val[sn],
                        d10_si = rs.d10_si[sn],
                        dt = rs.ot[sn]
                    });

                    geojson.features.Add(new RiverFeature
                    {
                        type = "Feature",
                        geometry = new Geometry
                        {
                            type = "Point",
                            coordinates = new double[] { rs.lng, rs.lat }
                        },
                        properties = new RiverProperties
                        {
                            観測所 = oi.obn,
                            水位 = rs.d10_val[sn],
                            計画高水位 = plaw,
                            はん濫危険水位 = danw,
                            避難判断水位 = spcw,
                            はん濫注意水位 = cauw,
                            水防団待機水位 = spfw,
                            観測時間 = rs.ot[sn],
                            コード = sc
                        }
                    });

                    oi.lat = rs.lat;
                    oi.lng = rs.lng;

                    File.WriteAllText(path, JsonConvert.SerializeObject(rs));
                    number++;
                }
                catch (Exception e1)
                {
                    LoggerClass.LogInfo("水位累積データ作成エラー 観測所: " + oi.obn + " メッセージ: " + e1.Message);
                }
            }
            File.WriteAllText(Path.Combine(AppInit.DataDir, "River", "RiverData.json"), JsonConvert.SerializeObject(riverDataList));
            Observation.SaveToXml(Path.Combine(AppInit.DataDir, "River", "RiverLevel.xml"), data, 0);
            File.WriteAllText(Path.Combine(AppInit.DataDir, "River", "RiverLevel.json"), JsonConvert.SerializeObject(data));
            File.WriteAllText(Path.Combine(AppInit.DataDir, "River", "RiverLevel.geojson"), JsonConvert.SerializeObject(geojson));

            File.WriteAllText(Path.Combine(AppInit.App_Data, "RiverLevelObservationTime.txt"), observationDateTime.ToString());
            return number;
        }

        //累積データの観測所情報の更新
        public void SetRiverInfo()
        {

            RiverDocd data = Observation.TgGetStream<RiverDocd>(_riverLevelUrl, 0);
            if (data == null)
                return;

            string j = File.ReadAllText(Path.Combine(AppInit.DataDir, "Config", "RiverLevel.json"));
            RiverStationList stationInfoList = JsonConvert.DeserializeObject<RiverStationList>(j);

            //累積データヘッダー部分の修正
            foreach (var oi in data.oi)
            {
                try
                {
                    RiverSeries rs;
                    string sc = oi.ofc + "-" + oi.obc;
                    string path = Path.Combine(AppInit.DataDir, "River", sc + ".json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        rs = JsonConvert.DeserializeObject<RiverSeries>(json);

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
                        rs.rsn = si.rsn;
                        rs.rn = si.rn;
                        rs.gmax = si.gmax;
                        rs.gmin = si.gmin;
                        File.WriteAllText(path, JsonConvert.SerializeObject(rs));
                    }
                }
                catch (Exception e1)
                {
                    LoggerClass.LogInfo("水位観測所情報修正エラー 観測所: " + oi.obn + " メッセージ: " + e1.Message);
                }
            }
        }
    }


    public class RiverDataList
    {
        public DateTime dt { get; set; }
        public List<RiverData> hr { get; set; }
    }

    public class RiverData
    {
        /// 管理事務所コード
        public int mo { get; set; }
        /// 観測局コード
        public string sc { get; set; }        
        /// 観測局名称
        public string obn { get; set; }
        /// 通報水位
        public double? plaw { get; set; }
        /// 警戒水位
        public double? danw { get; set; }
        /// 特別警戒水位
        public Double? spcw { get; set; }
        /// 危険水位
        public double? cauw { get; set; }
        /// 計画高水位
        public double? spfw { get; set; }
        /// 緯度
        public double lat { get; set; }
        /// 経度
        public double lng { get; set; }
        /// 河川名
        public string rn { get; set; }
        /// 水位
        public double? d10_val { get; set; }
        /// 水位ステータス
        public int d10_si { get; set; }
        /// 水位変化
        public int? d10_chg { get; set; }
        /// 観測時間
        public DateTime dt { get; set; }
    }

    public class RiverSeries
    {
        /// 管理事務所コード
        public int mo { get; set; }
        /// 観測局コード
        public string sc { get; set; }
        /// 観測局名称
        public string obn { get; set; }
        /// 計画高水位
        public double? plaw { get; set; }
        /// 危険水位
        public double? danw { get; set; }
        /// 特別警戒水位
        public double? spcw { get; set; }
        /// 警戒水位
        public double? cauw { get; set; }
        /// 通報水位
        public double? spfw { get; set; }
        /// 緯度
        public double lat { get; set; }
        /// 経度
        public double lng { get; set; }
        /// 所在地
        public string obl { get; set; }
        /// 水系
        public string rsn { get; set; }
        /// 河川名
        public string rn { get; set; }
        /// グラフ最大値
        public string gmax { get; set; }
        /// グラフ最小値
        public string gmin { get; set; }        // 観測時間
        public DateTime[] ot { get; set; }
        // 水位
        public double?[] d10_val { get; set; }
        public int[] d10_si { get; set; }
    }

    /// 水位データ
    [XmlRoot("docd")]
    public class RiverDocd
    {
        /// 更新日時
        public string cd { get; set; }
        /// 観測データのリスト
        [XmlArrayItemAttribute("oid")]
        public List<RiverOid> oi { get; set; }
    }

    /// 観測データ
    public class RiverOid
    {
        /// 事務所コード
        public int ofc { get; set; }
        /// 事務所名称
        public string ofn { get; set; }
        /// 観測局コード
        public int obc { get; set; }
        /// 観測局名称
        public string obn { get; set; }
        /// 計画高水位
        public string plaw { get; set; }
        /// 危険水位
        public string danw { get; set; }
        /// 特別警戒水位
        public string spcw { get; set; }
        /// 警戒水位
        public string cauw { get; set; }
        /// 通報水位
        public string spfw { get; set; }
        /// 緯度
        public double lat { get; set; }
        /// 経度
        public double lng { get; set; }
        /// 観測詳細データ
        public RiverOidOdd odd { get; set; }
    }

    /// 観測詳細データ
    public class RiverOidOdd
    {
        /// データ
        public RiverOidOddRD wd { get; set; }
    }

    /// データ
    public partial class RiverOidOddRD
    {
        /// 10分水位
        public Riverod d10_10m { get; set; }
        /// 1時間水位
        public Riverod d10_1h { get; set; }
    }

    /// 観測データ詳細
    public class Riverod : od
    {
        public string chg { get; set; }
    }

    public class RiverGeoJson
    {
        public string type { get; set; }
        public List<RiverFeature> features { get; set; }
    }

    public class RiverFeature
    {
        public string type { get; set; }
        public Geometry geometry { get; set; }
        public RiverProperties properties { get; set; }
    }

    public class RiverProperties
    {
        public string 観測所 { get; set; }
        public double? 水位 { get; set; }
        public double? 計画高水位 { get; set; }
        public double? はん濫危険水位 { get; set; }
        public double? 避難判断水位 { get; set; }
        public double? はん濫注意水位 { get; set; }
        public double? 水防団待機水位 { get; set; }
        public DateTime 観測時間 { get; set; }
        public string コード { get; set; }
    }
}
