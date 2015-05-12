// Copyright 2015 (c) Yasuhiro Niji
// Use of this source code is governed by the MIT License,
// as found in the LICENSE.txt file.
var riverUrl = "http://tk.ecitizen.jp/Data/River/";
function setRiver() {
    var xmlhttp = new XMLHttpRequest();
    xmlhttp.onreadystatechange = function () {
        if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
            var data = JSON.parse(xmlhttp.responseText);
            riverSummaryTable(data);
        }
    };
    xmlhttp.open("GET", riverUrl + "RiverData.json", true);
    xmlhttp.send();
}
function setRiverData() {
    var regex = new RegExp("[\\?&]station=([^&#]*)");
    var results = regex.exec(location.search);
    if (results === null)
        window.location.href = "River.html";
    var station = results[1];
    riverGetDetail(station);
}
function riverSummaryTable(data) {
    var out = "<table class='table table-bordered'>";
    out += "<tr><th>場所</th><th>水位</th><th>水位変化</th><th>観測時間<th>リンク</th></tr>";
    var i;
    for (i = 0; i < data.hr.length; i++) {
        out += '<tr><td>' + data.hr[i].obn + '</td><td>' + data.hr[i].d10_val + '</td><td>' + data.hr[i].d10_chg + '</td><td>' + data.hr[i].dt + '</td><td><a href="RiverData.html?station=' + data.hr[i].sc + '">リンク</a></td></tr>';
    }
    out += "<table>";
    out += "<p>データ: " + riverUrl + "RiverData.json" + "</p>";
    document.getElementById("id01").innerHTML = out;
}
function riverGetDetail(place) {
    var xmlhttp1 = new XMLHttpRequest();
    var url = riverUrl + place + ".json";
    xmlhttp1.onreadystatechange = function () {
        if (xmlhttp1.readyState == 4 && xmlhttp1.status == 200) {
            var data = JSON.parse(xmlhttp1.responseText);
            riverDetailTable(data, url);
        }
    };
    xmlhttp1.open("GET", url, true);
    xmlhttp1.send();
}
function riverDetailTable(data, url) {
    document.getElementById("place0").innerHTML = data.obn;
    var out = "<p>" + data.rsn + " " + data.rn + "</p>";
    out += "<p>所在地; " + data.obl + "</p>";
    if (data.spfw != null)
        out += "<p>水防団待機水位: " + data.spfw + "m</p>";
    if (data.cauw != null)
        out += "<p>はん濫注意水位: " + data.cauw + "m</p>";
    if (data.spcw != null)
        out += "<p>避難判断水位: " + data.spcw + "m</p>";
    if (data.danw != null)
        out += "<p>はん濫危険水位: " + data.danw + "m</p>";
    if (data.plaw != null)
        out += "<p>計画高水位: " + data.plaw + "m</p>";
    out += "<table class='table table-bordered'>";
    out += "<tr><th>観測時間</th><th>水位</th><th>ステータス</th></tr>";
    var i;
    for (i = 0; i < data.ot.length; i++) {
        out += '<tr><td>' + data.ot[i] + '</td><td>' + data.d10_val[i] + '</td><td>' + data.d10_si[i] + '</td><tr>';
    }
    out += "<table>";
    out += "<p>データ: " + url + "</p>";
    document.getElementById("id02").innerHTML = out;
}
