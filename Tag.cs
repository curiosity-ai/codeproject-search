using Curiosity.Library;

namespace codeproject_search
{
    [Node]
    public sealed class Tag
    {
        [Key] public string Url { get; set; }
        [Property] public string Name { get; set; }
    }
}
