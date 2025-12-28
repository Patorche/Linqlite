using System.Linq.Expressions;
using ExpressionTreeToString;

public static class ExprDump
{
    public static void Dump(Expression expr)
    {
        Console.WriteLine(expr.ToString("C#"));
        Console.WriteLine("--------------------------------------------------");
    }
}
