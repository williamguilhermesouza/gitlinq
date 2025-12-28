namespace GitLinq
{
    internal class AutoCompletionHandler : IAutoCompleteHandler
    {
        private readonly string[] _suggestions = ["Commits", "Branches", "Where", "Contains", "First", "Last", "help", "exit"];
        private char[] _separators = [' '];
        public char[] Separators { get => _separators; set { if (value != null) _separators = value; } }
        public string[] GetSuggestions(string text, int index) => [.. _suggestions.Where(s => s.Contains(text))];
    }
}
