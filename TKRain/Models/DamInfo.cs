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
    class DamInfo
   {
        private StationInfoList stationInfoList;
        const string DamInfoUrl = "http://www1.road.pref.tokushima.jp/c6/xml92100/00000_00000_00007.xml";
        const string DamInfoStationsUrl = "http://www1.road.pref.tokushima.jp/a6/rasterxml/Symbol_01_7.xml";

        public DamInfo()
        {
            string path = Path.Combine("Config", "DamInforStations.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                this.stationInfoList = JsonConvert.DeserializeObject<StationInfoList>(json);
                return;
            }

            SList sList = Observation.TgGetStream<SList>(DamInfoStationsUrl, 0);

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

        public int GetDamInfoData()
        {
            DateTime prevObservationTim;
            if (!IsUpdateRequired(out prevObservationTim))
                return 0;

            DamDocd data = Observation.TgGetStream<DamDocd>(DamInfoUrl, 0);
            if (data == null)
                return 0;

            string observationTime = data.oi[0].odd.dd.d10_10m.ot;
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
            Observation.SaveToXml(Path.Combine("data", "dam", "DamInfo.xml"), data, 0);
            File.WriteAllText(Path.Combine("data", "dam", "DamInfo.json"), JsonConvert.SerializeObject(data));

            File.WriteAllText(Path.Combine("data", "DamObservationTime.text"), observationDateTime.ToString());
            return 0;
        }

        private bool IsUpdateRequired(out DateTime PrevObservationTime)
        {
            try
            {
                PrevObservationTime = DateTime.Parse(File.ReadAllText(Path.Combine("data", "DamObservationTime.text")));
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
    public class DamDocd
    {
        /// 更新日時
        public string cd { get; set; }
        /// 観測データのリスト
        [XmlArrayItemAttribute("oid")]
        public List<DamOid> oi { get; set; }
    }

    /// 観測データ
    public class DamOid
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
        public DamOidOdd odd { get; set; }
    }

    /// 観測詳細データ
    public class DamOidOdd
    {
        /// データ
        public DamOidOddRD dd { get; set; }
    }

    /// データ
    public partial class DamOidOddRD
    {
        /// 10分貯水位
        public od d10_10m { get; set; }
        /// 時間貯水位
        public od d10_1h { get; set; }
        /// 10分貯水量
        public od d20_10m { get; set; }
        /// 時間貯水量
        public od d20_1h { get; set; }
        /// 10分貯水率
        public od d40_10m { get; set; }
        /// 時間貯水率
        public od d40_1h { get; set; }
        /// 10分流入量
        public od d50_10m { get; set; }
        /// 時間流入量
        public od d50_1h { get; set; }
        /// 10分放流量
        public od d70_10m { get; set; }
        /// 時間放流量
        public od d70_1h { get; set; }
        /// 10分流域平均雨量
        public od d10010_10m { get; set; }
        /// 時間流域平均雨量
        public od d10010_1h { get; set; }  
        /// 10分流域平均累計雨量
        public od d10070_10m { get; set; }
        /// 時間流域平均雨量
        public od d10070_1h { get; set; }
    }
}
