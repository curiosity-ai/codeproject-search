using Curiosity.Library;

namespace codeproject_search
{
    [Node]
    public sealed class Category
    {
        [Key] public string Url { get; set; }
        [Property] public string Name { get; set; }
    }

    [Node]
    public sealed class Subcategory
    {
        [Key] public string Url { get; set; }
        [Property] public string Name { get; set; }
    }
}
