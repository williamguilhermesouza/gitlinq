using GitLinq;
using GitLinq.Services;

namespace Tests.LinqExpressionBuilderTests;

[TestClass]
public class TakeSkipTests
{
    private static List<CommitInfo> CreateTestCommits()
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
    public void Take_ReturnsCorrectNumber()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Take(2)");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        var commits = result.ToList();
        Assert.AreEqual(2, commits.Count);
        Assert.AreEqual("abc1234", commits[0].Sha);
        Assert.AreEqual("def5678", commits[1].Sha);
    }

    [TestMethod]
    public void Take_MoreThanAvailable_ReturnsAll()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Take(100)");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        Assert.AreEqual(5, result.Count());
    }

    [TestMethod]
    public void Take_Zero_ReturnsEmpty()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Take(0)");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count());
    }

    [TestMethod]
    public void Skip_SkipsCorrectNumber()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Skip(2)");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        var commits = result.ToList();
        Assert.AreEqual(3, commits.Count);
        Assert.AreEqual("ghi9012", commits[0].Sha);
        Assert.AreEqual("jkl3456", commits[1].Sha);
        Assert.AreEqual("mno7890", commits[2].Sha);
    }

    [TestMethod]
    public void Skip_MoreThanAvailable_ReturnsEmpty()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Skip(100)");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count());
    }

    [TestMethod]
    public void SkipTake_Pagination()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Skip(1).Take(2)");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        var commits = result.ToList();
        Assert.AreEqual(2, commits.Count);
        Assert.AreEqual("def5678", commits[0].Sha);
        Assert.AreEqual("ghi9012", commits[1].Sha);
    }

    [TestMethod]
    public void WhereAndTake_FiltersThenLimits()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Message.Contains(\"Fix\")).Take(1)");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        var commits = result.ToList();
        Assert.AreEqual(1, commits.Count);
        Assert.AreEqual("ghi9012", commits[0].Sha);
    }
}
