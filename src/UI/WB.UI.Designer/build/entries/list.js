//import '/node_modules/jquery/dist/jquery.js';
//import $ from '/node_modules/jquery/dist/jquery.js';
import $ from 'jquery';

import '/node_modules/jquery-mousewheel/jquery.mousewheel.js';
//import '/node_modules/perfect-scrollbar/dist/perfect-scrollbar.js';
import PerfectScrollbar from 'perfect-scrollbar';
//import perfectScrollbar from 'perfect-scrollbar';
//import perfectScrollbar from '/node_modules/perfect-scrollbar/dist/perfect-scrollbar.js';
//$.perfectScrollbar = perfectScrollbar();
//const lists = $('#table-content-holder > .scroller-container');
//lists.perfectScrollbar();
//const container = document.querySelector('#container');

/*if (lists.length > 0) {
    perfectScrollbar.initialize(
        $('#table-content-holder > .scroller-container')[0]
    );
}*/

window.jQuery = window.$ = $;

import moment from 'moment';
window.moment = moment;

import '/node_modules/perfect-scrollbar/css/perfect-scrollbar.css';
import '/questionnaire/content/designer-start/bootstrap-custom.less';
import '/Styles/designer-list.less';
import '/Scripts/custom/common.js';

import './simplepage';

$(function () {
    const lists = $('#table-content-holder > .scroller-container');
    if (lists.length > 0) {
        const ps = new PerfectScrollbar(
            '#table-content-holder > .scroller-container'
        );
    }

    if (window.moment) {
        moment.locale('@CultureInfo.CurrentUICulture');

        $('time').each(function () {
            var me = $(this);
            var date = me.attr('datetime') || me.attr('time');
            var momentDate = moment
                .utc(date)
                .local()
                .format('MMM DD, YYYY HH:mm');
            me.text(momentDate);
        });
    }
});
