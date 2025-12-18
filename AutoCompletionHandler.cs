namespace GitLinq
{
    internal class AutoCompletionHandler : IAutoCompleteHandler
    {
        private char[] _separators = [' '];
        public char[] Separators { get => _separators; set { if (value != null) _separators = value; } }
        public string[] GetSuggestions(string text, int index) => new[] { "Commits", "help", "exit" };
    }
}
