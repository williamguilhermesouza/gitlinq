using GitLinq;
using GitLinq.AST;
using GitLinq.Services;

namespace Tests.LinqExpressionBuilderTests;

[TestClass]
public class DiffTests
{
    private static List<CommitDiff> CreateTestDiffs()
    {
        return new List<CommitDiff>
        {
            new()
            {
                Sha = "abc1234567890",
                Message = "Initial commit",
                MessageShort = "Initial commit",
                AuthorName = "Alice",
                AuthorEmail = "alice@test.com",
                AuthorWhen = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero),
                Files = new List<FileChange>
                {
                    new() { Path = "README.md", Status = "Added", LinesAdded = 50, LinesDeleted = 0 },
                    new() { Path = "src/Program.cs", Status = "Added", LinesAdded = 100, LinesDeleted = 0 }
                }
            },
            new()
            {
                Sha = "def5678901234",
                Message = "Add feature X",
                MessageShort = "Add feature X",
                AuthorName = "Bob",
                AuthorEmail = "bob@test.com",
                AuthorWhen = new DateTimeOffset(2025, 1, 2, 11, 0, 0, TimeSpan.Zero),
                Files = new List<FileChange>
                {
                    new() { Path = "src/Feature.cs", Status = "Added", LinesAdded = 200, LinesDeleted = 0 },
                    new() { Path = "src/Program.cs", Status = "Modified", LinesAdded = 10, LinesDeleted = 5 },
                    new() { Path = "tests/FeatureTests.cs", Status = "Added", LinesAdded = 80, LinesDeleted = 0 }
                }
            },
            new()
            {
                Sha = "ghi9012345678",
                Message = "Fix bug in feature X",
                MessageShort = "Fix bug in feature X",
                AuthorName = "Alice",
                AuthorEmail = "alice@test.com",
                AuthorWhen = new DateTimeOffset(2025, 1, 3, 12, 0, 0, TimeSpan.Zero),
                Files = new List<FileChange>
                {
                    new() { Path = "src/Feature.cs", Status = "Modified", LinesAdded = 5, LinesDeleted = 10 }
                }
            },
            new()
            {
                Sha = "jkl3456789012",
                Message = "Rename file",
                MessageShort = "Rename file",
                AuthorName = "Charlie",
                AuthorEmail = "charlie@test.com",
                AuthorWhen = new DateTimeOffset(2025, 1, 4, 13, 0, 0, TimeSpan.Zero),
                Files = new List<FileChange>
                {
                    new() { Path = "src/NewFeature.cs", OldPath = "src/Feature.cs", Status = "Renamed", LinesAdded = 0, LinesDeleted = 0 }
                }
            },
            new()
            {
                Sha = "mno7890123456",
                Message = "Delete old files",
                MessageShort = "Delete old files",
                AuthorName = "Bob",
                AuthorEmail = "bob@test.com",
                AuthorWhen = new DateTimeOffset(2025, 1, 5, 14, 0, 0, TimeSpan.Zero),
                Files = new List<FileChange>
                {
                    new() { Path = "old/Legacy.cs", Status = "Deleted", LinesAdded = 0, LinesDeleted = 500 },
                    new() { Path = "old/OldTests.cs", Status = "Deleted", LinesAdded = 0, LinesDeleted = 200 }
                }
            },
        };
    }

    // ==================== CommitDiff Property Tests ====================

    [TestMethod]
    public void CommitDiff_ShortSha_Returns7Characters()
    {
        var diff = new CommitDiff { Sha = "abc1234567890" };
        Assert.AreEqual("abc1234", diff.ShortSha);
    }

    [TestMethod]
    public void CommitDiff_TotalLinesAdded_SumsCorrectly()
    {
        var diff = CreateTestDiffs()[1]; // "Add feature X" commit
        Assert.AreEqual(290, diff.TotalLinesAdded); // 200 + 10 + 80
    }

    [TestMethod]
    public void CommitDiff_TotalLinesDeleted_SumsCorrectly()
    {
        var diff = CreateTestDiffs()[4]; // "Delete old files" commit
        Assert.AreEqual(700, diff.TotalLinesDeleted); // 500 + 200
    }

    [TestMethod]
    public void CommitDiff_FilesChanged_CountsCorrectly()
    {
        var diff = CreateTestDiffs()[1]; // "Add feature X" commit
        Assert.AreEqual(3, diff.FilesChanged);
    }

    // ==================== Parser Tests for Comparison Operators ====================

    [TestMethod]
    public void Parser_GreaterThan_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Diffs.Where(d => d.FilesChanged > 2)");
        
        // Root should be MethodCallNode for Where
        Assert.IsInstanceOfType(ast, typeof(MethodCallNode));
        var whereCall = (MethodCallNode)ast;
        Assert.AreEqual("Where", whereCall.Method);
        
        // Target should be IdentifierNode "Diffs"
        Assert.IsInstanceOfType(whereCall.Target, typeof(IdentifierNode));
        Assert.AreEqual("Diffs", ((IdentifierNode)whereCall.Target).Name);
        
        // Argument should be a lambda
        var args = whereCall.Arguments.ToList();
        Assert.AreEqual(1, args.Count);
        Assert.IsInstanceOfType(args[0], typeof(LambdaNode));
        var lambda = (LambdaNode)args[0];
        Assert.AreEqual("d", lambda.Parameter);
        
        // Lambda body should be BinaryNode with ">"
        Assert.IsInstanceOfType(lambda.Body, typeof(BinaryNode));
        var binary = (BinaryNode)lambda.Body;
        Assert.AreEqual(">", binary.Operator);
        
        // Left side: d.FilesChanged
        Assert.IsInstanceOfType(binary.Left, typeof(MemberAccessNode));
        var memberAccess = (MemberAccessNode)binary.Left;
        Assert.AreEqual("FilesChanged", memberAccess.Member);
        Assert.IsInstanceOfType(memberAccess.Target, typeof(IdentifierNode));
        Assert.AreEqual("d", ((IdentifierNode)memberAccess.Target).Name);
        
        // Right side: 2
        Assert.IsInstanceOfType(binary.Right, typeof(NumberLiteralNode));
        Assert.AreEqual(2, ((NumberLiteralNode)binary.Right).Value);
    }

    [TestMethod]
    public void Parser_LessThan_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Diffs.Where(d => d.TotalLinesAdded < 100)");
        
        var whereCall = (MethodCallNode)ast;
        var lambda = (LambdaNode)whereCall.Arguments.First();
        var binary = (BinaryNode)lambda.Body;
        
        Assert.AreEqual("<", binary.Operator);
        Assert.AreEqual("TotalLinesAdded", ((MemberAccessNode)binary.Left).Member);
        Assert.AreEqual(100, ((NumberLiteralNode)binary.Right).Value);
    }

    [TestMethod]
    public void Parser_GreaterThanOrEqual_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Diffs.Where(d => d.FilesChanged >= 3)");
        
        var whereCall = (MethodCallNode)ast;
        var lambda = (LambdaNode)whereCall.Arguments.First();
        var binary = (BinaryNode)lambda.Body;
        
        Assert.AreEqual(">=", binary.Operator);
        Assert.AreEqual("FilesChanged", ((MemberAccessNode)binary.Left).Member);
        Assert.AreEqual(3, ((NumberLiteralNode)binary.Right).Value);
    }

    [TestMethod]
    public void Parser_LessThanOrEqual_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Diffs.Where(d => d.TotalLinesDeleted <= 10)");
        
        var whereCall = (MethodCallNode)ast;
        var lambda = (LambdaNode)whereCall.Arguments.First();
        var binary = (BinaryNode)lambda.Body;
        
        Assert.AreEqual("<=", binary.Operator);
        Assert.AreEqual("TotalLinesDeleted", ((MemberAccessNode)binary.Left).Member);
        Assert.AreEqual(10, ((NumberLiteralNode)binary.Right).Value);
    }

    [TestMethod]
    public void Parser_Equal_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Diffs.Where(d => d.FilesChanged == 1)");
        
        var whereCall = (MethodCallNode)ast;
        var lambda = (LambdaNode)whereCall.Arguments.First();
        var binary = (BinaryNode)lambda.Body;
        
        Assert.AreEqual("==", binary.Operator);
        Assert.AreEqual("FilesChanged", ((MemberAccessNode)binary.Left).Member);
        Assert.AreEqual(1, ((NumberLiteralNode)binary.Right).Value);
    }

    [TestMethod]
    public void Parser_NotEqual_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Diffs.Where(d => d.FilesChanged != 0)");
        
        var whereCall = (MethodCallNode)ast;
        var lambda = (LambdaNode)whereCall.Arguments.First();
        var binary = (BinaryNode)lambda.Body;
        
        Assert.AreEqual("!=", binary.Operator);
        Assert.AreEqual("FilesChanged", ((MemberAccessNode)binary.Left).Member);
        Assert.AreEqual(0, ((NumberLiteralNode)binary.Right).Value);
    }

    [TestMethod]
    public void Parser_ChainedMethods_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Diffs.Where(d => d.TotalLinesAdded > 50).Take(10)");
        
        // Root is Take call
        Assert.IsInstanceOfType(ast, typeof(MethodCallNode));
        var takeCall = (MethodCallNode)ast;
        Assert.AreEqual("Take", takeCall.Method);
        
        // Take argument is 10
        var takeArgs = takeCall.Arguments.ToList();
        Assert.AreEqual(1, takeArgs.Count);
        Assert.IsInstanceOfType(takeArgs[0], typeof(NumberLiteralNode));
        Assert.AreEqual(10, ((NumberLiteralNode)takeArgs[0]).Value);
        
        // Target of Take is Where call
        Assert.IsInstanceOfType(takeCall.Target, typeof(MethodCallNode));
        var whereCall = (MethodCallNode)takeCall.Target;
        Assert.AreEqual("Where", whereCall.Method);
        
        // Target of Where is Diffs
        Assert.IsInstanceOfType(whereCall.Target, typeof(IdentifierNode));
        Assert.AreEqual("Diffs", ((IdentifierNode)whereCall.Target).Name);
        
        // Where has lambda with binary comparison
        var lambda = (LambdaNode)whereCall.Arguments.First();
        var binary = (BinaryNode)lambda.Body;
        Assert.AreEqual(">", binary.Operator);
        Assert.AreEqual(50, ((NumberLiteralNode)binary.Right).Value);
    }

    [TestMethod]
    public void Parser_DiffsFirst_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Diffs.First()");
        
        Assert.IsInstanceOfType(ast, typeof(MethodCallNode));
        var firstCall = (MethodCallNode)ast;
        Assert.AreEqual("First", firstCall.Method);
        Assert.IsInstanceOfType(firstCall.Target, typeof(IdentifierNode));
        Assert.AreEqual("Diffs", ((IdentifierNode)firstCall.Target).Name);
        Assert.AreEqual(0, firstCall.Arguments.Count());
    }

    [TestMethod]
    public void Parser_DiffsFirstFiles_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Diffs.First().Files");
        
        // Root is MemberAccessNode for Files
        Assert.IsInstanceOfType(ast, typeof(MemberAccessNode));
        var filesAccess = (MemberAccessNode)ast;
        Assert.AreEqual("Files", filesAccess.Member);
        
        // Target is First() call
        Assert.IsInstanceOfType(filesAccess.Target, typeof(MethodCallNode));
        var firstCall = (MethodCallNode)filesAccess.Target;
        Assert.AreEqual("First", firstCall.Method);
        
        // Target of First is Diffs
        Assert.IsInstanceOfType(firstCall.Target, typeof(IdentifierNode));
        Assert.AreEqual("Diffs", ((IdentifierNode)firstCall.Target).Name);
    }

    // ==================== FileChange Model Tests ====================

    [TestMethod]
    public void FileChange_Status_CanBeAdded()
    {
        var file = new FileChange { Path = "test.cs", Status = "Added", LinesAdded = 100, LinesDeleted = 0 };
        Assert.AreEqual("Added", file.Status);
        Assert.AreEqual(100, file.LinesAdded);
    }

    [TestMethod]
    public void FileChange_Status_CanBeModified()
    {
        var file = new FileChange { Path = "test.cs", Status = "Modified", LinesAdded = 10, LinesDeleted = 5 };
        Assert.AreEqual("Modified", file.Status);
    }

    [TestMethod]
    public void FileChange_Status_CanBeDeleted()
    {
        var file = new FileChange { Path = "test.cs", Status = "Deleted", LinesAdded = 0, LinesDeleted = 100 };
        Assert.AreEqual("Deleted", file.Status);
    }

    [TestMethod]
    public void FileChange_Status_CanBeRenamed()
    {
        var file = new FileChange { Path = "new.cs", OldPath = "old.cs", Status = "Renamed" };
        Assert.AreEqual("Renamed", file.Status);
        Assert.AreEqual("old.cs", file.OldPath);
    }

    [TestMethod]
    public void FileChange_IsBinary_DefaultsFalse()
    {
        var file = new FileChange { Path = "test.cs" };
        Assert.IsFalse(file.IsBinary);
    }

    // ==================== CommitDiff Collection Tests ====================

    [TestMethod]
    public void CommitDiff_EmptyFiles_ReturnsZeroTotals()
    {
        var diff = new CommitDiff { Sha = "test123", Files = new List<FileChange>() };
        Assert.AreEqual(0, diff.TotalLinesAdded);
        Assert.AreEqual(0, diff.TotalLinesDeleted);
        Assert.AreEqual(0, diff.FilesChanged);
    }

    [TestMethod]
    public void CommitDiff_SingleFile_CalculatesTotalsCorrectly()
    {
        var diff = new CommitDiff
        {
            Sha = "test123",
            Files = new List<FileChange>
            {
                new() { Path = "test.cs", LinesAdded = 50, LinesDeleted = 25 }
            }
        };
        Assert.AreEqual(50, diff.TotalLinesAdded);
        Assert.AreEqual(25, diff.TotalLinesDeleted);
        Assert.AreEqual(1, diff.FilesChanged);
    }

    [TestMethod]
    public void CommitDiff_MultipleFiles_CalculatesTotalsCorrectly()
    {
        var diff = new CommitDiff
        {
            Sha = "test123",
            Files = new List<FileChange>
            {
                new() { Path = "a.cs", LinesAdded = 10, LinesDeleted = 5 },
                new() { Path = "b.cs", LinesAdded = 20, LinesDeleted = 10 },
                new() { Path = "c.cs", LinesAdded = 30, LinesDeleted = 15 }
            }
        };
        Assert.AreEqual(60, diff.TotalLinesAdded);
        Assert.AreEqual(30, diff.TotalLinesDeleted);
        Assert.AreEqual(3, diff.FilesChanged);
    }

    // ==================== Test Data Verification ====================

    [TestMethod]
    public void TestData_HasExpectedCommitCount()
    {
        var diffs = CreateTestDiffs();
        Assert.AreEqual(5, diffs.Count);
    }

    [TestMethod]
    public void TestData_FirstCommit_HasTwoFiles()
    {
        var diffs = CreateTestDiffs();
        Assert.AreEqual(2, diffs[0].Files.Count);
    }

    [TestMethod]
    public void TestData_SecondCommit_HasThreeFiles()
    {
        var diffs = CreateTestDiffs();
        Assert.AreEqual(3, diffs[1].Files.Count);
    }

    [TestMethod]
    public void TestData_IncludesRenamedFile()
    {
        var diffs = CreateTestDiffs();
        var renamedFile = diffs[3].Files.FirstOrDefault(f => f.Status == "Renamed");
        Assert.IsNotNull(renamedFile);
        Assert.AreEqual("src/NewFeature.cs", renamedFile.Path);
        Assert.AreEqual("src/Feature.cs", renamedFile.OldPath);
    }

    [TestMethod]
    public void TestData_IncludesDeletedFiles()
    {
        var diffs = CreateTestDiffs();
        var deletedFiles = diffs[4].Files.Where(f => f.Status == "Deleted").ToList();
        Assert.AreEqual(2, deletedFiles.Count);
    }
}
