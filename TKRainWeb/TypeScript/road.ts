interface RoadDataList {
    dt: string
    hr: RoadData[]
}

interface RoadData {
    /// 事務所コード
   ofc: number
        /// 観測局コード
   obc: number
        /// 観測局名称
   obn: string
        /// 緯度
   lat: number
        /// 経度
   lng: number
        /// 気温
   d10030_val: number
        /// 気温ステータス
   d10030_si: number
   /// 風速
   d10060_val: number
        /// 風速ステータス
   d10060_si: number
        /// 風向
   d10070_val: string
        /// 風向ステータス
   d10070_si: number
        /// 観測時間
   dt: string
}

interface RoadSeries {
    /// 事務所コード
    ofc:number
        /// 観測局コード
    obc: number
        /// 観測局名称
    obn: string
        // 観測時間
    ot: string[]
        // 気温
    d10030_val: number[]
    d10030_si: number[]
        // 風速
    d10060_val: number[]
    d10060_si: number[]
        // 風向
    d10070_val: string[]
    d10070_si: number[]
}


var xmlhttp = new XMLHttpRequest();
var url = "http://tk.ecitizen.jp/Data/Road/RoadData.json";
var url0 = "http://tk.ecitizen.jp/Data/Road/"

xmlhttp.onreadystatechange = function () {
    if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
        var data = JSON.parse(xmlhttp.responseText);
        roadSummaryTable(data, url);
    }
}
xmlhttp.open("GET", url, true);
xmlhttp.send();

function roadSummaryTable(data: RoadDataList, url: string) {
    var out = "<table class='table table-bordered'>";
    out += "<tr><th>場所</th><th>気温</th><th>風向</th><th>風速</th><th>観測時間</th><th>Jsonデータ</th></tr>";
    var i;
    for (i = 0; i < data.hr.length; i++) {
        out += '<tr><td>' + data.hr[i].obn + '</td><td>' + data.hr[i].d10030_val + '</td><td>' + data.hr[i].d10070_val +
        '</td><td>' + data.hr[i].d10060_val + '</td><td>' + data.hr[i].dt +
        '</td><td>' + url0 + data.hr[i].ofc + '-' + data.hr[i].obc + '.json' + '</td></tr>';
    }
    out += "<table>";
    out += "<p>データ: " + url + "</p>";
    document.getElementById("id01").innerHTML = out;
    roadGetDetail(data.hr[0].ofc + '-' + data.hr[0].obc);
}

function roadGetDetail(place: string)
{
    var xmlhttp1 = new XMLHttpRequest();
    var url1 = url0 + place + ".json";

    xmlhttp1.onreadystatechange = function () {
        if (xmlhttp1.readyState == 4 && xmlhttp1.status == 200) {
            var data = JSON.parse(xmlhttp1.responseText);
            roadDetailTable(data, url1);
        }
    }
    xmlhttp1.open("GET", url1, true);
    xmlhttp1.send();
}

function roadDetailTable(data: RoadSeries, url: string) {
    document.getElementById("place0").innerHTML = data.obn;
    var out = "<table class='table table-bordered'>";
    out += "<tr><th>観測時間</th><th>気温</th><th>風向</th><th>風速</th></tr>";
    var i;
    for (i = 0; i < data.ot.length; i++) {
        out += '<tr><td>' + data.ot[i] + '</td><td>' + data.d10030_val[i] + '</td><td>' + data.d10070_val[i] + '</td><td>' + data.d10060_val[i] + '</td></tr>';
    }
    out += "<table>";
    out += "<p>データ: " + url + "</p>";
    document.getElementById("id02").innerHTML = out;
}