using Curiosity.Library;

namespace codeproject_search
{
    [Node]
    public sealed class Author
    {
        [Key] public string Url { get; set; }
        [Property] public string Name { get; set; }
    }
}
