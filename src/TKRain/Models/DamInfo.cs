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
    class DamInfo
   {
        const string DamInfoUrl = "http://www1.road.pref.tokushima.jp/c6/xml92100/00000_00000_00007.xml";
        const int SeriesNumber = 300;

        public DamInfo()
        {
        }

        public int GetDamInfoData(DateTime prevObservationTime)
        {
            int number = 0;

            DamDocd data = Observation.TgGetStream<DamDocd>(DamInfoUrl, 0);
            if (data == null)
                return 0;

            string observationTime = data.oi[0].odd.dd.d10_10m.ot;
            DateTime observationDateTime = observationTime.EndsWith("24:00") ?
                DateTime.Parse(observationTime.Substring(0, observationTime.Length - 6)).AddDays(1)
                : DateTime.Parse(observationTime);

            if (observationDateTime <= prevObservationTime)
                return 0;

            DamDataList damDataList = new DamDataList
            {
                dt = observationDateTime,
                hr = new List<DamData>()
            };

            //累積データの修正
            foreach (var oi in data.oi)
            {
                try
                {
                    DateTime doidt = observationDateTime;
                    if (observationTime != oi.odd.dd.d10_10m.ot)
                    {
                        LoggerClass.LogInfo("ダム情報観測時間相違 観測所: " + oi.obn);
                        doidt = observationTime.EndsWith("24:00") ?
                            DateTime.Parse(observationTime.Substring(0, observationTime.Length - 6)).AddDays(1)
                            : DateTime.Parse(observationTime);
                    }

                    DamSeries rs;
                    string sc = oi.ofc + "-" + oi.obc;
                    string path = Path.Combine("Data", "Dam", sc + ".json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        rs = JsonConvert.DeserializeObject<DamSeries>(json);
                        rs.obn = oi.obn;  //名称は毎回確認

                        DateTime rsdt = rs.ot[SeriesNumber - 1];
                        int nt = (int)((doidt - rsdt).Ticks / 6000000000);
                        for (int n = 0; n < SeriesNumber - nt; n++)
                        {
                            rs.ot[n] = rs.ot[n + nt];
                            rs.d10_val[n] = rs.d10_val[n + nt];
                            rs.d10_si[n] = rs.d10_si[n + nt];
                            rs.d20_val[n] = rs.d20_val[n + nt];
                            rs.d20_si[n] = rs.d20_si[n + nt];
                            rs.d40_val[n] = rs.d40_val[n + nt];
                            rs.d40_si[n] = rs.d40_si[n + nt];
                            rs.d50_val[n] = rs.d50_val[n + nt];
                            rs.d50_si[n] = rs.d50_si[n + nt];
                            rs.d70_val[n] = rs.d70_val[n + nt];
                            rs.d70_si[n] = rs.d70_si[n + nt];
                            rs.d10010_10m_val[n] = rs.d10010_10m_val[n + nt];
                            rs.d10010_10m_si[n] = rs.d10010_10m_si[n + nt];
                            rs.d10010_1h_val[n] = rs.d10010_1h_val[n + nt];
                            rs.d10010_1h_si[n] = rs.d10010_1h_si[n + nt];
                            rs.d10070_val[n] = rs.d10070_val[n + nt];
                            rs.d10070_si[n] = rs.d10070_si[n + nt];
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
                            rs.d20_val[n] = null;
                            rs.d20_si[n] = -1;
                            rs.d40_val[n] = null;
                            rs.d40_si[n] = -1;
                            rs.d50_val[n] = null;
                            rs.d50_si[n] = -1;
                            rs.d70_val[n] = null;
                            rs.d70_si[n] = -1;
                            rs.d10010_10m_val[n] = null;
                            rs.d10010_10m_si[n] = -1;
                            rs.d10010_1h_val[n] = null;
                            rs.d10010_1h_si[n] = -1;
                            rs.d10070_val[n] = null;
                            rs.d10070_si[n] = -1;
                        }
                    }
                    else
                    {
                        rs = new DamSeries
                        {
                            mo = oi.ofc,
                            sc = sc,
                            obn = oi.obn,
                            ot = new DateTime[SeriesNumber],
                            d10_val = new double?[SeriesNumber],
                            d10_si = new int[SeriesNumber],
                            d20_val = new double?[SeriesNumber],
                            d20_si = new int[SeriesNumber],
                            d40_val = new double?[SeriesNumber],
                            d40_si = new int[SeriesNumber],
                            d50_val = new double?[SeriesNumber],
                            d50_si = new int[SeriesNumber],
                            d70_val = new double?[SeriesNumber],
                            d70_si = new int[SeriesNumber],
                            d10010_10m_val = new double?[SeriesNumber],
                            d10010_10m_si = new int[SeriesNumber],
                            d10010_1h_val = new double?[SeriesNumber],
                            d10010_1h_si = new int[SeriesNumber],
                            d10070_val = new double?[SeriesNumber],
                            d10070_si = new int[SeriesNumber]
                    };
                        DateTime dt = doidt.AddMinutes(-10 * SeriesNumber);
                        for (int n = 0; n < SeriesNumber; n++)
                        {
                            dt = dt.AddMinutes(10);
                            rs.ot[n] = dt;
                        }
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d10_si[n] = -1;
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d20_si[n] = -1;
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d40_si[n] = -1;
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d50_si[n] = -1;
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d70_si[n] = -1;
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d10010_10m_si[n] = -1;
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d10010_1h_si[n] = -1;
                        for (int n = 0; n < SeriesNumber; n++)
                            rs.d10070_si[n] = -1;
                    }

                    int sn = SeriesNumber - 1;

                    rs.ot[sn] = doidt;
                    rs.d10_val[sn] = Observation.StringToDouble(oi.odd.dd.d10_10m.ov);
                    rs.d10_si[sn] = oi.odd.dd.d10_10m.osi;
                    rs.d20_val[sn] = Observation.StringToDouble(oi.odd.dd.d20_10m.ov);
                    rs.d20_si[sn] = oi.odd.dd.d20_10m.osi; ;
                    rs.d40_val[sn] = Observation.StringToDouble(oi.odd.dd.d40_10m.ov);
                    rs.d40_si[sn] = oi.odd.dd.d40_10m.osi; ;
                    rs.d50_val[sn] = Observation.StringToDouble(oi.odd.dd.d50_10m.ov);
                    rs.d50_si[sn] = oi.odd.dd.d50_10m.osi; ;
                    rs.d70_val[sn] = Observation.StringToDouble(oi.odd.dd.d70_10m.ov);
                    rs.d70_si[sn] = oi.odd.dd.d70_10m.osi; ;
                    rs.d10010_10m_val[sn] = Observation.StringToDouble(oi.odd.dd.d10010_10m.ov);
                    rs.d10010_10m_si[sn] = oi.odd.dd.d10010_10m.osi; ;
                    rs.d10010_1h_val[sn] = Observation.StringToDouble(oi.odd.dd.d10010_1h.ov);
                    rs.d10010_1h_si[sn] = oi.odd.dd.d10010_1h.osi; ;
                    rs.d10070_val[sn] = Observation.StringToDouble(oi.odd.dd.d10070_10m.ov);
                    rs.d10070_si[sn] = oi.odd.dd.d10070_10m.osi; ;

                    //10分毎の時間雨量の計算
                    if (rs.d10070_si[sn] != 0)
                    {
                        rs.d10010_1h_val[sn] = null;
                        rs.d10010_1h_si[sn] = rs.d10070_si[sn];
                    }
                    else if (rs.d70_val[sn] == 0)
                    {
                        rs.d10010_1h_val[sn] = 0;
                        rs.d10010_1h_si[sn] = 0;
                    }
                    else if (rs.d10070_si[sn - 6] == 0)
                    {
                        rs.d10010_1h_val[sn] = rs.d10070_val[sn] - rs.d10070_val[sn - 6];
                        if (rs.d10010_1h_val[sn] < 0)
                            rs.d10010_1h_val[sn] = 0;
                        rs.d10010_1h_si[sn] = 0;
                    }
                    else
                    {
                        rs.d10010_1h_val[sn] = null;
                        rs.d10010_1h_si[sn] = rs.d10070_si[sn - 6];
                    }

                    //現況ダム情報一覧の作成
                    //正時しかデータを収集していない観測局があるので、正時まで有効なデータがないか検索している
                    //すべての観測局が収集しているのが放流量なので、d70で確認
              
                    DateTime odt = rs.ot[sn];
                    int nminute = doidt.Minute / 10;
                    bool flag = false;

                    for (; sn > SeriesNumber - 2 - nminute; sn--)
                    {
                        if (rs.d70_si[sn] == 0)
                        {
                            flag = true;
                            break; ;
                        }
                    }

                    if (!flag)
                        sn = SeriesNumber - 1;


                    damDataList.hr.Add(new DamData
                    {
                        mo = rs.mo,
                        sc = sc,
                        obn = oi.obn,
                        lat = rs.lat,
                        lng = rs.lng,
                        d10_val = rs.d10_val[sn],
                        d10_si = rs.d10_si[sn],
                        d20_val = rs.d20_val[sn],
                        d20_si = rs.d20_si[sn],
                        d40_val = rs.d40_val[sn],
                        d40_si = rs.d40_si[sn],
                        d50_val = rs.d50_val[sn],
                        d50_si = rs.d50_si[sn],
                        d70_val = rs.d70_val[sn],
                        d70_si = rs.d70_si[sn],
                        d10010_10m_val = rs.d10010_10m_val[sn],
                        d10010_10m_si = rs.d10010_10m_si[sn],
                        d10010_1h_val = rs.d10010_1h_val[sn],
                        d10010_1h_si = rs.d10010_1h_si[sn],
                        d10070_val = rs.d10070_val[sn],
                        d10070_si = rs.d10070_si[sn],
                        dt = rs.ot[sn]
                    });

                    oi.lat = rs.lat;
                    oi.lng = rs.lng;

                    File.WriteAllText(path, JsonConvert.SerializeObject(rs));
                    number++;
                }
                catch (Exception e1)
                {
                    LoggerClass.LogInfo("ダム情報累積データ作成エラー 観測所: " + oi.obn + " メッセージ: " + e1.Message);
                }
            }
            File.WriteAllText(Path.Combine("Data", "Dam", "DamData.json"), JsonConvert.SerializeObject(damDataList));
            Observation.SaveToXml(Path.Combine("data", "dam", "DamInfo.xml"), data, 0);
            File.WriteAllText(Path.Combine("data", "dam", "DamInfo.json"), JsonConvert.SerializeObject(data));

            File.WriteAllText(Path.Combine("data", "DamObservationTime.text"), observationDateTime.ToString());
            return number;
        }

        //累積データの観測所情報の更新
        public void SetDamInfo()
        {

            DamDocd data = Observation.TgGetStream<DamDocd>(DamInfoUrl, 0);
            if (data == null)
                return;

            string j = File.ReadAllText(Path.Combine("Config", "DamInfo.json"));
            DamStationList stationInfoList = JsonConvert.DeserializeObject<DamStationList>(j);

            //累積データヘッダー部分の修正
            foreach (var oi in data.oi)
            {
                try
                {
                    DamSeries rs;
                    string sc = oi.ofc + "-" + oi.obc;
                    string path = Path.Combine("Data", "Dam", sc + ".json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        rs = JsonConvert.DeserializeObject<DamSeries>(json);

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
                    LoggerClass.LogInfo("ダム情報修正エラー 観測所: " + oi.obn + " メッセージ: " + e1.Message);
                }
            }
        }
    }


    public class DamDataList
    {
        public DateTime dt { get; set; }
        public List<DamData> hr { get; set; }
    }

    public class DamData
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
        /// 貯水位
        public double? d10_val { get; set; }
        public int d10_si { get; set; }
        /// 貯水量
        public double? d20_val { get; set; }
        public int d20_si { get; set; }
        /// 貯水率
        public double? d40_val { get; set; }
        public int d40_si { get; set; }
        /// 流入量
        public double? d50_val { get; set; }
        public int d50_si { get; set; }
        /// 放流量
        public double? d70_val { get; set; }
        public int d70_si { get; set; }
        /// 10分流域平均雨量
        public double? d10010_10m_val { get; set; }
        public int d10010_10m_si { get; set; }
        /// 時間流域平均雨量
        public Double? d10010_1h_val { get; set; }
        public int d10010_1h_si { get; set; }
        /// 流域平均累計雨量
        public double? d10070_val { get; set; }
        public int d10070_si { get; set; }
        /// 観測時間
        public DateTime dt { get; set; }
    }

    //ダム情報時系列データ
    public class DamSeries
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
        /// 水系
        public string rsn { get; set; }
        /// 河川名
        public string rn { get; set; }
        /// グラフ最大値
        public string gmax { get; set; }
        /// グラフ最小値
        public string gmin { get; set; }
        /// <summary>
        /// 観測時間
        /// </summary>
        public DateTime[] ot { get; set; }
        /// 貯水位
        public double?[] d10_val { get; set; }
        public int[] d10_si { get; set; }
        /// 貯水量
        public double?[] d20_val { get; set; }
        public int[] d20_si { get; set; }
        /// 貯水率
        public double?[] d40_val { get; set; }
        public int[] d40_si { get; set; }
        /// 流入量
        public double?[] d50_val { get; set; }
        public int[] d50_si { get; set; }
        /// 放流量
        public double?[] d70_val { get; set; }
        public int[] d70_si { get; set; }
        /// 10分流域平均雨量
        public double?[] d10010_10m_val { get; set; }
        public int[] d10010_10m_si { get; set; }
        /// 時間流域平均雨量
        public Double?[] d10010_1h_val { get; set; }
        public int[] d10010_1h_si { get; set; }
        /// 流域平均累計雨量
        public double?[] d10070_val { get; set; }
        public int[] d10070_si { get; set; }
    }


    /// ダム情報
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
