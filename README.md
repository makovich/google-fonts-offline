# Google Fonts Offline

[![NPM](https://nodei.co/npm/google-fonts-offline.png?mini=true)](https://nodei.co/npm/google-fonts-offline/)

## Getting Started

Internet in my country a little bit sucks sometimes so I need to use fonts continuously. With greatest HTML post-processing library for Grunt [processhtml](https://github.com/dciccale/grunt-processhtml) it may looks like this:

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

There are two options for downloading Google Fonts.

### Node.js

With Node.js install package (prefer global):

```
npm install -g google-fonts-offline
```

Then use links from Quick Use or your collection as an argument:

```
goofoffline "http://fonts.googleapis.com/css?family=Open+Sans"
```

All stuff will be stored at `fonts` directory by default. CSS file with `@font-face` declarations called `fonts.css`.

You can use `outDir` and `outCss` arguments for changing defaults. For example:

```
goofoffline outDir=tmp outCss=gf.css "http://fonts.googleapis.com/css?family=Ubuntu:200italic"
```

### .net/mono scriptcs

With [scriptcs](http://scriptcs.net/) download `goofoffline.csx` and `fontface.css.tpl` into your working directory and then use your Google Fonts link:

```
scriptcs goofoffline.csx -- "http://fonts.googleapis.com/css?family=Ubuntu:200italic"
```

> Notice! Powershell need escape double dash with backtick symbol, i.e. `` `--  `` instead of ` -- `.

Checkout your new shiny `fonts` directory and `fonts.css` file.

## Little thoughts

I've test this scripts just with Google Fonts but I think it should works with any stylesheets URLs with `@font-face` declarations.


## Contributors

* [Christian Ulbrich](https://github.com/ChristianUlbrich)
