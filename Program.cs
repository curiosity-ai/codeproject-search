using Curiosity.Library;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace codeproject_search
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ForceInvariantCultureAndUTF8Output();


            var (server, token) = (args[0], args[1]);

            using (var graph = Graph.Connect(server, token, "CodeProject"))
            {
                await graph.CreateNodeSchemaAsync<Article>();
                await graph.CreateNodeSchemaAsync<Author>();
                await graph.CreateNodeSchemaAsync<Tag>();
                await graph.CreateNodeSchemaAsync<Category>();
                await graph.CreateNodeSchemaAsync<Subcategory>();

                await graph.CreateEdgeSchemaAsync(Edges.AuthorOf, Edges.HasAuthor,
                                                  Edges.CategoryOf, Edges.HasCategory,
                                                  Edges.SubcategoryOf, Edges.HasSubcategory,
                                                  Edges.TagOf, Edges.HasTag);

                await IngestCodeProject(graph);

                await graph.CommitPendingAsync();
            }

            Console.WriteLine("Done !");
        }

        private static async Task IngestCodeProject(Graph graph)
        {
            Console.WriteLine("Reading site map...");

            var siteMap = await CodeProject.GetPage("https://www.codeproject.com/script/Content/SiteMap.aspx");
            var siteMapLinks  = CodeProject.GetLinks("https://www.codeproject.com/script/Content/SiteMap.aspx", siteMap);

            Console.WriteLine("Reading categories...");
            var (subcat2cat, catNames) = await CodeProject.GetCategories();

            foreach (var subcategoryUrl in siteMapLinks.Where(l => l.StartsWith("https://www.codeproject.com/KB/")))
            {
                var subcategoryName = catNames[subcategoryUrl];
                
                Console.WriteLine($"[{subcategoryName}] - Start processing");

                var subcategoryNode = graph.AddOrUpdate(new Subcategory() { Name = subcategoryName, Url = subcategoryUrl });

                var categoryUrl  = subcat2cat[subcategoryUrl];
                var categoryName = catNames[categoryUrl];

                var categoryNode = graph.AddOrUpdate(new Category() { Name = categoryName, Url = categoryUrl });

                graph.Link(categoryNode, subcategoryNode, Edges.HasSubcategory, Edges.SubcategoryOf);

                var subcategoryPage = await CodeProject.GetPage(subcategoryUrl);

                var articlesLinks   = CodeProject.GetLinks(subcategoryUrl, subcategoryPage)
                                                 .Where(u => u.StartsWith("https://www.codeproject.com/Articles/"));

                foreach (var articleLink in articlesLinks)
                {
                    Console.WriteLine($"[{categoryName} > {subcategoryName}] now processing {articleLink}");

                    var article = await CodeProject.GetPage(articleLink);

                    var tags    = CodeProject.GetTags(articleLink, article);
                    var stats   = CodeProject.GetStats(article);
                    var authors = CodeProject.GetAuthor(articleLink, article);
                    var content = CodeProject.GetContent(articleLink, article);
                    var date    = CodeProject.GetLastUpdated(article);

                    var articleNode = graph.AddOrUpdate(new Article()
                    {
                        Url         = articleLink,
                        Bookmarks   = stats.bookmarked,
                        Views       = stats.views,
                        Downloads   = stats.downloads,
                        Description = content.description,
                        Title       = content.title,
                        Text        = content.text,
                        Html        = content.html,
                        Timestamp   = date
                    });

                    foreach (var author in authors)
                    {
                        var authorNode = graph.AddOrUpdate(new Author() { Name = author.name, Url = author.url });
                        graph.Link(articleNode, authorNode, Edges.HasAuthor, Edges.AuthorOf);
                    }

                    foreach (var tag in tags)
                    {
                        var tagNode = graph.AddOrUpdate(new Tag() { Name = tag.tag, Url = tag.url });
                        graph.Link(articleNode, tagNode, Edges.HasTag, Edges.TagOf);
                    }

                    graph.Link(articleNode, subcategoryNode, Edges.HasSubcategory, Edges.SubcategoryOf);
                    graph.Link(articleNode, categoryNode,    Edges.HasCategory,    Edges.CategoryOf);
                }

                Console.WriteLine($"[{categoryName} > {subcategoryName}] - Done! \n\n");
            }
        }

        static void ForceInvariantCultureAndUTF8Output()
        {
            if (Environment.UserInteractive)
            {
                try
                {
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.InputEncoding  = Encoding.UTF8;
                }
                catch
                {
                    //This might throw if not running on a console, ignore as we don't care in that case
                }
            }
            Thread.CurrentThread.CurrentCulture     = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        }
    }
}
