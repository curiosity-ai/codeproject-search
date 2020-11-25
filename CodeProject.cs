using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace codeproject_search
{
    public static class CodeProject
    {
        public static async Task<HtmlDocument> GetPage(string url)
        {
            using (var c = new HttpClient())
            {
                var html = await c.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                return doc;
            }
        }

        public static IEnumerable<string> GetLinks(string baseUrl, HtmlDocument doc) 
        {
            return doc.DocumentNode
                      .SelectNodes("//a[@href]")
                      .Select(n => ToAbsolute(baseUrl, n.Attributes["href"].Value))
                      .Where(u => u is object)
                      .Distinct();
        }

        private static string ToAbsolute(string baseUrl, string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            var uri = new Uri(url, UriKind.RelativeOrAbsolute);

            if (!uri.IsAbsoluteUri)
            {
                uri = new Uri(new Uri(baseUrl), uri);
            }

            return uri.ToString();
        }

        public static (int views, int downloads, int bookmarked) GetStats(HtmlDocument doc)
        {
            var statsText = doc.GetElementbyId("ctl00_VerticalStats")?.InnerText;

            var views     = RE_Views.Match(statsText);
            var downloads = RE_Downloads.Match(statsText);
            var bookmarks = RE_Bookmarks.Match(statsText);


            return (views.Success     ? (int)(float.Parse(views.Groups[1].Value.Replace("K", "").Replace("M", ""))     * GetMultiplier(views)) : 0,
                    downloads.Success ? (int)(float.Parse(downloads.Groups[1].Value.Replace("K", "").Replace("M", "")) * GetMultiplier(downloads)) : 0,
                    bookmarks.Success ? (int)(float.Parse(bookmarks.Groups[1].Value.Replace("K", "").Replace("M", "")) * GetMultiplier(bookmarks)) : 0
                   );

            int GetMultiplier(Match match)
            {
                var val = match.Groups[1].Value;
                return (val.Contains("M") ? 1_000_000 : (val.Contains("K") ? 1_000 : 1));
            }
        }



        internal static async Task<(Dictionary<string, string> map, Dictionary<string, string> names)> GetCategories()
        {
            var doc = await GetPage("https://www.codeproject.com/KB/ajax/"); //Any chapter works, we just need a menu

            var menu = doc.DocumentNode.SelectNodes("//div[@id='SectionMenu']").First();

            var chapters = menu.SelectNodes("//div[@class='menu2']/a[@href]")
                               .Select(a => 
                                        (url:    ToAbsolute("https://www.codeproject.com/KB/ajax/", a.Attributes["href"].Value),
                                        chapter: string.Join("", a.Attributes["href"].Value
                                                                  .Split(new char[] {'/'}, StringSplitOptions.RemoveEmptyEntries).Take(2))
                                                                  .Replace("Chapters", "Chapter"),
                                        name:    a.InnerText.Trim()))
                               .ToDictionary(c => c.chapter, c => c);

            var subChaptersRoot = menu.SelectNodes("//div[@class='menu2-dropdown']");

            var map = new Dictionary<string, string>();
            var names = new Dictionary<string, string>();

            foreach (var sub in subChaptersRoot)
            {
                var chapterName = sub.Attributes["id"].Value;
                
                var chapter = chapters[chapterName];
                names[chapter.url] = chapter.name;

                var links = sub.SelectNodes("//div[@class='menu3']/a");

                foreach(var link in links)
                {
                    var url    = ToAbsolute("https://www.codeproject.com/KB/ajax/", link.Attributes["href"].Value);
                    map[url]   = chapter.url;
                    names[url] = link.InnerText.Trim();
                }
            }

            return (map, names);
        }



        public static (string description, string html, string text, string title) GetContent(string baseUrl, HtmlDocument doc)
        {
            var description = doc.GetElementbyId("ctl00_DescriptionSpot")?.InnerText ?? "";
            var title = doc.GetElementbyId("ctl00_ArticleTitle")?.InnerText ?? "";

            var content = doc.DocumentNode.SelectNodes("//div[@class='article']/form").FirstOrDefault();
            if(content is object)
            {
                RemoveAllMatching(content, "//input[@type='hidden']");
                RemoveAllMatching(content, "//iframe");
                RemoveAllMatching(content, "//div[@data-type='ad']");
                RemoveAllMatching(content, "//div[@class='share-list']");
                RemoveAllMatching(content, "//div[@class='author-wrapper']");
                RemoveAllMatching(content, "//div[@class='bottom-promo']");
                RemoveAllMatching(content, "//h2[@id='ctl00_AboutHeading']");
            }

            return (description, 
                    MakeLinksAbsolute(baseUrl, BuildPage(content?.InnerHtml ?? "")),
                    content.InnerText,
                    title);

            string BuildPage(string innerContent)
            {
                //remove images
                innerContent = innerContent.Replace("src=\"data:image/gif;base64,R0lGODlhAQABAAAAACH5BAEKAAEALAAAAAABAAEAAAICTAEAOw==\"", "");
                innerContent = innerContent.Replace("class=\"lazyload\"", "");
                innerContent = innerContent.Replace("<h2>Share</h2>", "");
                innerContent = innerContent.Trim(new char[] { '\r', '\n' });
                return $"<html><head><link rel=\"preconnect\" href=\"https://codeproject.freetls.fastly.net/App_Themes/CodeProject/Css/Main.min.css?dt=2.8.20201123.2\"></head><body>{innerContent}</body></html>";
            }
        }

        public static void RemoveAllMatching(HtmlNode content, string selector)
        {
            var hidden = content.SelectNodes(selector);
            if (hidden is object)
            {
                foreach (var c in hidden)
                {
                    c.ParentNode.RemoveChild(c);
                }
            }
        }

        static Regex RE_Views     = new Regex(@"([\d.KM]*?) view",       RegexOptions.Compiled);
        static Regex RE_Downloads = new Regex(@"([\d.KM]*?) download",   RegexOptions.Compiled);
        static Regex RE_Bookmarks = new Regex(@"([\d.KM]*?) bookmarked", RegexOptions.Compiled);

        public static IEnumerable<(string tag, string url)> GetTags(string baseUrl, HtmlDocument doc)
        {
            var tagNodes = doc.DocumentNode.SelectNodes("//a[@href and @rel='tag']");
            if (tagNodes is object) 
            {
                foreach (var n in tagNodes)
                {
                    yield return (n.InnerText, ToAbsolute(baseUrl, n.Attributes["href"].Value));
                }
            }
        }

        public static IEnumerable<(string name, string url)> GetAuthor(string baseUrl, HtmlDocument doc)
        {
            var authorLinks = doc.DocumentNode.SelectNodes("//a[@href and @rel='author']");

            foreach (var authorLink in authorLinks)
            {
                yield return (authorLink.InnerText, ToAbsolute(baseUrl, authorLink.Attributes["href"].Value));
            }
        }

        public static DateTimeOffset GetLastUpdated(HtmlDocument doc)
        {
            var dateNode = doc.GetElementbyId("ctl00_LastUpdated");
            if(dateNode is object)
            {
                return DateTimeOffset.ParseExact(dateNode.InnerText, "d MMM yyyy", new CultureInfo("en-US"));
            }
            else
            {
                return DateTimeOffset.UnixEpoch;
            }
        }

        private static string MakeLinksAbsolute(string baseUrl, string originalHtml)
        {
            var pattern = @"(?<name>data-src|src|href)=""(?<value>/[^""]*)""";
            var matchEvaluator = new MatchEvaluator(
                match =>
                {
                    var value = match.Groups["value"].Value;
                    var newUrl = ToAbsolute(baseUrl, value);
                    var name = match.Groups["name"].Value;
                    if (name == "data-src") name = "src";
                    return string.Format("{0}=\"{1}\"", name, newUrl);
                });
            return Regex.Replace(originalHtml, pattern, matchEvaluator);
        }
    }
}
