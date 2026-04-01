using System;
using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;

public static class CurlyExpressionReplacer
{
    private static readonly object EmptyContext = new();

    public static string ReplaceCurlyExpressions<TContext>(this string input, TContext context)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        
        return CurlyReplacer.Replace(input, expression => EvaluateExpression(expression, context));
    }

    public static string ReplaceCurlyExpressions(this string input)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        
        return CurlyReplacer.Replace(input, static expression => EvaluateExpression(expression, EmptyContext));
    }

    private static string EvaluateExpression<TContext>(string expression, TContext context)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return string.Empty;
        }

        try
        {
            var compiled = ExpressionCache<TContext>.CompiledExpressions.GetOrAdd(
                expression,
                static expr => DynamicExpressionParser.ParseLambda<TContext, object>(null, false, expr).Compile());
            var result = compiled.Invoke(context);
            return result?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to evaluate dynamic expression: {expression}", ex);
        }
    }

    private static class ExpressionCache<TContext>
    {
        internal static readonly ConcurrentDictionary<string, Func<TContext, object>> CompiledExpressions =
            new(StringComparer.Ordinal);
    }
}
