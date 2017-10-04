import "babel-polyfill";
global.jQuery = require('jquery');

import { assetsPath, locale } from "./config"
import { browserLanguage } from "shared/helpers"

if (process.env.NODE_ENV === "production") {
    __webpack_public_path__ = assetsPath
}

import * as moment from 'moment'
moment.locale(browserLanguage);

import Vue from "vue"
import VueI18n from "./locale"

Vue.use(VueI18n, {
    defaultNS: 'WebInterviewUI',
    ns: ['WebInterviewUI', 'Common'],
    nsSeparator: '.',
    resources: {
        'en': locale
    }
})

import config from "shared/config"
Vue.use(config)

import * as poly from "smoothscroll-polyfill"
poly.polyfill()

import "./misc/audioRecorder.js"
import "./misc/htmlPoly.js"
import "./components"
import "shared/components/questions"
import "shared/components/questions/parts"
import "./directives"

import "./errors"
import router from "./router"
import store from "./store"

import App from "./App"

const vueApp = new Vue({
    el: "#app",
    render: h => h(App),
    components: { App },
    store,
    router
})
