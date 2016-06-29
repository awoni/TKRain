// Copyright 2015 (c) Yasuhiro Niji
// Use of this source code is governed by the MIT License,
// as found in the LICENSE.txt file.
var roadUrl = "http://tk.ecitizen.jp/Data/Road/";
function setRoad() {
    var xmlhttp = new XMLHttpRequest();
    xmlhttp.onreadystatechange = function () {
        if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
            var data = JSON.parse(xmlhttp.responseText);
            roadSummaryTable(data);
        }
    };
    xmlhttp.open("GET", roadUrl + "RoadData.json", true);
    xmlhttp.send();
}
function setRoadData() {
    var regex = new RegExp("[\\?&]station=([^&#]*)"), results = regex.exec(location.search);
    if (results === null)
        window.location.href = "Road.html";
    roadGetDetail(results[1]);
}
function roadSummaryTable(data) {
    var out = "<table class='table table-bordered'>";
    out += "<tr><th>場所</th><th>気温</th><th>風向</th><th>風速</th><th>観測時間</th><th>リンク</th></tr>";
    var i;
    for (i = 0; i < data.hr.length; i++) {
        out += '<tr><td>' + data.hr[i].obn + '</td><td>' + data.hr[i].d10030_val + '</td><td>' + data.hr[i].d10070_val +
            '</td><td>' + data.hr[i].d10060_val + '</td><td>' + data.hr[i].dt +
            '</td><td><a href="RoadData.html?station=' + data.hr[i].sc + '">リンク</a></td></tr>';
    }
    out += "<table>";
    out += "<p>データ: " + roadUrl + "RoadData.json" + "</p>";
    document.getElementById("id01").innerHTML = out;
}
function roadGetDetail(place) {
    var xmlhttp = new XMLHttpRequest();
    var url = roadUrl + place + ".json";
    xmlhttp.onreadystatechange = function () {
        if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
            var data = JSON.parse(xmlhttp.responseText);
            roadDetailTable(data, url);
        }
    };
    xmlhttp.open("GET", url, true);
    xmlhttp.send();
}
function roadDetailTable(data, url) {
    document.getElementById("place0").innerHTML = data.obn;
    var out = "<p>所在地; " + data.obl + "</p>";
    out += "<table class='table table-bordered'>";
    out += "<tr><th>観測時間</th><th>気温</th><th>風向</th><th>風速</th></tr>";
    var i;
    for (i = 0; i < data.ot.length; i++) {
        out += '<tr><td>' + data.ot[i] + '</td><td>' + data.d10030_val[i] + '</td><td>' + data.d10070_val[i] + '</td><td>' + data.d10060_val[i] + '</td></tr>';
    }
    out += "<table>";
    out += "<p>データ: " + url + "</p>";
    document.getElementById("id02").innerHTML = out;
}
