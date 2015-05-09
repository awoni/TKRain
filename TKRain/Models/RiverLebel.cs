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
    class RiverLebel
    {
        private StationInfoList stationInfoList;
        const string RiverLevelUrl = "http://www1.road.pref.tokushima.jp/c6/xml92100/00000_00000_00004.xml";
        const string RiverLevelStationsUrl = "http://www1.road.pref.tokushima.jp/a6/rasterxml/Symbol_01_4.xml";
        const int SeriesNumber = 300;

        public RiverLebel()
        {
            string path = Path.Combine("Config", "RiverLebelStations.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                this.stationInfoList = JsonConvert.DeserializeObject<StationInfoList>(json);
                return;
            }

            SList sList = Observation.TgGetStream<SList>(RiverLevelStationsUrl, 0);

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

        public int GetRiverLevelData(DateTime prevObservationTime)
        {
            int number = 0; 

            RiverDocd data = Observation.TgGetStream<RiverDocd>(RiverLevelUrl, 0);
            if (data == null)
                return 0;

            string observationTime = data.oi[0].odd.wd.d10_10m.ot;
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
            Observation.SaveToXml(Path.Combine("Data", "River", "RiverLebel.xml"), data, 0);
            File.WriteAllText(Path.Combine("Data", "River", "RiverLebel.json"), JsonConvert.SerializeObject(data));

            RiverDataList riverDataList = new RiverDataList
            {
                dt = observationDateTime,
                hr = new List<RiverData>()
            };

            //累積データを作成
            foreach (var oi in data.oi)
            {
                try
                {
                    /// ToDo 24:00 の例外処理が必要
                    DateTime doidt = observationDateTime;
                    if (observationTime != oi.odd.wd.d10_10m.ot)
                    {
                        LoggerClass.NLogInfo("水位観測時間相違 観測所: " + oi.obn);
                        doidt = observationTime.EndsWith("24:00") ?
                            DateTime.Parse(observationTime.Substring(0, observationTime.Length - 6)).AddDays(1)
                            : DateTime.Parse(observationTime);
                    }

                    RiverSeries rs;
                    string path = Path.Combine("Data", "River", oi.ofc + "-" + oi.obc + ".json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        rs = JsonConvert.DeserializeObject<RiverSeries>(json);
                        DateTime rsdt = rs.ot[SeriesNumber - 1];
                        int nt = (int)((doidt - rsdt).Ticks / 6000000000);
                        for (int n = 0; n < SeriesNumber - nt; n++)
                        {
                            rs.ot[n] = rs.ot[n + nt];
                            rs.d10_val[n] = rs.d10_val[n + nt];
                            rs.d10_si[n] = rs.d10_si[n + nt];
                        }
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
                            ofc = oi.ofc,
                            obc = oi.obc,
                            obn = oi.obn,
                            plaw = Observation.StringToDouble(oi.plaw),
                            danw = Observation.StringToDouble(oi.danw),
                            spcw = Observation.StringToDouble(oi.spcw),
                            cauw = Observation.StringToDouble(oi.cauw),
                            spfw = Observation.StringToDouble(oi.spfw),
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

                    rs.ot[SeriesNumber - 1] = doidt;
                    rs.d10_val[SeriesNumber - 1] = Observation.StringToDouble(oi.odd.wd.d10_10m.ov);
                    rs.d10_si[SeriesNumber - 1] = oi.odd.wd.d10_10m.osi;

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
                        ofc = oi.ofc,
                        obc = oi.obc,
                        obn = oi.obn,
                        plaw = Observation.StringToDouble(oi.plaw),
                        danw = Observation.StringToDouble(oi.danw),
                        spcw = Observation.StringToDouble(oi.spcw),
                        cauw = Observation.StringToDouble(oi.cauw),
                        spfw = Observation.StringToDouble(oi.spfw),
                        lat = oi.lat,
                        lng = oi.lng,
                        d10_val = rs.d10_val[sn],
                        d10_si = rs.d10_si[sn],
                        dt = rs.ot[sn]
                    });

                    File.WriteAllText(path, JsonConvert.SerializeObject(rs));
                    number++;
                }
                catch (Exception e1)
                {
                    LoggerClass.NLogInfo("水位累積データ作成エラー 観測所: " + oi.obn + " メッセージ: " + e1.Message);
                }
            }
            File.WriteAllText(Path.Combine("Data", "River", "RiverData.json"), JsonConvert.SerializeObject(riverDataList));

            File.WriteAllText(Path.Combine("data", "RiverLevelObservationTime.text"), observationDateTime.ToString());
            return number;
        }
    }


    public class RiverDataList
    {
        public DateTime dt { get; set; }
        public List<RiverData> hr { get; set; }
    }

    public class RiverData
    {
        /// 事務所コード
        public int ofc { get; set; }
        /// 観測局コード
        public int obc { get; set; }
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
        /// 事務所コード
        public int ofc { get; set; }
        /// 観測局コード
        public int obc { get; set; }
        /// 観測局名称
        public string obn { get; set; }
        /// 通報水位
        public double? plaw { get; set; }
        /// 警戒水位
        public double? danw { get; set; }
        /// 特別警戒水位
        public double? spcw { get; set; }
        /// 危険水位
        public double? cauw { get; set; }
        /// 計画高水位
        public double? spfw { get; set; }
        // 観測時間
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
        /// 通報水位
        public string plaw { get; set; }
        /// 警戒水位
        public string danw { get; set; }
        /// 特別警戒水位
        public string spcw { get; set; }
        /// 危険水位
        public string cauw { get; set; }
        /// 計画高水位
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
}
