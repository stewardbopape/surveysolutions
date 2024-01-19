import { mande } from 'mande';
import { useBlockUIStore } from '../stores/blockUI';
import { useProgressStore } from '../stores/progress';
import { isNull } from 'lodash';

const api = mande('/' /*, globalOptions*/);

export function get(url, queryParams) {
    const progressStore = useProgressStore();
    const blockUI = useBlockUIStore();

    blockUI.start();
    progressStore.start();

    if (queryParams) {
        return api
            .get(url, {
                query: queryParams
            })
            .then(response => {
                blockUI.stop();
                progressStore.stop();
                return response;
            })
            .catch(error => {
                blockUI.stop();
                progressStore.stop();
            });
    }

    return api
        .get(url)
        .then(response => {
            blockUI.stop();
            progressStore.stop();
            return response;
        })
        .catch(error => {
            blockUI.stop();
            progressStore.stop();
        });
}

export function post(url, params) {
    const progressStore = useProgressStore();
    const blockUI = useBlockUIStore();

    const headers = getHeaders();
    blockUI.start();
    progressStore.start();
    return api
        .post(url, params, { headers: headers })
        .then(response => {
            blockUI.stop();
            progressStore.stop();
            return response;
        })
        .catch(error => {
            blockUI.stop();
            progressStore.stop();
        });
}

export function patch(url, params) {
    const progressStore = useProgressStore();
    const blockUI = useBlockUIStore();

    const headers = getHeaders();

    blockUI.start();
    progressStore.start();

    return api
        .patch(url, params, { headers: headers })
        .then(response => {
            blockUI.stop();
            progressStore.stop();
            return response;
        })
        .catch(error => {
            blockUI.stop();
            progressStore.stop();
        });
}

export function del(url, params) {
    const progressStore = useProgressStore();
    const blockUI = useBlockUIStore();

    const headers = getHeaders();
    blockUI.start();
    progressStore.start();
    return api
        .delete(url, params, { headers: headers })
        .then(response => {
            blockUI.stop();
            progressStore.stop();
            return response;
        })
        .catch(error => {
            blockUI.stop();
            progressStore.stop();
        });
}

export function commandCall(commandType, command) {
    return post('/api/command', {
        type: commandType,
        command: JSON.stringify(command)
    });
}

export function upload(url, file, command) {
    const progressStore = useProgressStore();
    const blockUI = useBlockUIStore();

    progressStore.start();
    blockUI.start();

    const api = mande(url, { headers: { 'Content-Type': null } });

    const formData = new FormData();
    formData.append('file', isNull(file) ? '' : file);
    formData.append(
        'command',
        isNull(command) ? null : JSON.stringify(command)
    );

    return api
        .post(formData)
        .then(response => {
            blockUI.stop();
            progressStore.stop();

            return response;
        })
        .catch(err => {
            blockUI.stop();
            progressStore.stop();

            throw err;
        });
}

function getHeaders() {
    return {
        'Content-Type': 'application/json',
        Accept: 'application/json',
        'X-CSRF-TOKEN': getCsrfCookie()
    };
}

function getCsrfCookie() {
    var name = 'CSRF-TOKEN-D=';
    var decodedCookie = decodeURIComponent(document.cookie);
    var ca = decodedCookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return '';
}
