using Sprache;

namespace GitLinq
{
    public static class QueryParser
    {
        public static Parser<string> Identifier = Parse.Letter.Or(Parse.Char('_')).AtLeastOnce().Text().Token();

        public static string ParseExpression(string inputExpression)
        {
            return Identifier.Parse(inputExpression);
        }

    }
}
