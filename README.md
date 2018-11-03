# Google Fonts Offline

[![NPM](https://nodei.co/npm/google-fonts-offline.png?mini=true)](https://nodei.co/npm/google-fonts-offline/)

## Getting Started

Sometimes you just have to have fonts locally on your machine. The easiest way is to install package globally:

```
npm install -g google-fonts-offline
```

Then use links from Quick Use or your collection as an argument:

```
goofoffline "http://fonts.googleapis.com/css?family=Open+Sans"
```

All downloads will be saved in `fonts` directory by default and the CSS file with `@font-face` declarations will be named `fonts.css`. You can use `outDir` and `outCss` arguments to change this. For example:

```
goofoffline outDir=tmp outCss=gf.css "http://fonts.googleapis.com/css?family=Ubuntu:200italic"
```

With greatest HTML post-processing library for Grunt [processhtml](https://github.com/dciccale/grunt-processhtml) you may switch between local and hosted variants as simple as:

```html
<!-- build:remove:release -->
        <link rel="stylesheet" href="fonts/fonts.css">
<!-- /build -->

<!-- build:remove:dev  -->
        <script src="//ajax.googleapis.com/ajax/libs/webfont/1.4.7/webfont.js"></script>
        <script>
            WebFont.load({
                google: {
                    families: ['Dosis:200', 'Smythe', 'Ubuntu']
                }
            });
        </script>
<!-- /build -->
```

## Contributors

* [Christian Ulbrich](https://github.com/ChristianUlbrich)
