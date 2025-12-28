using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq
{
    public static class ExpressionVisualizer
    {
        public static string Dump(Expression expr)
        {
            var sb = new StringBuilder();
            Dump(expr, sb, 0);
            return sb.ToString();
        }

        private static void Dump(Expression expr, StringBuilder sb, int indent)
        {
            if (expr == null)
            {
                Indent(sb, indent);
                sb.AppendLine("null");
                return;
            }

            Indent(sb, indent);
            sb.Append(expr.NodeType);
            sb.Append(" : ");
            sb.AppendLine(expr.Type.Name);

            switch (expr)
            {
                case MethodCallExpression m:
                    Indent(sb, indent + 1);
                    sb.AppendLine($"Method: {m.Method.Name}");

                    Indent(sb, indent + 1);
                    sb.AppendLine("Object:");
                    Dump(m.Object, sb, indent + 2);

                    Indent(sb, indent + 1);
                    sb.AppendLine("Arguments:");
                    foreach (var arg in m.Arguments)
                        Dump(arg, sb, indent + 2);
                    break;

                case MemberExpression me:
                    Indent(sb, indent + 1);
                    sb.AppendLine($"Member: {me.Member.Name}");

                    Indent(sb, indent + 1);
                    sb.AppendLine("Expression:");
                    Dump(me.Expression, sb, indent + 2);
                    break;

                case LambdaExpression l:
                    Indent(sb, indent + 1);
                    sb.AppendLine("Parameters:");
                    foreach (var p in l.Parameters)
                    {
                        Indent(sb, indent + 2);
                        sb.AppendLine($"{p.Name} : {p.Type.Name}");
                    }

                    Indent(sb, indent + 1);
                    sb.AppendLine("Body:");
                    Dump(l.Body, sb, indent + 2);
                    break;

                case ConstantExpression c:
                    Indent(sb, indent + 1);
                    sb.AppendLine($"Value: {c.Value}");
                    break;

                case UnaryExpression u:
                    Indent(sb, indent + 1);
                    sb.AppendLine($"Unary: {u.NodeType}");
                    Dump(u.Operand, sb, indent + 2);
                    break;

                case BinaryExpression b:
                    Indent(sb, indent + 1);
                    sb.AppendLine("Left:");
                    Dump(b.Left, sb, indent + 2);

                    Indent(sb, indent + 1);
                    sb.AppendLine("Right:");
                    Dump(b.Right, sb, indent + 2);
                    break;

                default:
                    Indent(sb, indent + 1);
                    sb.AppendLine($"Unhandled: {expr.GetType().Name}");
                    break;
            }
        }

        private static void Indent(StringBuilder sb, int indent)
            => sb.Append(' ', indent * 2);
    }

}
