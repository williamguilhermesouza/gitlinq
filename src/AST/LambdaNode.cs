namespace GitLinq.AST;

public record LambdaNode(string Parameter, BaseNode Body) : BaseNode;
