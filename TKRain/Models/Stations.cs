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
using Newtonsoft.Json;
using System.Xml.Serialization;

/*
** 観測地点の所在地、座標等を取得するプログラム
** 東部県土整備局管内の観測所は、庁舎別の分類にしなおしている。
*/

namespace TKRain.Models
{
    class Stations
    {
        const string StationUrl = "http://www1.road.pref.tokushima.jp/c6/xml90000/00000_00000_00000.xml";

        private class ManageOffice
        {
            public string Address { get; set; }
            public int Office { get; set; }
        }

        readonly static List<ManageOffice> ManageOfficeList = new List<ManageOffice> {
                new ManageOffice {Address = "徳島市", Office = 1},
                new ManageOffice {Address = "鳴門市", Office = 2},
                new ManageOffice {Address = "小松島市", Office = 1},
                new ManageOffice {Address = "勝浦郡", Office = 1},
                new ManageOffice {Address = "名東郡", Office = 1},
                new ManageOffice {Address = "名西郡石井町", Office = 3},
                new ManageOffice {Address = "名西郡神山町", Office = 1},
                new ManageOffice {Address = "板野郡松茂町", Office = 2},
                new ManageOffice {Address = "板野郡北島町", Office = 1},
                new ManageOffice {Address = "板野郡藍住町", Office = 1},
                new ManageOffice {Address = "板野郡板野町", Office = 2},
                new ManageOffice {Address = "板野郡上板町", Office = 3},
            };

        public void GetStationInformations()
        {

            RainStationList rain = new RainStationList();
            RiverStationList river = new RiverStationList();
            DamStationList dam = new DamStationList();
            TideStationList tide = new TideStationList();
            RoadStationList road = new RoadStationList();

            var sl = Observation.TgGetStream<StaionList>(StationUrl, 0);
            foreach (var om in sl.pom.pa.om)
            {
                int ofc = om.ofc;
                foreach (var o in om.obn)
                {
                    double lat, lng;
                    int mo;
                    string obl;
                    switch (o.ikc)
                    {
                        case 1:
                            XyToBl.Calcurate(4, double.Parse(o.yc), double.Parse(o.xc),  out lat, out lng);
                            mo = GetManageOffice(ofc, o.obl, out obl, o.ikc + ":" + o.obn);
                            rain.Add(new RainStationInfo
                            {
                                mo = mo,
                                sc = ofc + "-" +o.obc,
                                //ofc = ofc,
                                //obc = o.obc,
                                obn = o.obn,
                                obl = obl,
                                lat = lat,
                                lng = lng
                            });
                            break;
                        case 4:
                            XyToBl.Calcurate(4, double.Parse(o.yc), double.Parse(o.xc), out lat, out lng);
                            mo = GetManageOffice(ofc, o.obl, out obl, o.ikc + ":" + o.obn);
                            river.Add(new RiverStationInfo
                            {
                                mo = mo,
                                sc = ofc + "-" + o.obc,
                                //ofc = ofc,
                                //obc = o.obc,
                                obn = o.obn,
                                obl = obl,
                                rsn = o.rsn,
                                rn = o.rn,
                                gmax = o.gmax,
                                gmin = o.gmin,
                                //spfw = o.spfw,
                                //cauw = o.cauw,
                                //spcw = o.spcw,
                                //danw = o.danw,
                                //plaw = o.plaw,
                                lat = lat,
                                lng = lng,
                            });
                            break;
                        case 7:
                            XyToBl.Calcurate(4, double.Parse(o.yc), double.Parse(o.xc), out lat, out lng);
                            mo = GetManageOffice(ofc, o.obl, out obl, o.ikc + ":" + o.obn);
                            dam.Add(new DamStationInfo
                            {
                                mo = mo,
                                sc = ofc + "-" + o.obc,
                                //ofc = ofc,
                                //obc = o.obc,
                                obn = o.obn,
                                obl = obl,
                                rsn = o.rsn,
                                rn = o.rn,
                                gmax = o.gmax,
                                gmin = o.gmin,
                                lat = lat,
                                lng = lng,
                            });
                            break;
                        case 12:
                            XyToBl.Calcurate(4, double.Parse(o.yc), double.Parse(o.xc), out lat, out lng);
                            mo = GetManageOffice(ofc, o.obl, out obl, o.ikc + ":" + o.obn);
                            tide.Add(new TideStationInfo
                            {
                                mo = mo,
                                sc = ofc + "-" + o.obc,
                                //ofc = ofc,
                                //obc = o.obc,
                                obn = o.obn,
                                obl = obl,
                                lat = lat,
                                lng = lng
                            });
                            break;
                        case 302:
                            XyToBl.Calcurate(4, double.Parse(o.yc), double.Parse(o.xc), out lat, out lng);
                            mo = GetManageOffice(ofc, o.obl, out obl, o.ikc + ":" + o.obn);
                            road.Add(new RoadStationInfo
                            {
                                mo = mo,
                                sc = ofc + "-" + o.obc,
                                //ofc = ofc,
                                //obc = o.obc,
                                obn = o.obn,
                                obl = obl,
                                lat = lat,
                                lng = lng
                            });
                            break;
                    }
                }
            }
            File.WriteAllText(Path.Combine("Config", "Rainfall.json"), JsonConvert.SerializeObject(rain, Formatting.Indented));
            File.WriteAllText(Path.Combine("Config", "RiverLevel.json"), JsonConvert.SerializeObject(river, Formatting.Indented));
            File.WriteAllText(Path.Combine("Config", "DamInfo.json"), JsonConvert.SerializeObject(dam, Formatting.Indented));
            File.WriteAllText(Path.Combine("Config", "TideLevel.json"), JsonConvert.SerializeObject(tide, Formatting.Indented));
            File.WriteAllText(Path.Combine("Config", "RoadWeather.json"), JsonConvert.SerializeObject(road, Formatting.Indented));
        }

