using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

if (Env.ScriptArgs.Count == 0)
{
    Console.WriteLine();
    Console.WriteLine("  No job found. Use URL like it Google Fonts says as script argument.");
    Console.WriteLine();
    Console.WriteLine("  Usage:");
    Console.WriteLine("    scriptcs goofoffline.csx -- fontUrl");
    Console.WriteLine();
    Console.WriteLine("  Example:");
    Console.WriteLine("    scriptcs goofoffline.csx -- \"http://fonts.googleapis.com/css?family=Open+Sans|Roboto\"");
    Console.WriteLine();
    Console.WriteLine("  With PowerShell:");
    Console.WriteLine("    scriptcs goofoffline.csx `-- \"http://fonts.googleapis.com/css?family=Open+Sans|Roboto\"");
    Console.WriteLine();
    
    Environment.Exit(0);
}

//
//                                                           Group[0]
//                                                           vvvvvvvv
const string FontFaceRulePattern = @"(?<=@font-face\s*\{\s*)([^\{\}]+)";
//
//                                                Group[1]                              Group[2]
//                                                vvvvvv                                vvvvvv
const string FontUrlPattern      = @"url\([\'""]?([^)]+?)[\'""]?\)(?=\s*format\([\'""]?([^)]+?)[\'""]?\))";

//                                                              Group[1]
//                                   Group[0]                   vvvvvvv
//                                   vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
const string FontFamilyPattern   = @"font-family\s*\:\s*[\'""]?([^\;]+?)[\'""]?\;";
const string FontWeightPattern   = @"font-weight\s*\:\s*[\'""]?([^\;]+?)[\'""]?\;";
const string FontStylePattern    =  @"font-style\s*\:\s*[\'""]?([^\;]+?)[\'""]?\;";

string _outputDir                = Path.Combine(Directory.GetCurrentDirectory(), "fonts"),
       _cssFilename              = "fonts.css",
       _fontFaceBulletproofStyle = File.ReadAllText("fontface.css.tpl");

var _fontfaceList = new NameValueCollection();
var _fileExts = new Dictionary<string, string>
{
    { "embedded-opentype", ".eot"  },
    { "woff",              ".woff" },
    { "truetype",          ".ttf"  },
    { "svg",               ".svg"  }
};
var _userAgents = new string[]
{
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
};
var _urlToProcess = Env.ScriptArgs[0];
var _fontDownloadingTasks = new List<Task>();

// Step 0. Create directory if it does not exist
CreateOutputDirectory();

// Step 1. Lookup and download all fonts from user URLs
Console.WriteLine("Downloading started...");
Array.ForEach(_userAgents, ProcessGoogleFontsUrl);
Task.WaitAll(_fontDownloadingTasks.ToArray());

// Step 2. Build CSS
Console.WriteLine("Composing CSS file...");
BuildOutputCssFile();

Console.WriteLine("Done!");

void ProcessGoogleFontsUrl(string userAgent)
{
    var client = new WebClient();
    client.Headers["User-Agent"] = userAgent;

    var css = client.DownloadString(_urlToProcess);

    foreach (Match fontfaceOccurence in Regex.Matches(css, FontFaceRulePattern))
    {
        ProcessFontface(fontfaceOccurence.Value);
    }
}

void ProcessFontface(string fontface)
{
    var fontfaceKey = ComposeFontFaceKey(fontface);

    foreach (Match fontUrlOccurence in Regex.Matches(fontface, FontUrlPattern))
    {
        var url = fontUrlOccurence.Groups[1].Value;
        var format = fontUrlOccurence.Groups[2].Value;

        if ( !(_fontfaceList[fontfaceKey] ?? string.Empty).Contains(url) )
        {
            _fontDownloadingTasks.Add(

                new WebClient()
                        .DownloadDataTaskAsync(url)
                        .ContinueWith(t => {

                            var filePath = Path.Combine(_outputDir, fontfaceKey.Replace(' ', '+').Replace(':', '_') + _fileExts[format]);
                            File.WriteAllBytes(filePath, t.Result);

                            lock (_fontfaceList)
                            {
                                _fontfaceList.Add(fontfaceKey, string.Format("{0}|{1}", format, url));
                            }

                            Console.WriteLine("  {0}\t({1})", fontfaceKey, format);
                        })
            );
        }
    }
}

string ComposeFontFaceKey(string css)
{
    var family = Regex.Match(css, FontFamilyPattern).Groups[1].Value;
    var weight = Regex.Match(css, FontWeightPattern).Groups[1].Value;
    var style =  Regex.Match(css, FontStylePattern).Groups[1].Value;

    return string.Format("{0}:{1}:{2}", family, weight, style);
}

void BuildOutputCssFile()
{
    foreach (var fontfaceKey in _fontfaceList.AllKeys)
    {
        var css = new String(_fontFaceBulletproofStyle.ToCharArray());
        var splittedArray = fontfaceKey.Split(':');
        
        var fontFamily = splittedArray[0];
        var fontWeight = splittedArray[1];
        var fontStyle = splittedArray[2];

        var fontFilename = fontfaceKey.Replace(' ', '+').Replace(':', '_');

        css = css
            .Replace("{{fontFilename}}", fontFilename)
            .Replace("{{fontFamily}}", fontFamily)
            .Replace("{{fontWeight}}", fontWeight)
            .Replace("{{fontStyle}}", fontStyle);

        foreach (var fontsUrl in _fontfaceList.GetValues(fontfaceKey))
        {
            var formatProps = fontsUrl.Split('|');
            var format = formatProps[0];
            var googleFontsUrl = formatProps[1];

            css = css.Replace("{{" + format + "-gf-url}}", googleFontsUrl);
        }

        File.AppendAllText(Path.Combine(_outputDir, _cssFilename), css);
    }
}

void CreateOutputDirectory()
{
    if (!Directory.Exists(_outputDir))
    {
        Directory.CreateDirectory(_outputDir);
    }
}
