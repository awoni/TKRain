using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using System.Xml.Serialization;

namespace TKRain.Models
{
    class Stations
    {
        const string StationUrl = "http://www1.road.pref.tokushima.jp/c6/xml90000/00000_00000_00000.xml";
        public static void GetStationInformations()
        {
            string path = Path.Combine("Config", "RainfallStations.json");
            string json = File.ReadAllText(path);
            var stationInfoList = JsonConvert.DeserializeObject<StationInfoList>(json);

            RainStationList rain = new RainStationList();

            var sl = Observation.TgGetStream<StaionList>(StationUrl, 0);
            foreach (var om in sl.pom.pa.om)
            {
                int ofc = om.ofc;
                foreach (var o in om.obn)
                {
                    if (o.ikc == 1)
                    {
                        double lat, lng;
                        XyToBl.Calcurate(4, double.Parse(o.xc), double.Parse(o.yc), out lat, out lng);
                        rain.Add(new RainStationInfo
                        {
                            ofc = ofc,
                            obc = o.obc,
                            obn = o.obn,
                            obl = o.obl,
                            lat = lat,
                            lng = lng
                        });
                    }
                }
            }

            foreach (var sil in stationInfoList)
            {
                var r = rain.Find(x => x.ofc == sil.ofc && x.obc == sil.obc);
                if (r == null)
                    LoggerClass.NLogInfo("観測所のデータがない: " + sil.obn);
                if (Math.Abs(r.lat - sil.lat) > 0.000001 || (Math.Abs(r.lng - sil.lng) > 0.000001))
                    LoggerClass.NLogInfo("座標データ相違: " + sil.obn);
            }
        }
    }

    [XmlRoot("dmd")]
    public class StaionList
    {
        /// <remarks/>
        public string cd { get; set; }
        /// <remarks/>
        public dmdPom pom { get; set; }
    }

    /// <remarks/>
    public class dmdPom
    {
        /// <remarks/>
        public dmdPomPA pa { get; set; }
    }

    /// <remarks/>
    public class dmdPomPA
    {
        /// 使われていない
        //public object pak { get; set; }

        /// 使われていない
        //public object pan { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItem("of")]
        public List<OfficeStationList> om { get; set; }
    }

    /// <remarks/>
    public class OfficeStationList
    {
        /// <remarks/>
        public int ofc { get; set; }
        /// <remarks/>
        public string ofn { get; set; }
        /// <remarks/>
        public string ofnr { get; set; }
        /// <remarks/>
        [XmlElement("obn")]
        public List<StationInformation> obn { get; set; }
    }

    /// 観測所情報

    public partial class StationInformation
    {
        /// 観測所コード
        public int obc { get; set; }
        /// 観測所名
        public string obn { get; set; }
        /// 未使用
        //public string obnr { get; set; }
        /// 所在地
        public string obl { get; set; }
        /// 水系
        public string rsn { get; set; }
        /// 河川名
        public string rn { get; set; }
        /// 不明
        //public string gzrm { get; set; }
        /// 不明
        //public string gzhm { get; set; }
        /// 不明
        //public string gcmax { get; set; }
        /// 不明
        //public string gcmin { get; set; }
        /// グラフ最大値
        public string gmax { get; set; }
        /// グラフ最小値
        public string gmin { get; set; }
        /// 計画高水位
        public string spfw { get; set; }
        /// はん濫危険水位
        public string cauw { get; set; }
        /// 避難判断水位
        public string spcw { get; set; }
        /// はん濫注意水位
        public string danw { get; set; }
        /// 水防団待機水位
        public string plaw { get; set; }
        /// 雨量　未使用
        //public string ra { get; set; }
        /// 水位　未使用
        //public string wa { get; set; }
        /// ダム諸量　未使用
        //public string da { get; set; }
        /// 道路気象　未使用
        //public string we { get; set; }
        /// 潮位　カメラ
        //public string curl { get; set; }
        /// 潮位、未使用
        //public string ti { get; set; }
        /// 業務番号
        // 1: 雨量　4: 水位　7: ダム諸量　12: 潮位　301:情報板　302: 道路気象
        public int ikc { get; set; }
        /// 業務名　これだけアトリビュート
        /// 読み込むためにはクラスを作成する必要がある
        //public string ikn { get; set; }
        /// x座標
        public string xc { get; set; }
        /// y座標
        public string yc { get; set; }
    }
}
