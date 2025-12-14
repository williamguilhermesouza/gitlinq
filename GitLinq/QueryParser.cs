using Sprache;

namespace GitLinq;

internal static class QueryParser
{
    private static readonly Parser<string> Identifier =
        Parse.Identifier(Parse.Letter.Or(Parse.Char('_')), Parse.LetterOrDigit.Or(Parse.Char('_'))).Token();

    private static readonly Parser<AstNode> StringLiteral =
        (from open in Parse.Char('"')
            from content in Parse.CharExcept('"').Many().Text()
            from close in Parse.Char('"')
            select (AstNode)new LiteralNode(content)).Token();

    private static readonly Parser<AstNode> NumberLiteral =
        Parse.Number.Token().Select(n => (AstNode)new LiteralNode(int.Parse(n)));

    private static readonly Parser<AstNode> BoolLiteral =
        Parse.String("true").Token().Return((AstNode)new LiteralNode(true))
            .Or(Parse.String("false").Token().Return((AstNode)new LiteralNode(false)));

    private static readonly Parser<MemberAccessNode> MemberAccess =
        from first in Identifier
        from rest in (from dot in Parse.Char('.').Token()
            from id in Identifier
            select id).Many()
        select new MemberAccessNode(new[] { first }.Concat(rest));

    private static readonly Parser<InstanceMethodCallNode> InstanceMethodCall =
        from member in MemberAccess
        from dot in Parse.Char('.')
        from method in Identifier
        from open in Parse.Char('(')
        from args in Parse.Ref(() => LambdaArg).DelimitedBy(Parse.Char(',').Token()).Optional()
        from close in Parse.Char(')')
        select BuildInstanceMethod(member, method, args.GetOrElse(Enumerable.Empty<AstNode>()));

    static InstanceMethodCallNode BuildInstanceMethod(MemberAccessNode m, string name, IEnumerable<AstNode> args)
    {
        var node = new InstanceMethodCallNode(m, name);
        foreach (var a in args) node.Args.Add(a);
        return node;
    }

    private static readonly Parser<AstNode> LambdaArg =
        StringLiteral
            .Or(NumberLiteral)
            .Or(BoolLiteral)
            .Or(InstanceMethodCall.Select(x => (AstNode)x))
            .Or(MemberAccess.Select(x => (AstNode)x));

    private static readonly Parser<string> Operator =
        Parse.String("==").Text()
            .Or(Parse.String("!=").Text())
            .Or(Parse.String(">=").Text())
            .Or(Parse.String("<=").Text())
            .Or(Parse.String(">").Text())
            .Or(Parse.String("<").Text())
            .Token();

    private static readonly Parser<AstNode> SimpleBinaryExpr =
        from left in InstanceMethodCall.Select(x => (AstNode)x)
            .Or(MemberAccess.Select(x => (AstNode)x))
            .Or(StringLiteral)
            .Or(NumberLiteral)
        from op in Operator
        from right in InstanceMethodCall.Select(x => (AstNode)x)
            .Or(MemberAccess.Select(x => (AstNode)x))
            .Or(StringLiteral)
            .Or(NumberLiteral)
        select (AstNode)new BinaryOpNode(op, left, right);

    private static readonly Parser<AstNode> LogicalExpr =
        Parse.ChainOperator(
            Parse.String("&&").Text().Token().Or(Parse.String("||").Text().Token()).Select(s =>
                (Func<AstNode, AstNode, AstNode>)((l, r) => new BinaryOpNode(s, l, r))),
            SimpleBinaryExpr.Token(),
            (op, left, right) => op(left, right)
        );

    private static readonly Parser<LambdaNode> Lambda =
        from param in Identifier
        from arrow in Parse.String("=>").Token()
        from body in LogicalExpr.Token().Or(SimpleBinaryExpr.Token()).Or(InstanceMethodCall.Select(x => (AstNode)x))
        select new LambdaNode(param, body);

    private static readonly Parser<AstNode> MethodArg =
        Lambda.Select(x => (AstNode)x)
            .Or(StringLiteral)
            .Or(NumberLiteral)
            .Or(BoolLiteral);

    private static readonly Parser<MethodCallNode> MethodCall =
        from name in Identifier
        from open in Parse.Char('(')
        from args in MethodArg.DelimitedBy(Parse.Char(',').Token()).Optional()
        from close in Parse.Char(')')
        select BuildMethodCall(name, args.GetOrElse(Enumerable.Empty<AstNode>()));

    static MethodCallNode BuildMethodCall(string name, IEnumerable<AstNode> args)
    {
        var m = new MethodCallNode(name);
        foreach (var a in args) m.Arguments.Add(a);
        return m;
    }

    private static readonly Parser<RootQueryNode> FullQuery =
        from root in Identifier
        from calls in (from dot in Parse.Char('.') from mc in MethodCall select mc).Many()
        select BuildRoot(root, calls);

    private static RootQueryNode BuildRoot(string root, IEnumerable<MethodCallNode> calls)
    {
        var r = new RootQueryNode(root);
        foreach (var c in calls) r.Calls.Add(c);
        return r;
    }

    public static RootQueryNode ParseQuery(string text)
    {
        var res = FullQuery.TryParse(text);
        return !res.WasSuccessful ? throw new Exception($"Parse error: {res.Message}") : res.Value;
    }
}