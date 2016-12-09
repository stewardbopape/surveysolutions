﻿var gulp = require('gulp'),
    plugins = require('gulp-load-plugins')(),
    concat = require('gulp-concat'),
    uglify = require('gulp-uglify'),
    mainBowerFiles = require('main-bower-files'),
    sass = require('gulp-sass'),
    autoprefixer = require('gulp-autoprefixer'),
    cssnano = require('gulp-cssnano'),
    util = require('gulp-util'),
    debug = require('gulp-debug'),
    rename = require('gulp-rename');

var config = {
    production: !!util.env.production,
    buildDir: './build',
    filesToInject: [
        {
            file: "LogOn.cshtml",
            folder: '../Views/Account/'
        },
        {
            file: "_MainLayout.cshtml",
            folder: '../Views/Shared/'
        }
    ],
    cssFilesToWatch: './css/*.scss"',
    cssSource: './css/markup.scss',
    cssAppInject: 'cssApp',
    cssLibsInject: 'cssLibs',
    jsAppInject: 'jsApp',
    jsLibsInject: 'jsLibs'
};


gulp.task('inject-css', ['styles', 'bowerCss'], function () {
    if (config.production) {
        var cssApp = gulp.src(config.buildDir + '/markup-*.min.css', { read: false });
        var cssLibs = gulp.src(config.buildDir + '/libs-*.min.css', { read: false });

        var tasks = config.filesToInject.map(function (fileToInject) {
            var target = gulp.src(fileToInject.folder + fileToInject.file);

            return target
                .pipe(plugins.inject(cssApp, { relative: true, name: config.cssAppInject }))
                .pipe(plugins.inject(cssLibs, { relative: true, name: config.cssLibsInject }))
                .pipe(gulp.dest(fileToInject.folder));
        });

        return tasks;
    }

    return util.noop();
});

gulp.task('inject-js', ['inject-css', 'bowerJs'], function () {
    if (config.production) {
        var jsApp = gulp.src(config.buildDir + '/app-*.min.js', { read: false });
        var jsLibs = gulp.src(config.buildDir + '/libs-*.min.js', { read: false });

        var tasks = config.filesToInject.map(function (fileToInject) {
            var target = gulp.src(fileToInject.folder + fileToInject.file);

            return target
                .pipe(plugins.inject(jsLibs, { relative: true, name: config.jsLibsInject }))
                .pipe(plugins.inject(jsApp, { relative: true, name: config.jsAppInject }))
                .pipe(gulp.dest(fileToInject.folder));
        });
        return tasks;
    }

    return util.noop();
});

gulp.task('styles', function () {
    return gulp.src(config.cssSource)
        .pipe(sass())
        .pipe(autoprefixer('last 2 version'))
        .pipe(gulp.dest(config.buildDir))
        .pipe(rename({ suffix: '.min' }))
        .pipe(plugins.rev())
        .pipe(cssnano())
    	.pipe(gulp.dest(config.buildDir));
});

gulp.task('watch-styles', function () {
    gulp.watch(config.cssFilesToWatch, ['styles']);
});

gulp.task('bowerJs', function () {
    return gulp.src(mainBowerFiles('**/*.js'))
        .pipe(plugins.ngAnnotate())
      	.pipe(concat('libs.js'))
        .pipe(gulp.dest(config.buildDir))
        .pipe(rename({ suffix: '.min' }))
        .pipe(plugins.uglify())
        .pipe(plugins.rev())
    	.pipe(gulp.dest(config.buildDir));
});

gulp.task('bowerCss', function () {
    return gulp.src(mainBowerFiles('**/*.css'))
        .pipe(autoprefixer('last 2 version'))
        .pipe(concat('libs.css'))
        .pipe(gulp.dest(config.buildDir))
        .pipe(rename({ suffix: '.min' }))
        .pipe(cssnano())
        .pipe(plugins.rev())
    	.pipe(gulp.dest(config.buildDir));
});

gulp.task('clean', function () {
    return gulp.src(config.buildDir + '/*').pipe(plugins.clean());
});

gulp.task('default', ['clean'], function () {
    gulp.start(/*'watch-styles', */'styles', 'bowerCss', 'bowerJs', 'inject-css', 'inject-js');
});