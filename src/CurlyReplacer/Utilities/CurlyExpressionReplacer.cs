using System;
using System.Linq.Dynamic.Core;

public static class CurlyExpressionReplacer
{

    public static string ReplaceCurlyExpressions<TContext>(this string input, TContext context)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        
        return CurlyReplacer.Replace(input, expression => EvaluateExpression(expression, context));
    }

    public static string ReplaceCurlyExpressions(this string input)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        
        return CurlyReplacer.Replace(input, expression => EvaluateExpression(expression, new { }));
    }

    private static string EvaluateExpression<TContext>(string expression, TContext context)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return string.Empty;
        }

        try
        {
            var lambda = DynamicExpressionParser.ParseLambda<TContext, object>(null, false, expression);
            try
            {
                var compiled = lambda.Compile();
                var result = compiled.Invoke(context);
                return result?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to evaluate dynamic expression: {expression}", ex);
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to evaluate dynamic expression: {expression}", ex);
        }
    }
}
