var fs = require('fs'),
    path = require('path'),
    stream = require('stream'),
    url = require('url'),
    http = require('http'),

    //                                            Group[0]
    //                                            vvvvvvvv
    fontFaceRulePattern = /(?:@font-face\s*\{\s*)([^\{\}]+)/gi,
    //
    //                             Group[1]                               Group[2]
    //                             vvvvvv                                 vvvvvv
    fontUrlPattern = /url\([\'\"]?([^\)]+?)[\'\"]?\)(?:\s*format\([\'\"]?([^\)]+?)[\'\"]?\))/gi,
    //
    //                                             Group[1]
    //                   Group[0]                  vvvvvvv
    //                   vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
    fontFamilyPattern = /font-family\s*\:\s*[\'"]?([^\;]+?)[\'"]?\;/i,
    fontWeightPattern = /font-weight\s*\:\s*[\'"]?([^\;]+?)[\'"]?\;/i,
    fontStylePattern =   /font-style\s*\:\s*[\'"]?([^\;]+?)[\'"]?\;/i,

    scriptOutputDir = 'fonts',
    cssFilename = 'fonts.css',

    fontFaceBulletproofStyle = fs.readFileSync(path.join(path.dirname(fs.realpathSync(__filename)), 'fontface.css.tpl')).toString();

    fileExts = {
        "embedded-opentype" : ".eot",
        "woff" : ".woff",
        "truetype" : ".ttf",
        "svg" : ".svg"
    },
    userAgents = [
        // EOT lives in IE
        "Mozilla/5.0 (MSIE 9.0; Windows NT 6.1; Trident/5.0)",

        // WOFF for Windows
        "Mozilla/5.0 (Windows NT 6.1; rv:2.0.1) Gecko/20100101 Firefox/4.0.1",
        
        // TTF needs Opera
        "Opera/9.80 (Macintosh; Intel Mac OS X; U; en) Presto/2.2.15 Version/10.00",

        // SVG loves iPad
        "Mozilla/5.0 (iPad; U; CPU OS 3_2 like Mac OS X; en-us) AppleWebKit/531.21.10 (KHTML, like Gecko) Version/4.0.4 Mobile/7B334b Safari/531.21.10"

        // WOFF for *nix ("Mozilla/5.0 (Linux; Android 4.1.1; Nexus 7 Build/JRO03D) AppleWebKit/535.19 (KHTML, like Gecko) Chrome/18.0.1025.166  Safari/535.19")
        // http://michaelboeke.com/blog/2013/09/10/Self-hosting-Google-web-fonts/

        // WOFF2 for Chrome ("Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/37.0.2062.103")
    ],

    fontfaceList = {},
    requestsInProgress = 0,

    urlToProcess = '',

    httpOptions = {
        'hostname' : '',
        'path' : '',
        'headers' : {}
    };

exports.setOutputDir = function (dirName) {
    scriptOutputDir = dirName || scriptOutputDir;
};

exports.setCssFilename = function (filename) {
    cssFilename = filename || cssFilename;
};

exports.download = function (urlToProcess) {

    if (!urlToProcess) {
        console.log('No URL to process! Use something like this: http://fonts.googleapis.com/css?family=Open+Sans|Roboto');
        process.exit(0);
    }

    httpOptions.hostname = url.parse(urlToProcess).hostname;
    httpOptions.path = url.parse(urlToProcess).path;

    createOutputDir(scriptOutputDir);
    processGoogleFontsUrl(urlToProcess);

    (function awaitHttpRequests () {

        setTimeout(function () {

            requestsInProgress > 0 ? awaitHttpRequests() : buildOutputCssFile();

        }, 50);

    }());
};

function createOutputDir (dirName) {
    var dirPath = path.join(process.cwd(), dirName);

    fs.exists(dirPath, function (exists) {
        if(!exists) fs.mkdirSync(dirPath);
    });
}

function processGoogleFontsUrl (userAgent) {

    if (!userAgent) return;

    console.log("Mimics to %s...", userAgent);

    requestsInProgress += 1;

    httpCallback = function (response) {
        var buffer = [];

        response.on('data', function (chunk) {
            buffer.push(chunk);
        });

        response.on('end', function () {
            var css = Buffer.concat(buffer).toString();
            
            css.match(fontFaceRulePattern).forEach(processFontface);

            requestsInProgress -= 1;

            processGoogleFontsUrl(userAgents.shift());
        });
    };

    httpOptions.headers['User-Agent'] = userAgent;

    http.get(httpOptions, httpCallback)
        .on('error', handleHttpError);
}

function processFontface (css) {
    var urlMatches,
        fontKey = composeFontFaceKey(css);

    while ((urlMatches = fontUrlPattern.exec(css)) !== null) {

        var fontUrlWithFormat = urlMatches[2] + '|' + urlMatches[1],
            filename = fontKey.replace(/\s/g, '+').replace(/:/g, '_'),
            extension = fileExts[urlMatches[2]];

        // There is no font in collection with such a fontKey
        if (!fontfaceList[fontKey]) {
            downloadFont(urlMatches[1], filename + extension);
            fontfaceList[fontKey] = [fontUrlWithFormat];
            continue;
        }

        // There is no font url in collection already
        if (fontfaceList[fontKey].indexOf(fontUrlWithFormat) === -1) {
            downloadFont(urlMatches[1], filename + extension);
            fontfaceList[fontKey].push(fontUrlWithFormat);
        }
    }
}

function composeFontFaceKey(css) {
    var family = css.match(fontFamilyPattern)[1],
        weight = css.match(fontWeightPattern)[1],
        style =  css.match(fontStylePattern)[1];

    return family + ':' + weight + ':' + style;
}

function downloadFont(url, filename) {
    
    console.log("  Downloading %s...", filename);

    requestsInProgress += 1;

    http.get(url, function (response) {
        var buffer = [];

        response.on('data', function (chunk) {
            buffer.push(chunk);
        });

        response.on('end', function () {
            fs.writeFile(path.join(scriptOutputDir, filename), Buffer.concat(buffer));
            requestsInProgress -= 1;
        });
    })
    .on('error', handleHttpError);
}

function buildOutputCssFile () {
    var i,
        fontKey,
        splittedArray,
        fontFamily,
        fontWeight,
        fontStyle,
        fontFormat,
        fontGoogleFontsUrl,
        outputCss = '';

    console.log('Composing CSS file...');

    for (fontKey in fontfaceList) {
        splittedArray = fontKey.split(':');

        fontFamily = splittedArray[0];
        fontWeight = splittedArray[1];
        fontStyle = splittedArray[2];

        fontFilename = fontKey.replace(/\s/g, '+').replace(/:/g, '_');

        outputCss = fontFaceBulletproofStyle
                        .replace(/\{\{fontFilename\}\}/g, fontFilename)
                        .replace(/\{\{fontFamily\}\}/g, fontFamily)
                        .replace(/\{\{fontWeight\}\}/g, fontWeight)
                        .replace(/\{\{fontStyle\}\}/g, fontStyle);

        for (i = fontfaceList[fontKey].length - 1; i >= 0; i--) {
            splittedArray = fontfaceList[fontKey][i].split('|');
            
            fontFormat = splittedArray[0];
            fontGoogleFontsUrl = splittedArray[1];

            outputCss = outputCss
                            .replace(new RegExp('\\{\\{' + fontFormat + '-gf-url\\}\\}', 'g'), fontGoogleFontsUrl);
        }

        // Should be sync! Fontface declarations order matters!
        fs.appendFileSync(path.join(scriptOutputDir, cssFilename), outputCss);
    }

    console.log('Done!');
}

function handleHttpError (error) {
    requestsInProgress -= 1;
    console.log('Request error: %s', error.message);
}
