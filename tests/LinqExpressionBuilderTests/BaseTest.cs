using GitLinq;
using GitLinq.AST;
using GitLinq.Services;

namespace Tests.LinqExpressionBuilderTests;

[TestClass]
public class BaseTest
{
    protected static List<CommitInfo> CreateTestCommits()
    {
        return new List<CommitInfo>
        {
            new() { Sha = "abc1234", Message = "Initial commit", MessageShort = "Initial commit", AuthorName = "Alice", AuthorEmail = "alice@test.com", AuthorWhen = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero) },
            new() { Sha = "def5678", Message = "Add feature X", MessageShort = "Add feature X", AuthorName = "Bob", AuthorEmail = "bob@test.com", AuthorWhen = new DateTimeOffset(2025, 1, 2, 11, 0, 0, TimeSpan.Zero) },
            new() { Sha = "ghi9012", Message = "Fix bug in feature X", MessageShort = "Fix bug in feature X", AuthorName = "Alice", AuthorEmail = "alice@test.com", AuthorWhen = new DateTimeOffset(2025, 1, 3, 12, 0, 0, TimeSpan.Zero) },
            new() { Sha = "jkl3456", Message = "Update documentation", MessageShort = "Update documentation", AuthorName = "Charlie", AuthorEmail = "charlie@test.com", AuthorWhen = new DateTimeOffset(2025, 1, 4, 13, 0, 0, TimeSpan.Zero) },
            new() { Sha = "mno7890", Message = "Fix critical bug", MessageShort = "Fix critical bug", AuthorName = "Bob", AuthorEmail = "bob@test.com", AuthorWhen = new DateTimeOffset(2025, 1, 5, 14, 0, 0, TimeSpan.Zero) },
        };
    }

    [TestMethod]
    public void BuildStringLiteralExpression()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = new StringLiteralNode("test");
        
        var result = builder.Execute(ast);
        
        Assert.AreEqual("test", result);
    }
}
