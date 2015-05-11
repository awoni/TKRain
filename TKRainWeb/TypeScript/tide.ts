// Copyright 2015 (c) Yasuhiro Niji
// Use of this source code is governed by the MIT License,
// as found in the LICENSE.txt file.

interface TideDataList {
    dt: string
    hr: TideData[]
}

interface TideData {
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
        /// 潮位
   level: number
        /// 観測時間
   dt: string
}

interface TideSeries {
    /// 事務所コード
    mo:number
        /// 観測局コード
    sc: string
        /// 観測局名称
    obn: string
        // 観測時間
    ot: string[]
        // 潮位
    level: number[]
}

var tideUrl = "http://tk.ecitizen.jp/Data/Tide/";

function setTide() {
    var xmlhttp = new XMLHttpRequest();
    xmlhttp.onreadystatechange = function () {
        if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
            var data = JSON.parse(xmlhttp.responseText);
            tideSummaryTable(data);
        }
    }
    xmlhttp.open("GET", tideUrl + "TideData.json", true);
    xmlhttp.send();
}

function setTideData() {
    var regex = new RegExp("[\\?&]station=([^&#]*)"),
    results = regex.exec(location.search);
    if (results === null)
        window.location.href = "Tide.html";
    tideGetDetail(results[1]);
}

function tideSummaryTable(data: TideDataList) {
    var out = "<table class='table table-bordered'>";
    out += "<tr><th>場所</th><th>潮位</th><th>観測時間</th><th>リンク</th></tr>";
    var i;
    for (i = 0; i < data.hr.length; i++) {
        out += '<tr><td>' + data.hr[i].obn + '</td><td>' + data.hr[i].level +
        '</td><td>' + data.hr[i].dt +
        '</td><td><a href="TideData.html?station=' + data.hr[i].sc + '">リンク</a></td></tr>';
    }
    out += "<table>";
    out += "<p>データ: " + tideUrl + "TideData.json" + "</p>";
    document.getElementById("id01").innerHTML = out;
}

function tideGetDetail(place: string)
{
    var xmlhttp = new XMLHttpRequest();
    var url = tideUrl + place + ".json";

    xmlhttp.onreadystatechange = function () {
        if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
            var data = JSON.parse(xmlhttp.responseText);
            tideDetailTable(data, url);
        }
    }
    xmlhttp.open("GET", url, true);
    xmlhttp.send();
}

function tideDetailTable(data: TideSeries, url: string) {
    document.getElementById("place0").innerHTML = data.obn;
    var out = "<table class='table table-bordered'>";
    out += "<tr><th>観測時間</th><th>潮位</th></tr>";
    var i;
    for (i = data.ot.length - 144; i < data.ot.length; i++) {
        out += '<tr><td>' + data.ot[i] + '</td><td>' + data.level[i]  + '</td></tr>';
    }
    out += "<table>";
    out += "<p>データ: " + url + "</p>";
    document.getElementById("id02").innerHTML = out;
}