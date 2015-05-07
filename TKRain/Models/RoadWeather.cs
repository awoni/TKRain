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

        public int GetRoadWeatherData()
        {
            DateTime prevObservationTim;
            if (!IsUpdateRequired(out prevObservationTim))
                return 0;

            RoadDocd data = Observation.TgGetStream<RoadDocd>(RoadWeatherUrl, 0);
            if (data == null)
                return 0;

            string observationTime = data.oi[0].odd.wd.d10030_10m.ot;
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
            Observation.SaveToXml(Path.Combine("data", "RoadWeather.xml"), data, 0);
            File.WriteAllText(Path.Combine("data", "RoadWeather.json"), JsonConvert.SerializeObject(data));

            File.WriteAllText(Path.Combine("data", "RoadWeatherObservationTime.text"), observationDateTime.ToString());
            return 0;
        }

        private bool IsUpdateRequired(out DateTime PrevObservationTime)
        {
            try
            {
                PrevObservationTime = DateTime.Parse(File.ReadAllText(Path.Combine("data", "RoadWeatherObservationTime.text")));
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

