using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;

if (Env.ScriptArgs.Count == 0)
{
    Console.WriteLine();
    Console.WriteLine("  No job found. Use urls like it Google Fonts says as script arguments.");
    Console.WriteLine();
    Console.WriteLine("  Usage:");
    Console.WriteLine("    scriptcs gfo.csx -- fontUrl1[ fontUrl2[ fontUrl3]]");
    Console.WriteLine();
    Console.WriteLine("  Example:");
    Console.WriteLine("    scriptcs gfo.csx -- http://fonts.googleapis.com/css?family=Open+Sans http://fonts.googleapis.com/css?family=Roboto");
    Console.WriteLine("    scriptcs gfo.csx -- http://fonts.googleapis.com/css?family=Open+Sans|Roboto");
    Console.WriteLine();
    Environment.Exit(0);
}

//
//                                                           Group[0]
//                                                           vvvvvvvv
const string fontFaceRulePattern = @"(?<=@font-face\s*\{\s*)([^\{\}]+)";
//
//                                           Group[1]                              Group[2]
//                                           vvvvvv                                vvvvvv
const string fontUrlPattern = @"url\([\'""]?([^)]+?)[\'""]?\)(?=\s*format\([\'""]?([^)]+?)[\'""]?\))?";

//                                                            Group[1]
//                                 Group[0]                   vvvvvvv
//                                 vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
const string fontFamilyPattern = @"font-family\s*\:\s*[\'""]?([^\;]+?)[\'""]?\;";
const string fontWeightPattern = @"font-weight\s*\:\s*[\'""]?([^\;]+?)[\'""]?\;";
const string fontStylePattern =   @"font-style\s*\:\s*[\'""]?([^\;]+?)[\'""]?\;";

const string scriptOutputDir = "fonts";
const string cssFilename = "fonts.css";
const string fontFaceBulletproofStyle =
@"@font-face {
    font-family: '{{fontFamily}}';
    font-style: {{fontStyle}};
    font-weight: {{fontWeight}};
    src: url('{{fileName}}.eot');                                   /* {{embedded-opentype-realurl}} */
    src: local('☺'),
        url('{{fileName}}.eot?#iefix') format('embedded-opentype'), /* {{embedded-opentype-realurl}} */
        url('{{fileName}}.woff') format('woff'),                    /* {{woff-realurl}} */
        url('{{fileName}}.ttf') format('truetype'),                 /* {{truetype-realurl}} */
        url('{{fileName}}.svg#{{fileName}}') format('svg');         /* {{svg-realurl}} */
}
";

string ComposeFontFaceKey(string cssProps)
{
    var family = Regex.Match(cssProps, fontFamilyPattern).Groups[1].Value;
    var weight = Regex.Match(cssProps, fontWeightPattern).Groups[1].Value;
    var style =  Regex.Match(cssProps, fontStylePattern).Groups[1].Value;

    return string.Format("{0}:{1}:{2}", family, weight, style);
}

var fontsUrls = new NameValueCollection();
var userAgents = new Dictionary<string, string>
{
    // EOT lives in IE
    { "embedded-opentype", "Mozilla/5.0 (MSIE 9.0; Windows NT 6.1; Trident/5.0)" },

    // WOFF for Windows
    { "woff",              "Mozilla/5.0 (Windows NT 6.1; rv:2.0.1) Gecko/20100101 Firefox/4.0.1" },
    
    // TTF needs Opera
    { "truetype",          "Opera/9.80 (Macintosh; Intel Mac OS X; U; en) Presto/2.2.15 Version/10.00" },

    // SVG loves iPad
    { "svg",               "Mozilla/5.0 (iPad; U; CPU OS 3_2 like Mac OS X; en-us) AppleWebKit/531.21.10 (KHTML, like Gecko) Version/4.0.4 Mobile/7B334b Safari/531.21.10" }

    // WOFF for *nix ("Mozilla/5.0 (Linux; Android 4.1.1; Nexus 7 Build/JRO03D) AppleWebKit/535.19 (KHTML, like Gecko) Chrome/18.0.1025.166  Safari/535.19")
    // http://michaelboeke.com/blog/2013/09/10/Self-hosting-Google-web-fonts/

    // WOFF2 for Chrome ("Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/37.0.2062.103")
};
var fontExts = new Dictionary<string, string>
{
    { "embedded-opentype", ".eot"  },
    { "woff",              ".woff" },
    { "truetype",          ".ttf"  },
    { "svg",               ".svg"  }
};

// Step 0. Create directory if it does not exist
var outputDir = Path.Combine(Directory.GetCurrentDirectory(), scriptOutputDir);
if (!Directory.Exists(outputDir))
{
    Directory.CreateDirectory(outputDir);
}

// Step 1. Lookup and download all fonts from user URLs
using (var client = new WebClient())
{
    foreach(var fontUrl in Env.ScriptArgs)
    {
        foreach(var userAgent in userAgents)
        {
            Console.WriteLine("Mimics to {0}", userAgent.Value);
            client.Headers["User-Agent"] = userAgent.Value;

            var fontFaces = client.DownloadString(fontUrl);

            foreach (Match fontFaceOccurence in Regex.Matches(fontFaces, fontFaceRulePattern))
            {
                var currentFontFace = fontFaceOccurence.Value;
                var fontFaceKey = ComposeFontFaceKey(currentFontFace);

                foreach (Match fontUrlOccurence in Regex.Matches(currentFontFace, fontUrlPattern))
                {
                    var url = fontUrlOccurence.Groups[1].Value;
                    var format = fontUrlOccurence.Groups[2].Value;

                    // script recognize such font format AND has no font url in collection already
                    if (userAgents.ContainsKey(format) && !(fontsUrls[fontFaceKey] ?? "").Contains(url))
                    {
                        var filePath = Path.Combine(outputDir, fontFaceKey.Replace(' ', '+').Replace(':', '_') + fontExts[format]);

                        Console.WriteLine("  Downloading {0} as {1}...", fontFaceKey, format);

                        File.WriteAllBytes(filePath, new WebClient().DownloadData(url));

                        fontsUrls.Add(fontFaceKey, string.Format("{0}|{1}", format, url));
                    }
                }
            }
        }
    }
}

// Step 2. Build CSS
Console.WriteLine("Composing CSS file...");
foreach (var fontFaceKey in fontsUrls.AllKeys)
{
    var css = new String(fontFaceBulletproofStyle.ToCharArray());

    var fontProps = fontFaceKey.Split(':');
    var fileName = fontFaceKey.Replace(' ', '+').Replace(':', '_');
    var fontFamily = fontProps[0];
    var fontWeight = fontProps[1];
    var fontStyle = fontProps[2];

    css = css
        .Replace("{{fileName}}", fileName)
        .Replace("{{fontFamily}}", fontFamily)
        .Replace("{{fontWeight}}", fontWeight)
        .Replace("{{fontStyle}}", fontStyle);

    foreach (var fontsUrl in fontsUrls.GetValues(fontFaceKey))
    {
        var formatProps = fontsUrl.Split('|');
        var format = formatProps[0];
        var realUrl = formatProps[1];

        css = css.Replace("{{" + format + "-realurl}}", realUrl);
    }

    File.AppendAllText(Path.Combine(outputDir, "fonts.css"), css);
}

Console.WriteLine("Done!");
