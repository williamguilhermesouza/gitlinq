using GitLinq;
using GitLinq.Models;

namespace Tests.LinqExpressionBuilderTests;

[TestClass]
public class FirstTests
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
    public void First_ReturnsFirstCommit()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.First()");
        
        var result = builder.Execute(ast) as CommitInfo;
        
        Assert.IsNotNull(result);
        Assert.AreEqual("abc1234", result.Sha);
    }

    [TestMethod]
    public void FirstWithPredicate_ReturnsFirstMatchingCommit()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.First(c => c.Message.Contains(\"Fix\"))");
        
        var result = builder.Execute(ast) as CommitInfo;
        
        Assert.IsNotNull(result);
        Assert.AreEqual("ghi9012", result.Sha);
        Assert.IsTrue(result.Message.Contains("Fix"));
    }

    [TestMethod]
    public void FirstWithPredicate_FindsSpecificMessage()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.First(c => c.Message.Contains(\"Initial\"))");
        
        var result = builder.Execute(ast) as CommitInfo;
        
        Assert.IsNotNull(result);
        Assert.AreEqual("abc1234", result.Sha);
        Assert.AreEqual("Initial commit", result.Message);
    }

    [TestMethod]
    public void FirstWithPredicate_NoMatch_ThrowsException()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.First(c => c.Message.Contains(\"nonexistent\"))");
        
        var exception = Assert.ThrowsException<System.Reflection.TargetInvocationException>(() => builder.Execute(ast));
        Assert.IsInstanceOfType(exception.InnerException, typeof(InvalidOperationException));
    }

    [TestMethod]
    public void FirstOrDefault_ReturnsFirstCommit()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.FirstOrDefault()");
        
        var result = builder.Execute(ast) as CommitInfo;
        
        Assert.IsNotNull(result);
        Assert.AreEqual("abc1234", result.Sha);
    }

    [TestMethod]
    public void FirstOrDefaultWithPredicate_ReturnsFirstMatchingCommit()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.FirstOrDefault(c => c.Message.Contains(\"Fix\"))");
        
        var result = builder.Execute(ast) as CommitInfo;
        
        Assert.IsNotNull(result);
        Assert.AreEqual("ghi9012", result.Sha);
    }

    [TestMethod]
    public void FirstOrDefaultWithPredicate_NoMatch_ReturnsNull()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.FirstOrDefault(c => c.Message.Contains(\"nonexistent\"))");
        
        var result = builder.Execute(ast);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FirstAfterWhere_ReturnsCorrectCommit()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.AuthorName.Contains(\"Bob\")).First()");
        
        var result = builder.Execute(ast) as CommitInfo;
        
        Assert.IsNotNull(result);
        Assert.AreEqual("def5678", result.Sha);
        Assert.AreEqual("Bob", result.AuthorName);
    }
}
