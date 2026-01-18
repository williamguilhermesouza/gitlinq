using GitLinq;
using GitLinq.AST;
using GitLinq.Models;

namespace Tests.LinqExpressionBuilderTests;

[TestClass]
public class DiffTests
{
    private static List<DiffData> CreateTestDiffs()
    {
        return new List<DiffData>
        {
            new()
            {
                Files = new List<FileChange>
                {
                    new() { Path = "README.md", Status = "Added", LinesAdded = 50, LinesDeleted = 0 },
                    new() { Path = "src/Program.cs", Status = "Added", LinesAdded = 100, LinesDeleted = 0 }
                }
            },
            new()
            {
                Files = new List<FileChange>
                {
                    new() { Path = "src/Feature.cs", Status = "Added", LinesAdded = 200, LinesDeleted = 0 },
                    new() { Path = "src/Program.cs", Status = "Modified", LinesAdded = 10, LinesDeleted = 5 },
                    new() { Path = "tests/FeatureTests.cs", Status = "Added", LinesAdded = 80, LinesDeleted = 0 }
                }
            },
            new()
            {
                Files = new List<FileChange>
                {
                    new() { Path = "src/Feature.cs", Status = "Modified", LinesAdded = 5, LinesDeleted = 10 }
                }
            },
            new()
            {
                Files = new List<FileChange>
                {
                    new() { Path = "src/NewFeature.cs", OldPath = "src/Feature.cs", Status = "Renamed", LinesAdded = 0, LinesDeleted = 0 }
                }
            },
            new()
            {
                Files = new List<FileChange>
                {
                    new() { Path = "old/Legacy.cs", Status = "Deleted", LinesAdded = 0, LinesDeleted = 500 },
                    new() { Path = "old/OldTests.cs", Status = "Deleted", LinesAdded = 0, LinesDeleted = 200 }
                }
            },
        };
    }

    // ==================== DiffData Property Tests ====================

    [TestMethod]
    public void DiffData_TotalLinesAdded_SumsCorrectly()
    {
        var diff = CreateTestDiffs()[1]; // Second diff with 3 files
        Assert.AreEqual(290, diff.TotalLinesAdded); // 200 + 10 + 80
    }

    [TestMethod]
    public void DiffData_TotalLinesDeleted_SumsCorrectly()
    {
        var diff = CreateTestDiffs()[4]; // Last diff with deletions
        Assert.AreEqual(700, diff.TotalLinesDeleted); // 500 + 200
    }

    [TestMethod]
    public void DiffData_FilesChanged_CountsCorrectly()
    {
        var diff = CreateTestDiffs()[1]; // Second diff with 3 files
        Assert.AreEqual(3, diff.FilesChanged);
    }

    // ==================== Parser Tests for Comparison Operators ====================

