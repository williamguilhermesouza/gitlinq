namespace GitLinq.AST;

public record BinaryNode(BaseNode Left, string Operator, BaseNode Right) : BaseNode;