using GitLinq.AST;
using Sprache;

namespace GitLinq
{
    public static class QueryParser
    {
        private static readonly Parser<string> Identifier =
            Parse.Letter.Or(Parse.Char('_')).AtLeastOnce().Text().Token();

        // Support both standard quotes and smart/curly quotes (Windows terminal compatibility)
        private static readonly Parser<char> OpenQuote = 
            Parse.Chars('"', '"', '„');  // Standard, left curly, low-9 quote
        
        private static readonly Parser<char> CloseQuote = 
            Parse.Chars('"', '"', '"');  // Standard, right curly, left curly (for mismatched pairs)
        
        private static readonly Parser<char> AnyQuote = 
            Parse.Chars('"', '"', '"', '„');

        private static readonly Parser<BaseNode> StringLiteral =
            from open in OpenQuote
            from content in Parse.CharExcept(c => AnyQuote.TryParse(c.ToString()).WasSuccessful, "non-quote").Many().Text()
            from close in CloseQuote
            select (BaseNode) new StringLiteralNode(content);

        private static readonly Parser<BaseNode> NumberLiteral =
            from digits in Parse.Digit.AtLeastOnce().Text().Token()
            select (BaseNode) new NumberLiteralNode(int.Parse(digits));

        private static readonly Parser<BaseNode> IdNode =
            Identifier.Select(BaseNode (name) => new IdentifierNode(name));

        private static readonly Parser<BaseNode> Lambda =
            from lparen in Parse.Char('(').Optional()
            from param in Identifier
            from rparen in Parse.Char(')').Optional()
            from arrow in Parse.String("=>").Token()
            from body in Parse.Ref(() => ExpressionParser)
            select (BaseNode)new LambdaNode(param, body);

        // Represents a suffix: either ".member" or ".method(args)"
        private static readonly Parser<Func<BaseNode, BaseNode>> MethodCallSuffix =
            from dot in Parse.Char('.')
            from method in Identifier
            from lparen in Parse.Char('(')
            from args in Parse.Ref(() => ExpressionParser).DelimitedBy(Parse.Char(',').Token()).Optional()
            from rparen in Parse.Char(')')
            select new Func<BaseNode, BaseNode>(target => new MethodCallNode(target, method, [..args.GetOrElse([])]));

        private static readonly Parser<Func<BaseNode, BaseNode>> MemberAccessSuffix =
            from dot in Parse.Char('.')
            from member in Identifier
            select new Func<BaseNode, BaseNode>(target => new MemberAccessNode(target, member));

        // A suffix is either a method call or a member access
        private static readonly Parser<Func<BaseNode, BaseNode>> Suffix =
            MethodCallSuffix.Or(MemberAccessSuffix);

        // Parse a primary expression (identifier, string literal, or number) followed by zero or more suffixes
        private static readonly Parser<BaseNode> ChainedExpression =
            from primary in IdNode.Or(StringLiteral).Or(NumberLiteral)
            from suffixes in Suffix.Many()
            select suffixes.Aggregate(primary, (current, suffix) => suffix(current));

        private static readonly Parser<BaseNode> ExpressionParser =
            Lambda.Or(ChainedExpression);

        public static BaseNode ParseExpression(string inputExpression)
        {
            var rootNode = ExpressionParser.Parse(inputExpression);
            return rootNode;
        }

    }
}
