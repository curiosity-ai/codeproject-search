using Curiosity.Library;
using System;

namespace codeproject_search
{
    [Node]
    public sealed class Article
    {
        [Key] public string Url { get; set; }
        [Property] public string Title { get; set; }
        [Property] public string Description { get; set; }
        [Property] public string Text { get; set; }
        [Property] public string Html { get; set; }
        [Timestamp] public DateTimeOffset Timestamp { get; set; }
        [Property] public int Views { get; set; }
        [Property] public int Bookmarks { get; set; }
        [Property] public int Downloads { get; set; }
    }
}
