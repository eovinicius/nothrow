using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NoThrow.Tests;

// README.md is also the NuGet package readme. Its C# snippets are compiled here, with C# 12 (what net8.0 consumers get)
// and nullable enabled, so the documentation cannot drift from the API. Diagnostics point at README.md lines.
public partial class ReadmeTests
{
    private const string ReadmeFileName = "README.md";

    [Fact]
    public void Snippets_AreAllExtracted()
    {
        var readme = File.ReadAllText(ReadmePath);

        Assert.NotEmpty(Snippets);
        Assert.Equal(CSharpFence().Count(readme), Snippets.Count);
        Assert.DoesNotMatch(OtherCSharpFence(), readme); // ```cs or ```c# would be skipped: use ```csharp
    }

    [Fact]
    public void Snippets_Compile()
    {
        var source = BuildSource(Snippets);
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12));
        var compilation = CSharpCompilation.Create(
            "ReadmeSnippets",
            [tree],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        // Errors, and nullable warnings: the README promises the snippets are warning-free with nullable enabled.
        var problems = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error
                || (d.Severity == DiagnosticSeverity.Warning && d.Id.StartsWith("CS86", StringComparison.Ordinal)))
            .Select(Describe)
            .ToList();

        Assert.True(problems.Count == 0, "README snippets do not compile:" + Environment.NewLine + string.Join(Environment.NewLine, problems));
    }

    private static string ReadmePath => Path.Combine(AppContext.BaseDirectory, ReadmeFileName);

    private static List<Snippet> Snippets => Extract(File.ReadAllLines(ReadmePath));

    private static List<Snippet> Extract(string[] lines)
    {
        var snippets = new List<Snippet>();
        StringBuilder? code = null;
        var firstLine = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            if (code is null && lines[i].TrimEnd() == "```csharp")
            {
                code = new StringBuilder();
                firstLine = i + 2; // 1-based number of the line after the fence
            }
            else if (code is not null && lines[i].TrimEnd() == "```")
            {
                snippets.Add(new Snippet(snippets.Count + 1, firstLine, code.ToString()));
                code = null;
            }
            else
            {
                code?.AppendLine(lines[i]);
            }
        }

        return snippets;
    }

    // A snippet may mix type declarations, members and statements, as a C# script can. Types go to the top level;
    // members and statements go into their own class deriving from SnippetContext, which declares the fields and
    // helpers the snippets assume (_users, command, result...). #line maps every piece back to its README line.
    private static string BuildSource(IEnumerable<Snippet> snippets)
    {
        var source = new StringBuilder(Stubs);

        foreach (var snippet in snippets)
        {
            var root = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(
                snippet.Code,
                new CSharpParseOptions(LanguageVersion.CSharp12, kind: SourceCodeKind.Script)).GetRoot();

            // In script code a top-level `var x = ...;` parses as a field; without modifiers it is a local variable.
            var types = root.Members.Where(m => m is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax).ToList();
            var statements = root.Members
                .Where(m => m is GlobalStatementSyntax || (m is FieldDeclarationSyntax field && field.Modifiers.Count == 0))
                .ToList();
            var members = root.Members.Except(types).Except(statements);

            foreach (var type in types)
            {
                AppendMapped(source, snippet, type);
            }

            source.AppendLine("#line default");
            source.AppendLine($"public sealed class Snippet{snippet.Number} : SnippetContext {{");

            foreach (var member in members)
            {
                AppendMapped(source, snippet, member);
            }

            source.AppendLine("#line default");
            source.AppendLine("public async Task<object?> Run() {");

            foreach (var statement in statements)
            {
                AppendMapped(source, snippet, statement);
            }

            source.AppendLine("#line default");
            source.AppendLine("return null; } }");
        }

        return source.ToString();
    }

    private static void AppendMapped(StringBuilder source, Snippet snippet, SyntaxNode node)
    {
        var line = snippet.FirstLine + node.GetLocation().GetLineSpan().StartLinePosition.Line;

        source.AppendLine(CultureInfo.InvariantCulture, $"#line {line} \"{ReadmeFileName}\"");
        source.AppendLine(node.ToString());
    }

    // Compiles against the running framework (net8.0 or net10.0), the library and xUnit's asserts.
    private static IEnumerable<MetadataReference> References()
    {
        var runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
        var framework = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => path.StartsWith(runtimeDirectory, StringComparison.OrdinalIgnoreCase));

        return framework
            .Append(typeof(Result).Assembly.Location)
            .Append(typeof(Assert).Assembly.Location)
            .Select(path => MetadataReference.CreateFromFile(path));
    }

    private static string Describe(Diagnostic diagnostic)
    {
        var span = diagnostic.Location.GetMappedLineSpan();

        return $"  {span.Path}({span.StartLinePosition.Line + 1}): {diagnostic.Id}: {diagnostic.GetMessage(CultureInfo.InvariantCulture)}";
    }

    [GeneratedRegex(@"^```csharp\s*$", RegexOptions.Multiline)]
    private static partial Regex CSharpFence();

    [GeneratedRegex(@"^```\s*(cs|c#|C#)\s*$", RegexOptions.Multiline)]
    private static partial Regex OtherCSharpFence();

    private sealed record Snippet(int Number, int FirstLine, string Code);

    // What the snippets take for granted. Kept minimal: only members some snippet uses.
    private const string Stubs = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Text.Json;
        global using System.Threading.Tasks;
        global using NoThrow;
        global using Xunit;

        #pragma warning disable CS0108, CS0162, CS1998 // members hiding the context's, `return null` after a snippet's return, async without await

        public sealed class User
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = "";
            public bool IsActive { get; init; }
        }

        public sealed class Customer
        {
            public Result Reserve(decimal amount) => Result.Success();
        }

        public sealed class Order
        {
            public Guid Id { get; init; }
            public decimal Total { get; init; }
            public static Result<Order> Place(Customer customer, Email email, Quantity quantity) => new Order();
        }

        public sealed class Quantity
        {
            public static Result<Quantity> Create(int value) => new Quantity();
        }

        public sealed record PlaceOrder(string Email, int Quantity, Guid CustomerId);

        public sealed record OrderCreated(Guid OrderId);

        public static class OrderErrors
        {
            public static readonly Error NotFound = Error.NotFound("Order.NotFound", "Order not found.");
            public static readonly Error CreationFailed = Error.Failure("Order.CreationFailed", "The order could not be created.");
        }

        public sealed class UserStore : List<User>
        {
            public Task<Result<User>> FindAsync(Guid id) => Task.FromResult<Result<User>>(new User());
        }

        public sealed class CustomerStore
        {
            public Task<Result<Customer>> FindAsync(Guid id) => Task.FromResult<Result<Customer>>(new Customer());
        }

        public sealed class OrderStore
        {
            public Order? FindOrDefault(Guid id) => null;
            public Result<Order> Create(User user, IReadOnlyList<Guid> items) => new Order();
            public Task<Result<Order>> CreateAsync(User user, IReadOnlyList<Guid> items) => Task.FromResult<Result<Order>>(new Order());
            public Task AddAsync(Order order) => Task.CompletedTask;
        }

        public sealed class Log
        {
            public void Info(string message) { }
            public void Warn(string message) { }
        }

        public sealed class Bus
        {
            public Task PublishAsync(object message) => Task.CompletedTask;
        }

        public sealed class OrderHandler
        {
            public Task<Result<Guid>> Handle(PlaceOrder command) => Task.FromResult<Result<Guid>>(Guid.NewGuid());
        }

        public abstract class SnippetContext
        {
            protected readonly UserStore _users = new();
            protected readonly CustomerStore _customers = new();
            protected readonly OrderStore _orders = new();
            protected readonly Log _log = new();
            protected readonly Bus _bus = new();
            protected readonly OrderHandler handler = new();
            protected readonly PlaceOrder command = new("someone@example.com", 1, Guid.Empty);
            protected readonly Guid userId = Guid.Empty;
            protected readonly IReadOnlyList<Guid> items = [];
            protected readonly Result<User> result = Result.Success(new User());

            protected Result<User> Find(Guid id) => result;

            protected static object NotFound() => new();
        }

        """;
}
