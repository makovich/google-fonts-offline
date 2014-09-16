@font-face {
    font-family: '{{fontFamily}}';
    font-style: {{fontStyle}};
    font-weight: {{fontWeight}};
    src: url({{fontFilename}}.eot); /* {{embedded-opentype-gf-url}} */
    src: local('☺'),
        url({{fontFilename}}.eot?#iefix) format('embedded-opentype'), /* {{embedded-opentype-gf-url}} */
        url({{fontFilename}}.woff) format('woff'), /* {{woff-gf-url}} */
        url({{fontFilename}}.ttf) format('truetype'), /* {{truetype-gf-url}} */
        url({{fontFilename}}.svg#{{fontFilename}}) format('svg'); /* {{svg-gf-url}} */
}
