// Copyright 2015 (c) Yasuhiro Niji
// Use of this source code is governed by the MIT License,
// as found in the LICENSE.txt file.

interface RainDataList {
    dt: string
    hr: RainData[]
}

interface RainData {
    /// 事務所コード
    mo: number
    /// 観測局コード
    sc: string
    /// 観測局名称
    obn: string
    /// 緯度
    lat: number
    /// 経度
    lng: number
    /// 時間雨量
    d10_1h_val: number
    /// 時間雨量ステータス
    d10_1h_si: number
    /// 累計雨量
    d70_10m_val: number
    /// 累計雨量ステータス
    d70_10m_si: number
    /// 観測時間
    dt: string
}

interface RainSeries {
    /// 事務所コード
    mo: number
    /// 観測局コード
    sc: string
    /// 観測局名称
    obn: string
    /// 所在地
    obl: string
    // 観測時間
    ot: string[]
    // 10分雨量
    d10_10m_val: number[]
    d10_10m_si: number[]
    // 時間雨量
    d10_1h_val: number[]
    d10_1h_si: number[]
    // 累計雨量
    d70_10m_val: number[]
    d70_10m_si: number[]
}

var rainUrl = "http://tk.ecitizen.jp/Data/Rain/";

function setRain() {
    var xmlhttp = new XMLHttpRequest();
    xmlhttp.onreadystatechange = function () {
        if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
            var data = JSON.parse(xmlhttp.responseText);
            rainSummaryTable(data, rainUrl + "RainData.json");
        }
    }
    xmlhttp.open("GET", rainUrl + "RainData.json", true);
    xmlhttp.send();
}

function setRainData() {
    var regex = new RegExp("[\\?&]station=([^&#]*)");
    var results = regex.exec(location.search);
    if (results === null)
        window.location.href = "Rain.html"; 
    var station = results[1]; 
    rainGetDetail(station);
}

function rainSummaryTable(data: RainDataList, url: string) {
    var out = "<table class='table table-bordered'>";
    out += "<tr><th>場所</th><th>時間雨量</th><th>累計雨量</th><th>観測時間<th>リンク</th></tr>";
    var i;
    for (i = 0; i < data.hr.length; i++) {
        out += '<tr><td>' + data.hr[i].obn + '</td><td>' + data.hr[i].d10_1h_val + '</td><td>' + data.hr[i].d70_10m_val +
        '</td><td>' + data.hr[i].dt +
        '</td><td><a href="RainData.html?station=' + data.hr[i].sc + '">リンク</a></td></tr>';
    }
    out += "<table>";
    out += "<p>データ: " + url + "</p>";
    document.getElementById("id01").innerHTML = out;
}

function rainGetDetail(place: string) {
    var xmlhttp1 = new XMLHttpRequest();
    var url = rainUrl + place + ".json";

    xmlhttp1.onreadystatechange = function () {
        if (xmlhttp1.readyState == 4 && xmlhttp1.status == 200) {
            var data = JSON.parse(xmlhttp1.responseText);
            rainDetailTable(data, url);
        }
    }
    xmlhttp1.open("GET", url, true);
    xmlhttp1.send();
}

function rainDetailTable(data: RainSeries, url: string) {
    document.getElementById("place0").innerHTML = data.obn;
    var out = "<p>所在地; " + data.obl + "</p>";
    out += "<table class='table table-bordered'>";
    out += "<tr><th>観測時間</th><th>10分雨量</th><th>ステータス</th><th>時間雨量</th><th>ステータス</th><th>累計雨量</th><th>ステータス</th></tr>";
    var i;
    for (i = 0; i < data.ot.length; i++) {
        out += '<tr><td>' + data.ot[i] + '</td><td>' + data.d10_10m_val[i] + '</td><td>' + data.d10_10m_si[i] +
        '</td><td>' + data.d10_1h_val[i] + '</td><td>' + data.d10_1h_si[i] +
        '</td><td>' + data.d70_10m_val[i] + '</td><td>' + data.d70_10m_si[i] + '</td></tr>';
    }
    out += "<table>";
    out += "<p>データ: " + url + "</p>";
    document.getElementById("id02").innerHTML = out;
}