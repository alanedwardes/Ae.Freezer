const fs = require('fs');
const path = require('path');

let getContent = (path) => {
    const data = fs.readFileSync(path, 'utf8');
    return JSON.parse(data);
}

let safeResolve = (base, target) => {
    const targetPath = '.' + path.posix.normalize('/' + target);
    return path.posix.resolve(base, targetPath);
}

let getContentPath = (uri) => {
    const correctedUri = uri.endsWith('/') ? uri + 'index' : uri;
    return safeResolve('content', correctedUri);
}

let generateRedirect = (uri) => {
    return {
        status: '301',
        headers: {
            location: [{
                key: 'Location',
                value: uri
            }]
        }
    };
}

exports.handler = async (event, context) => {
    const request = event.Records[0].cf.request;

    try {
        // Try to get the content using this URI
        return getContent(getContentPath(request.uri));
    }
    catch (err) {
        // Not found, try the compensation workflow
    }

    try {
        const adjustedUri = request.uri + '/';

        // Try to get the same content with a trailing slash
        getContent(getContentPath(adjustedUri));

        // It exists, redirect to it
        return generateRedirect(adjustedUri);
    }
    catch (err) {
        // Not found, serve a 404 response
    }

    return getContent('content/errors/404');
};
