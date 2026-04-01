// See https://aka.ms/new-console-template for more information

using System.Linq.Dynamic.Core.CustomTypeProviders;

var context = new TestClass { Name = "Peter" };

Console.WriteLine("Hello, {{Name}}! Here are some numbers: {{Test(1, 2, 3)}}".ReplaceCurlyExpressions(context));
Console.WriteLine("Hello, {{\"Mr Femiani\"}}! Here is a computed value: {{1 + (2 * 5)}}".ReplaceCurlyExpressions());


[DynamicLinqType]
public class TestClass

{
    public string Name { get; set; } = string.Empty;
    public string Test(params int[] numbers)
    {
        return string.Join(", ", numbers);
    }
}
