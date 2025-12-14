namespace GitLinq;

internal abstract class AstNode { }

internal class RootQueryNode(string rootName) : AstNode
{
    public string RootName { get; } = rootName;
    public List<MethodCallNode> Calls { get; } = [];
}

internal class MethodCallNode(string methodName) : AstNode
{
    public string MethodName { get; } = methodName;
    public List<AstNode> Arguments { get; } = new();
    public override string ToString() => $"{MethodName}({string.Join(", ", Arguments.Select(a => a.ToString()))})";
}

internal class LambdaNode(string paramName, AstNode body) : AstNode
{
    public string ParamName { get; } = paramName;
    public AstNode Body { get; } = body;
    public override string ToString() => $"{ParamName} => {Body}";
}

internal class BinaryOpNode(string op, AstNode left, AstNode right) : AstNode
{
    public string Op { get; } = op;
    public AstNode Left { get; } = left;
    public AstNode Right { get; } = right;
    public override string ToString() => $"({Left} {Op} {Right})";
}

internal class MemberAccessNode(IEnumerable<string> path) : AstNode
{
    public List<string> Path { get; } = path.ToList();
    public override string ToString() => string.Join(".", Path);
}

internal class LiteralNode(object value) : AstNode
{
    public object Value { get; } = value;
    public override string ToString() => Value is string ? $"\"{Value}\"" : Value?.ToString() ?? "null";
}

internal class InstanceMethodCallNode(MemberAccessNode target, string methodName) : AstNode
{
    public MemberAccessNode TargetMember { get; } = target;
    public string MethodName { get; } = methodName;
    public List<AstNode> Args { get; } = [];
    public override string ToString() => $"{TargetMember}.{MethodName}({string.Join(", ", Args)})";
}
