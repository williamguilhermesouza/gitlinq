namespace GitLinq.AST;

public record MemberAccessNode(BaseNode Target, string Member) : BaseNode;