        private static int GetManageOffice(int ofc, string address, out string alterAddress, string obn)
        {
            if (address.StartsWith("徳島県"))
                alterAddress = address.Substring(3);
            else
                alterAddress = address;
            if (ofc > 2)
                return ofc;
            foreach(var ma in ManageOfficeList)
            {
                if (alterAddress.StartsWith(ma.Address))
                    return ma.Office;
            }
            LoggerClass.NLogInfo("所在地に問題。観測所: " + obn + " 所在地: " + address);
            return ofc;
        }
    }

    public class StationInfoList : List<StationInfo> { }

    public class StationInfo
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
    }

    public class RainStationList : List<RainStationInfo> { }
    public class RainStationInfo : StationInfo
    {
        /// 所在地
        public string obl { get; set; }
    }
    public class RiverStationList : List<RiverStationInfo> { }
    public class RiverStationInfo : StationInfo
    {
        /// 所在地
        public string obl { get; set; }
        /// 水系
        public string rsn { get; set; }
        /// 河川名
        public string rn { get; set; }
        /// グラフ最大値
        public string gmax { get; set; }
        /// グラフ最小値
        public string gmin { get; set; }
        /// 計画高水位
        //public string spfw { get; set; }
        /// はん濫危険水位
        //public string cauw { get; set; }
        /// 避難判断水位
        //public string spcw { get; set; }
        /// はん濫注意水位
        //public string danw { get; set; }
        /// 水防団待機水位
        //public string plaw { get; set; }
    }
    public class DamStationList : List<DamStationInfo> { }
    public class DamStationInfo : StationInfo
    {
        /// 所在地
        public string obl { get; set; }
        /// 水系
        public string rsn { get; set; }
        /// 河川名
        public string rn { get; set; }
        /// グラフ最大値
        public string gmax { get; set; }
        /// グラフ最小値
        public string gmin { get; set; }
    }
    public class RoadStationList : List<RoadStationInfo> { }
    public class RoadStationInfo : StationInfo
    {
        /// 所在地
        public string obl { get; set; }
    }
    public class TideStationList : List<TideStationInfo> { }
    public class TideStationInfo : StationInfo
    {
        /// 所在地
        public string obl { get; set; }
    }

    /// <summary>
    /// 観測局情報
    /// </summary>
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
