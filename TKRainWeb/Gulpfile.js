/*
This file in the main entry point for defining Gulp tasks and using Gulp plugins.
Click here to learn more. http://go.microsoft.com/fwlink/?LinkId=518007
*/

var gulp = require('gulp');
var clean = require('gulp-clean');
var concat = require('gulp-concat');
var jshint = require('gulp-jshint');
var uglify = require('gulp-uglify');
var rename = require('gulp-rename');
var minify = require('gulp-minify-css');

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
        .pipe(minify())
        .pipe(rename({ extname: '.min.css' }))
        .pipe(gulp.dest('wwwroot/css'))
})

gulp.task("watcher", function () {
    gulp.watch("TypeScript/*.js", ['TypeScript']);
});