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

        public int GetRiverLevelData()
        {
            DateTime prevObservationTim;
            if (!IsUpdateRequired(out prevObservationTim))
                return 0;

            RiverDocd data = Observation.TgGetStream<RiverDocd>(RiverLevelUrl, 0);
            if (data == null)
                return 0;

            string observationTime = data.oi[0].odd.wd.d10_10m.ot;
            DateTime observationDateTime = observationTime.EndsWith("24:00") ?
                DateTime.Parse(observationTime.Substring(0, observationTime.Length - 6)).AddDays(1)
                : DateTime.Parse(observationTime);

            if (observationDateTime <= prevObservationTim)
                return 0;

            foreach (var stationData in data.oi)
            {
                var station = stationInfoList.Find(x => x.ofc == stationData.ofc && x.obc == stationData.obc);
                stationData.lat = station.lat;
                stationData.lng = station.lng;
            }
            Observation.SaveToXml(Path.Combine("data", "RiverLebel.xml"), data, 0);
            File.WriteAllText(Path.Combine("data", "RiverLebel.json"), JsonConvert.SerializeObject(data));

            File.WriteAllText(Path.Combine("data", "RiverLevelObservationTime.text"), observationDateTime.ToString());
            return 0;
        }

        private bool IsUpdateRequired(out DateTime PrevObservationTime)
        {
            try
            {
                PrevObservationTime = DateTime.Parse(File.ReadAllText(Path.Combine("data", "RiverLevelObservationTime.text")));
                //10分ごとに更新
                if ((DateTime.Now - PrevObservationTime).Ticks >= 6000000000L)
                    return true;
                return false;
            }
            catch
            {
                PrevObservationTime = default(DateTime);
                return true;
            }
        }
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
