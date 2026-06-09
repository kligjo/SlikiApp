/**
 * TranslationsHierarchy Interceptor
 *
 * Intercepts translation dictionary related calls to allow an hierarchical representation of the keys without conflicts.
 */

(function () {

    'use strict';

    angular.module('umbraco.services').config(function ($httpProvider) {

    $httpProvider.interceptors.push(function ($q, $injector) {
        return {
            'request': function(request) {
                // Intercept Dictionary Create request to append parent key prefix
                if(request.url.toLowerCase().startsWith('/umbraco/backoffice/umbracoapi/dictionary/create')) {
                    const parentIdMatch = new RegExp('parentId=(-?[0-9]+)', 'i').exec(request.url);
                    const keyMatch = new RegExp('key=([^&?=]+)', 'i').exec(request.url);
                    if (parentIdMatch && parentIdMatch[1] && keyMatch && keyMatch[1]) {
                        const parentId = parseInt(parentIdMatch[1]);
                        const key = keyMatch[1];

                        if (parentId > -1) {
                            const dictionaryResource = $injector.get('dictionaryResource');
                            return dictionaryResource.getById(parentId)
                                .then(function (data) {
                                    const parentKeyPrefix = data.name + '.';
                                    if (!key.startsWith(parentKeyPrefix)) {
                                        request.url = request.url.replace('key=' + key, 'key=' + parentKeyPrefix + key);
                                    }
                                    return request;
                                });
                        }
                    }
                }
                return request;
            },
            'response': function (response) {
                // Intercept DictionaryTree GetNodes response data to hide parent key prefix
                if (response.config.url.toLowerCase().startsWith('/umbraco/backoffice/umbracotrees/dictionarytree/getnodes')) {
                    if (response.data && response.data instanceof Array) {
                        const parentIdMatch = new RegExp('id=(-?[0-9]+)', 'i').exec(response.config.url);
                        if (parentIdMatch && parentIdMatch[1]) {
                            const parentId = parseInt(parentIdMatch[1]);

                            if (parentId > -1) {
                                const dictionaryResource = $injector.get('dictionaryResource');
                                return dictionaryResource.getById(parentId)
                                    .then(function (data) {
                                        const parentKey = data.name;

                                        response.data.forEach(function(e) {
                                            const prefix = parentKey + '.';
                                            if (e.name && e.name.startsWith(prefix)) {
                                                e.name = e.name.replace(prefix, '');
                                            } else {
                                                e.name = '#' + e.name;
                                            }
                                        });
                                        return response;
                                    });
                            }
                        }
                    };
                }
                return response;
            }
        };
    });
});

})();