    [TestMethod]
    public void Parser_GreaterThan_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.FilesChanged > 2)");
        
        // Root should be MethodCallNode for Where
        Assert.IsInstanceOfType(ast, typeof(MethodCallNode));
        var whereCall = (MethodCallNode)ast;
        Assert.AreEqual("Where", whereCall.Method);
        
        // Target should be IdentifierNode "Commits"
        Assert.IsInstanceOfType(whereCall.Target, typeof(IdentifierNode));
        Assert.AreEqual("Commits", ((IdentifierNode)whereCall.Target).Name);
        
        // Argument should be a lambda
        var args = whereCall.Arguments.ToList();
        Assert.AreEqual(1, args.Count);
        Assert.IsInstanceOfType(args[0], typeof(LambdaNode));
        var lambda = (LambdaNode)args[0];
        Assert.AreEqual("c", lambda.Parameter);
        
        // Lambda body should be BinaryNode with ">"
        Assert.IsInstanceOfType(lambda.Body, typeof(BinaryNode));
        var binary = (BinaryNode)lambda.Body;
        Assert.AreEqual(">", binary.Operator);
        
        // Right side: 2
        Assert.IsInstanceOfType(binary.Right, typeof(NumberLiteralNode));
        Assert.AreEqual(2, ((NumberLiteralNode)binary.Right).Value);
    }

    [TestMethod]
    public void Parser_LessThan_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.TotalLinesAdded < 100)");
        
        var whereCall = (MethodCallNode)ast;
        var lambda = (LambdaNode)whereCall.Arguments.First();
        var binary = (BinaryNode)lambda.Body;
        
        Assert.AreEqual("<", binary.Operator);
        Assert.AreEqual(100, ((NumberLiteralNode)binary.Right).Value);
    }

    [TestMethod]
    public void Parser_GreaterThanOrEqual_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.FilesChanged >= 3)");
        
        var whereCall = (MethodCallNode)ast;
        var lambda = (LambdaNode)whereCall.Arguments.First();
        var binary = (BinaryNode)lambda.Body;
        
        Assert.AreEqual(">=", binary.Operator);
        Assert.AreEqual(3, ((NumberLiteralNode)binary.Right).Value);
    }

    [TestMethod]
    public void Parser_LessThanOrEqual_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.TotalLinesDeleted <= 10)");
        
        var whereCall = (MethodCallNode)ast;
        var lambda = (LambdaNode)whereCall.Arguments.First();
        var binary = (BinaryNode)lambda.Body;
        
        Assert.AreEqual("<=", binary.Operator);
        Assert.AreEqual(10, ((NumberLiteralNode)binary.Right).Value);
    }

    [TestMethod]
    public void Parser_Equal_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.FilesChanged == 1)");
        
        var whereCall = (MethodCallNode)ast;
        var lambda = (LambdaNode)whereCall.Arguments.First();
        var binary = (BinaryNode)lambda.Body;
        
        Assert.AreEqual("==", binary.Operator);
        Assert.AreEqual(1, ((NumberLiteralNode)binary.Right).Value);
    }

    [TestMethod]
    public void Parser_NotEqual_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.FilesChanged != 0)");
        
        var whereCall = (MethodCallNode)ast;
        var lambda = (LambdaNode)whereCall.Arguments.First();
        var binary = (BinaryNode)lambda.Body;
        
        Assert.AreEqual("!=", binary.Operator);
        Assert.AreEqual(0, ((NumberLiteralNode)binary.Right).Value);
    }

    [TestMethod]
    public void Parser_ChainedMethods_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.TotalLinesAdded > 50).Take(10)");
        
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
        
        // Target of Where is Commits
        Assert.IsInstanceOfType(whereCall.Target, typeof(IdentifierNode));
        Assert.AreEqual("Commits", ((IdentifierNode)whereCall.Target).Name);
        
        // Where has lambda with binary comparison
        var lambda = (LambdaNode)whereCall.Arguments.First();
        var binary = (BinaryNode)lambda.Body;
        Assert.AreEqual(">", binary.Operator);
        Assert.AreEqual(50, ((NumberLiteralNode)binary.Right).Value);
    }

    [TestMethod]
    public void Parser_CommitsFirst_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.First()");
        
        Assert.IsInstanceOfType(ast, typeof(MethodCallNode));
        var firstCall = (MethodCallNode)ast;
        Assert.AreEqual("First", firstCall.Method);
        Assert.IsInstanceOfType(firstCall.Target, typeof(IdentifierNode));
        Assert.AreEqual("Commits", ((IdentifierNode)firstCall.Target).Name);
        Assert.AreEqual(0, firstCall.Arguments.Count());
    }

    [TestMethod]
    public void Parser_CommitsFirstDiffFiles_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.First().Diff.Files");
        
        // Root is MemberAccessNode for Files
        Assert.IsInstanceOfType(ast, typeof(MemberAccessNode));
        var filesAccess = (MemberAccessNode)ast;
        Assert.AreEqual("Files", filesAccess.Member);
        
        // Target is Diff member access
        Assert.IsInstanceOfType(filesAccess.Target, typeof(MemberAccessNode));
        var diffAccess = (MemberAccessNode)filesAccess.Target;
        Assert.AreEqual("Diff", diffAccess.Member);
        
        // Target of Diff is First() call
        Assert.IsInstanceOfType(diffAccess.Target, typeof(MethodCallNode));
        var firstCall = (MethodCallNode)diffAccess.Target;
        Assert.AreEqual("First", firstCall.Method);
        
        // Target of First is Commits
        Assert.IsInstanceOfType(firstCall.Target, typeof(IdentifierNode));
        Assert.AreEqual("Commits", ((IdentifierNode)firstCall.Target).Name);
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

    // ==================== DiffData Collection Tests ====================

    [TestMethod]
    public void DiffData_EmptyFiles_ReturnsZeroTotals()
    {
        var diff = new DiffData { Files = new List<FileChange>() };
        Assert.AreEqual(0, diff.TotalLinesAdded);
        Assert.AreEqual(0, diff.TotalLinesDeleted);
        Assert.AreEqual(0, diff.FilesChanged);
    }

    [TestMethod]
    public void DiffData_SingleFile_CalculatesTotalsCorrectly()
    {
        var diff = new DiffData
        {
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
    public void DiffData_MultipleFiles_CalculatesTotalsCorrectly()
    {
        var diff = new DiffData
        {
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
    public void TestData_HasExpectedDiffCount()
    {
        var diffs = CreateTestDiffs();
        Assert.AreEqual(5, diffs.Count);
    }

    [TestMethod]
    public void TestData_FirstDiff_HasTwoFiles()
    {
        var diffs = CreateTestDiffs();
        Assert.AreEqual(2, diffs[0].Files.Count);
    }

    [TestMethod]
    public void TestData_SecondDiff_HasThreeFiles()
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

    // ==================== Content Search Tests ====================

    [TestMethod]
    public void FileChange_AddedContains_FindsText()
    {
        var file = new FileChange
        {
            Path = "test.cs",
            AddedContent = new List<string> { "public class Test", "// TODO: implement this", "}" }
        };
        Assert.IsTrue(file.AddedContains("TODO"));
        Assert.IsTrue(file.AddedContains("class"));
        Assert.IsFalse(file.AddedContains("FIXME"));
    }

    [TestMethod]
    public void FileChange_DeletedContains_FindsText()
    {
        var file = new FileChange
        {
            Path = "test.cs",
            DeletedContent = new List<string> { "old code", "removed bug" }
        };
        Assert.IsTrue(file.DeletedContains("bug"));
        Assert.IsTrue(file.DeletedContains("old"));
        Assert.IsFalse(file.DeletedContains("new"));
    }

    [TestMethod]
    public void FileChange_ContentContains_FindsInAddedOrDeleted()
    {
        var file = new FileChange
        {
            Path = "test.cs",
            AddedContent = new List<string> { "new feature" },
            DeletedContent = new List<string> { "old feature" }
        };
        Assert.IsTrue(file.ContentContains("new"));
        Assert.IsTrue(file.ContentContains("old"));
        Assert.IsTrue(file.ContentContains("feature"));
        Assert.IsFalse(file.ContentContains("missing"));
    }

    [TestMethod]
    public void FileChange_AddedContains_IsCaseInsensitive()
    {
        var file = new FileChange
        {
            Path = "test.cs",
            AddedContent = new List<string> { "TODO: fix this" }
        };
        Assert.IsTrue(file.AddedContains("todo"));
        Assert.IsTrue(file.AddedContains("TODO"));
        Assert.IsTrue(file.AddedContains("Todo"));
    }

    // ==================== DiffData Content Search Tests ====================

    [TestMethod]
    public void DiffData_AddedContains_FindsAcrossFiles()
    {
        var diff = new DiffData
        {
            Files = new List<FileChange>
            {
                new() { Path = "file1.cs", AddedContent = new List<string> { "new feature" } },
                new() { Path = "file2.cs", AddedContent = new List<string> { "another line" } }
            }
        };
        Assert.IsTrue(diff.AddedContains("feature"));
        Assert.IsTrue(diff.AddedContains("another"));
        Assert.IsFalse(diff.AddedContains("missing"));
    }

    [TestMethod]
    public void DiffData_DeletedContains_FindsAcrossFiles()
    {
        var diff = new DiffData
        {
            Files = new List<FileChange>
            {
                new() { Path = "file1.cs", DeletedContent = new List<string> { "old code" } },
                new() { Path = "file2.cs", DeletedContent = new List<string> { "removed function" } }
            }
        };
        Assert.IsTrue(diff.DeletedContains("old"));
        Assert.IsTrue(diff.DeletedContains("removed"));
        Assert.IsFalse(diff.DeletedContains("missing"));
    }

    [TestMethod]
    public void DiffData_ContentContains_FindsInAddedOrDeleted()
    {
        var diff = new DiffData
        {
            Files = new List<FileChange>
            {
                new() { Path = "file1.cs", AddedContent = new List<string> { "new feature" }, DeletedContent = new List<string> { "old feature" } }
            }
        };
        Assert.IsTrue(diff.ContentContains("new"));
        Assert.IsTrue(diff.ContentContains("old"));
        Assert.IsTrue(diff.ContentContains("feature"));
        Assert.IsFalse(diff.ContentContains("missing"));
    }

    [TestMethod]
    public void DiffData_AddedContains_IsCaseInsensitive()
    {
        var diff = new DiffData
        {
            Files = new List<FileChange>
            {
                new() { Path = "test.cs", AddedContent = new List<string> { "TODO: fix this" } }
            }
        };
        Assert.IsTrue(diff.AddedContains("todo"));
        Assert.IsTrue(diff.AddedContains("TODO"));
        Assert.IsTrue(diff.AddedContains("Todo"));
    }

    // ==================== Nested Any Parser Tests ====================

    [TestMethod]
    public void Parser_NestedAny_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.Files.Any(f => f.Path.Contains(\"cs\")))");
        
        // Root should be MethodCallNode for Where
        Assert.IsInstanceOfType(ast, typeof(MethodCallNode));
        var whereCall = (MethodCallNode)ast;
        Assert.AreEqual("Where", whereCall.Method);
        
        // Where argument should be lambda: c => c.Diff.Files.Any(...)
        var outerLambda = (LambdaNode)whereCall.Arguments.First();
        Assert.AreEqual("c", outerLambda.Parameter);
        
        // Lambda body should be Any call
        Assert.IsInstanceOfType(outerLambda.Body, typeof(MethodCallNode));
        var anyCall = (MethodCallNode)outerLambda.Body;
        Assert.AreEqual("Any", anyCall.Method);
        
        // Any target should be c.Diff.Files
        Assert.IsInstanceOfType(anyCall.Target, typeof(MemberAccessNode));
        var filesAccess = (MemberAccessNode)anyCall.Target;
        Assert.AreEqual("Files", filesAccess.Member);
        
        // Any argument should be lambda: f => f.Path.Contains("cs")
        var innerLambda = (LambdaNode)anyCall.Arguments.First();
        Assert.AreEqual("f", innerLambda.Parameter);
        
        // Inner lambda body should be Contains call
        Assert.IsInstanceOfType(innerLambda.Body, typeof(MethodCallNode));
        var containsCall = (MethodCallNode)innerLambda.Body;
        Assert.AreEqual("Contains", containsCall.Method);
    }

    [TestMethod]
    public void Parser_AddedContainsMethod_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.Files.Any(f => f.AddedContains(\"TODO\")))");
        
        var whereCall = (MethodCallNode)ast;
        var outerLambda = (LambdaNode)whereCall.Arguments.First();
        var anyCall = (MethodCallNode)outerLambda.Body;
        var innerLambda = (LambdaNode)anyCall.Arguments.First();
        
        // Inner lambda body should be AddedContains call
        Assert.IsInstanceOfType(innerLambda.Body, typeof(MethodCallNode));
        var addedContainsCall = (MethodCallNode)innerLambda.Body;
        Assert.AreEqual("AddedContains", addedContainsCall.Method);
        
        // Argument should be string "TODO"
        var args = addedContainsCall.Arguments.ToList();
        Assert.AreEqual(1, args.Count);
        Assert.IsInstanceOfType(args[0], typeof(StringLiteralNode));
        Assert.AreEqual("TODO", ((StringLiteralNode)args[0]).Value);
    }

    // ==================== Commits.Diff Property Tests ====================

    [TestMethod]
    public void Parser_CommitsDiffFilesChanged_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.FilesChanged > 5)");
        
        // Root should be MethodCallNode for Where
        Assert.IsInstanceOfType(ast, typeof(MethodCallNode));
        var whereCall = (MethodCallNode)ast;
        Assert.AreEqual("Where", whereCall.Method);
        
        // Target should be IdentifierNode "Commits"
        Assert.IsInstanceOfType(whereCall.Target, typeof(IdentifierNode));
        Assert.AreEqual("Commits", ((IdentifierNode)whereCall.Target).Name);
        
        // Argument should be a lambda: c => c.Diff.FilesChanged > 5
        var lambda = (LambdaNode)whereCall.Arguments.First();
        Assert.AreEqual("c", lambda.Parameter);
        
        // Lambda body should be BinaryNode with ">"
        Assert.IsInstanceOfType(lambda.Body, typeof(BinaryNode));
        var binary = (BinaryNode)lambda.Body;
        Assert.AreEqual(">", binary.Operator);
        
        // Left side: c.Diff.FilesChanged (nested member access)
        Assert.IsInstanceOfType(binary.Left, typeof(MemberAccessNode));
        var filesChanged = (MemberAccessNode)binary.Left;
        Assert.AreEqual("FilesChanged", filesChanged.Member);
        
        // Target of FilesChanged should be c.Diff
        Assert.IsInstanceOfType(filesChanged.Target, typeof(MemberAccessNode));
        var diff = (MemberAccessNode)filesChanged.Target;
        Assert.AreEqual("Diff", diff.Member);
        
        // Target of Diff should be c
        Assert.IsInstanceOfType(diff.Target, typeof(IdentifierNode));
        Assert.AreEqual("c", ((IdentifierNode)diff.Target).Name);
    }

    [TestMethod]
    public void Parser_CommitsDiffFilesAny_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.Files.Any(f => f.Path.Contains(\"cs\")))");
        
        var whereCall = (MethodCallNode)ast;
        var outerLambda = (LambdaNode)whereCall.Arguments.First();
        
        // Lambda body should be Any call on c.Diff.Files
        Assert.IsInstanceOfType(outerLambda.Body, typeof(MethodCallNode));
        var anyCall = (MethodCallNode)outerLambda.Body;
        Assert.AreEqual("Any", anyCall.Method);
        
        // Target should be c.Diff.Files
        Assert.IsInstanceOfType(anyCall.Target, typeof(MemberAccessNode));
        var files = (MemberAccessNode)anyCall.Target;
        Assert.AreEqual("Files", files.Member);
        
        // Target of Files should be c.Diff
        Assert.IsInstanceOfType(files.Target, typeof(MemberAccessNode));
        var diff = (MemberAccessNode)files.Target;
        Assert.AreEqual("Diff", diff.Member);
    }

    [TestMethod]
    public void Parser_CommitsDiffAddedContains_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.Files.Any(f => f.AddedContains(\"TODO\")))");
        
        var whereCall = (MethodCallNode)ast;
        var outerLambda = (LambdaNode)whereCall.Arguments.First();
        var anyCall = (MethodCallNode)outerLambda.Body;
        var innerLambda = (LambdaNode)anyCall.Arguments.First();
        
        // Inner lambda body should be AddedContains call
        Assert.IsInstanceOfType(innerLambda.Body, typeof(MethodCallNode));
        var addedContainsCall = (MethodCallNode)innerLambda.Body;
        Assert.AreEqual("AddedContains", addedContainsCall.Method);
    }

    // ==================== DiffData Direct Content Search Parser Tests ====================

    [TestMethod]
    public void Parser_DiffAddedContainsDirect_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.AddedContains(\"TODO\"))");
        
        var whereCall = (MethodCallNode)ast;
        var lambda = (LambdaNode)whereCall.Arguments.First();
        
        // Lambda body should be AddedContains call directly on Diff
        Assert.IsInstanceOfType(lambda.Body, typeof(MethodCallNode));
        var addedContainsCall = (MethodCallNode)lambda.Body;
        Assert.AreEqual("AddedContains", addedContainsCall.Method);
        
        // Target should be c.Diff
        Assert.IsInstanceOfType(addedContainsCall.Target, typeof(MemberAccessNode));
        var diff = (MemberAccessNode)addedContainsCall.Target;
        Assert.AreEqual("Diff", diff.Member);
    }

    [TestMethod]
    public void Parser_DiffDeletedContainsDirect_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.DeletedContains(\"old\"))");
        
        var whereCall = (MethodCallNode)ast;
        var lambda = (LambdaNode)whereCall.Arguments.First();
        
        Assert.IsInstanceOfType(lambda.Body, typeof(MethodCallNode));
        var deletedContainsCall = (MethodCallNode)lambda.Body;
        Assert.AreEqual("DeletedContains", deletedContainsCall.Method);
    }

    [TestMethod]
    public void Parser_DiffContentContainsDirect_ParsesCorrectAST()
    {
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.ContentContains(\"feature\"))");
        
        var whereCall = (MethodCallNode)ast;
        var lambda = (LambdaNode)whereCall.Arguments.First();
        
        Assert.IsInstanceOfType(lambda.Body, typeof(MethodCallNode));
        var contentContainsCall = (MethodCallNode)lambda.Body;
        Assert.AreEqual("ContentContains", contentContainsCall.Method);
    }

    // ==================== DiffData Direct Content Search Execution Tests ====================

    private static List<CommitInfo> CreateCommitsWithDiffContent()
    {
        return new List<CommitInfo>
        {
            new()
            {
                Sha = "abc1234",
                Message = "Add TODO comments",
                MessageShort = "Add TODO comments",
                AuthorName = "Alice",
                AuthorEmail = "alice@test.com",
                AuthorWhen = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero),
                Diff = new DiffData
                {
                    Files = new List<FileChange>
                    {
                        new() { Path = "file1.cs", AddedContent = new List<string> { "// TODO: fix this", "new code" } }
                    }
                }
            },
            new()
            {
                Sha = "def5678",
                Message = "Remove old code",
                MessageShort = "Remove old code",
                AuthorName = "Bob",
                AuthorEmail = "bob@test.com",
                AuthorWhen = new DateTimeOffset(2025, 1, 2, 11, 0, 0, TimeSpan.Zero),
                Diff = new DiffData
                {
                    Files = new List<FileChange>
                    {
                        new() { Path = "file2.cs", DeletedContent = new List<string> { "old function", "deprecated code" } }
                    }
                }
            },
            new()
            {
                Sha = "ghi9012",
                Message = "Refactor feature",
                MessageShort = "Refactor feature",
                AuthorName = "Alice",
                AuthorEmail = "alice@test.com",
                AuthorWhen = new DateTimeOffset(2025, 1, 3, 12, 0, 0, TimeSpan.Zero),
                Diff = new DiffData
                {
                    Files = new List<FileChange>
                    {
                        new() { Path = "file3.cs", AddedContent = new List<string> { "new implementation" }, DeletedContent = new List<string> { "old implementation" } }
                    }
                }
            }
        };
    }

    [TestMethod]
    public void Execute_DiffAddedContainsDirect_FiltersCorrectly()
    {
        var builder = new LinqExpressionBuilder(CreateCommitsWithDiffContent());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.AddedContains(\"TODO\"))");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        var commits = result.ToList();
        Assert.AreEqual(1, commits.Count);
        Assert.AreEqual("abc1234", commits[0].Sha);
    }

    [TestMethod]
    public void Execute_DiffDeletedContainsDirect_FiltersCorrectly()
    {
        var builder = new LinqExpressionBuilder(CreateCommitsWithDiffContent());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.DeletedContains(\"deprecated\"))");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        var commits = result.ToList();
        Assert.AreEqual(1, commits.Count);
        Assert.AreEqual("def5678", commits[0].Sha);
    }

    [TestMethod]
    public void Execute_DiffContentContainsDirect_FiltersCorrectly()
    {
        var builder = new LinqExpressionBuilder(CreateCommitsWithDiffContent());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Diff.ContentContains(\"implementation\"))");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        var commits = result.ToList();
        Assert.AreEqual(1, commits.Count);
        Assert.AreEqual("ghi9012", commits[0].Sha);
    }

    [TestMethod]
    public void CommitInfo_Diff_HasFilesProperty()
    {
        var commit = new CommitInfo
        {
            Sha = "abc123",
            Diff = new DiffData
            {
                Files = new List<FileChange>
                {
                    new() { Path = "test.cs", LinesAdded = 50 }
                }
            }
        };
        Assert.AreEqual(1, commit.Diff.Files.Count);
        Assert.AreEqual(50, commit.Diff.TotalLinesAdded);
    }
}
