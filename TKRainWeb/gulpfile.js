/// <binding Clean='clean' />
"use strict";
var gulp = require('gulp');
var rimraf = require("rimraf");
var concat = require('gulp-concat');
var jshint = require('gulp-jshint');
var uglify = require('gulp-uglify');
var rename = require('gulp-rename');
var cssmin = require("gulp-cssmin");

gulp.task('default', function () {
    gulp.src(['bower_components/bootstrap/dist/*/*'])
        .pipe(gulp.dest('wwwroot/lib/bootstrap'))

});

gulp.task("TypeScript", function () {
    gulp.src(['TypeScript/*.js'])
        .pipe(jshint())
        .pipe(uglify())
        .pipe(rename({ extname: '.min.js' }))
        .pipe(gulp.dest('wwwroot/lib'))
})

gulp.task("css", function () {
    gulp.src(['css/*.css'])
        .pipe(cssmin())
        .pipe(rename({ extname: '.min.css' }))
        .pipe(gulp.dest('wwwroot/css'))
})

gulp.task("watcher", function () {
    gulp.watch("TypeScript/*.js", ['TypeScript']);
});
