using System.Linq;
using System.Linq.Dynamic.Core.CustomTypeProviders;

public class CurlyReplacerTests
{
    [Fact]
    public void Parse_WithNoCaptures_ReturnsEmpty()
    {
        var captures = CurlyReplacer.Parse("no placeholders here").ToList();
        Assert.Empty(captures);
    }

    [Fact]
    public void Parse_WithSingleCapture_ReturnsExpectedContentAndIndexes()
    {
        var captures = CurlyReplacer.Parse("before {{name}} after").ToList();

        var capture = Assert.Single(captures);
        Assert.Equal("name", capture.Content);
        Assert.Equal(7, capture.StartIndex);
        Assert.Equal(14, capture.EndIndex);
    }

    [Fact]
    public void Parse_WithMultipleCaptures_ReturnsAllInOrder()
    {
        var captures = CurlyReplacer.Parse("a {{x}} b {{y}}").ToList();

        Assert.Collection(
            captures,
            first =>
            {
                Assert.Equal("x", first.Content);
                Assert.Equal(2, first.StartIndex);
                Assert.Equal(6, first.EndIndex);
            },
            second =>
            {
                Assert.Equal("y", second.Content);
                Assert.Equal(10, second.StartIndex);
                Assert.Equal(14, second.EndIndex);
            });
    }

    [Fact]
    public void Parse_WithNestedDoubleCurlyInsideContent_PreservesNestedTokensInContent()
    {
        var captures = CurlyReplacer.Parse("{{a {{b}} c}}").ToList();

        var capture = Assert.Single(captures);
        Assert.Equal("a {{b}} c", capture.Content);
        Assert.Equal(0, capture.StartIndex);
        Assert.Equal(12, capture.EndIndex);
    }

    [Fact]
    public void Parse_WithUnmatchedOpeningDoubleCurly_YieldsNoCapture()
    {
        var captures = CurlyReplacer.Parse("abc {{oops").ToList();
        Assert.Empty(captures);
    }

    [Fact]
    public void Replace_ReplacesEachCaptureUsingContentDelegate()
    {
        var result = CurlyReplacer.Replace("Hi {{name}}, from {{city}}!", s => s.ToUpperInvariant());
        Assert.Equal("Hi NAME, from CITY!", result);
    }

    [Fact]
    public void Replace_WithNestedDoubleCurlyInCapture_ReplacesOuterCapture()
    {
        var result = CurlyReplacer.Replace("{{a {{b}} c}}", s => $"[{s}]");
        Assert.Equal("[a {{b}} c]", result);
    }

    [Fact]
    public void Replace_WhenReplacementReturnsNull_UsesEmptyString()
    {
        var result = CurlyReplacer.Replace("x {{a}} y", _ => null!);
        Assert.Equal("x  y", result);
    }

    [Fact]
    public void Replace_WithUnmatchedOpeningDoubleCurly_LeavesInputUnchanged()
    {
        var input = "x {{a";
        var result = CurlyReplacer.Replace(input, s => s.ToUpperInvariant());
        Assert.Equal(input, result);
    }
}

[DynamicLinqType]
public sealed class AnnotatedPersonContext
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public int Age { get; init; }
    public string FormatName(string firstName, string lastName) => $"{firstName} {lastName}";
    public string Test(params int[] numbers) => string.Join(", ", numbers);
}

public sealed class NonAnnotatedContext
{
    public string Echo(string value) => value;
}

public class CurlyExpressionReplacerTests
{
    [Fact]
    public void Replace_ReplacesSingleExpressionFromContext()
    {
        var context = new AnnotatedPersonContext { FirstName = "Pete", LastName = "Femiani", Age = 42 };
        var result = CurlyExpressionReplacer.ReplaceCurlyExpressions("Hello {{FirstName}}", context);

        Assert.Equal("Hello Pete", result);
    }

    [Fact]
    public void Replace_EvaluatesExpressionAsLambdaRightSide()
    {
        var context = new AnnotatedPersonContext { FirstName = "Pete", LastName = "Femiani", Age = 42 };
        var result = CurlyExpressionReplacer.ReplaceCurlyExpressions("{{FirstName + \" \" + LastName + \" is \" + Age}}", context);

        Assert.Equal("Pete Femiani is 42", result);
    }

    [Fact]
    public void Replace_WithConditionalExpression_ReturnsExpectedValue()
    {
        var context = new AnnotatedPersonContext { FirstName = "Pete", LastName = "Femiani", Age = 20 };
        var result = CurlyExpressionReplacer.ReplaceCurlyExpressions("Status: {{Age >= 21 ? \"adult\" : \"minor\"}}", context);

        Assert.Equal("Status: minor", result);
    }

    [Fact]
    public void Replace_CanInvokePublicMethodOnContext()
    {
        var context = new AnnotatedPersonContext { FirstName = "Pete", LastName = "Femiani", Age = 42 };
        var result = CurlyExpressionReplacer.ReplaceCurlyExpressions("{{FormatName(FirstName, LastName)}}", context);

        Assert.Equal("Pete Femiani", result);
    }
    [Fact]
    public void Replace_CanInvokeParamsMethodOnAnnotatedContext()
    {
        var context = new AnnotatedPersonContext { FirstName = "Pete", LastName = "Femiani", Age = 42 };
        var result = CurlyExpressionReplacer.ReplaceCurlyExpressions("{{Test(1, 2, 3)}}", context);

        Assert.Equal("1, 2, 3", result);
    }

    [Fact]
    public void Replace_WhenExpressionReturnsNull_UsesEmptyString()
    {
        var context = new AnnotatedPersonContext { FirstName = "Pete", LastName = "Femiani", Age = 42 };
        var result = CurlyExpressionReplacer.ReplaceCurlyExpressions("Value: {{null}}", context);

        Assert.Equal("Value: ", result);
    }

    [Fact]
    public void Replace_WithUnmatchedOpeningDoubleCurly_LeavesInputUnchanged()
    {
        var context = new AnnotatedPersonContext { FirstName = "Pete", LastName = "Femiani", Age = 42 };

        var input = "Hello {{FirstName";
        var result = CurlyExpressionReplacer.ReplaceCurlyExpressions(input, context);

        Assert.Equal(input, result);
    }

    [Fact]
    public void Replace_WithInvalidExpression_ThrowsInvalidOperationException()
    {
        var context = new AnnotatedPersonContext { FirstName = "Pete", LastName = "Femiani", Age = 42 };
        var ex = Assert.Throws<InvalidOperationException>(() => CurlyExpressionReplacer.ReplaceCurlyExpressions("{{NotARealMember + 1}}", context));
        Assert.Contains("Failed to evaluate dynamic expression", ex.Message);
    }
    [Fact]
    public void Replace_NoContext_EvaluatesLiteralAndArithmetic()
    {
        var input = "Label: {{\"ok\"}}, Value: {{1 + (2 * 5)}}";
        var result = input.ReplaceCurlyExpressions();

        Assert.Equal("Label: ok, Value: 11", result);
    }

    [Fact]
    public void Replace_MethodCallOnNonAnnotatedContext_ThrowsInvalidOperationException()
    {
        var context = new NonAnnotatedContext();
        var ex = Assert.Throws<InvalidOperationException>(() => CurlyExpressionReplacer.ReplaceCurlyExpressions("{{Echo(\"hi\")}}", context));
        Assert.Contains("not accessible", ex.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
