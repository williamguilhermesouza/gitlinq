namespace GitLinq.AST;

public record MethodCallNode(BaseNode Target, string Method, IEnumerable<BaseNode> Arguments) : BaseNode;