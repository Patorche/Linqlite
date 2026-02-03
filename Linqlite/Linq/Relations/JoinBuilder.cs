using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Linqlite.Linq.Relations
{
    public static class JoinBuilder
    {
        public static IQueryable BuildJoin(
                                    IQueryable leftSource,
                                    Type leftType,          // ← tu ne dois plus utiliser ça
                                    IQueryable rightSource,
                                    Type rightType,
                                    string leftKeyName,
                                    string rightKeyName)
        {
            // 1. Le vrai type du left, c’est celui du IQueryable
            var realLeftType = leftSource.ElementType;

            // 2. Le vrai type du right, idem
            var realRightType = rightSource.ElementType;

            // 3. Construire les lambdas avec les bons types
            var leftParam = Expression.Parameter(realLeftType, "left");
            var rightParam = Expression.Parameter(realRightType, "right");

            var leftKeySelector =
                Expression.Lambda(
                    BuildLeftKeySelector(leftParam, leftKeyName, leftType),
                    leftParam
                );

            var _rightKey = Expression.Property(rightParam, rightKeyName);
            var keyType = leftKeySelector.Body.Type;
            var rightKeySelector = BuildRightKeySelector(rightParam, _rightKey, keyType);
          /*  var rightKeySelector =
                Expression.Lambda(
                    Expression.PropertyOrField(rightParam, rightKeyName),
                    rightParam
                );
          */

            // 4. Construire le type anonyme résultat
            var anonType = AnonymousTypeFactory.Create(new[] { ("Left", realLeftType), ("Right", realRightType) });

            var ctor = anonType.GetConstructors()[0];

            var resultSelector = Expression.Lambda(
                                    Expression.New(
                                        ctor,
                                        new Expression[] { leftParam, rightParam},
                                        anonType.GetProperty("Left"),
                                        anonType.GetProperty("Right")),
                                    leftParam,
                                    rightParam);

            // 5. Construire l’appel LeftJoin avec les bons types génériques
            var leftJoinMethod = typeof(Queryable).GetMethods()
                                    .First(m => m.Name == "LeftJoin" && m.GetParameters().Length == 5)
                                    .MakeGenericMethod(realLeftType, realRightType, leftKeySelector.Body.Type, anonType);

            var call = Expression.Call(
                leftJoinMethod,
                leftSource.Expression,
                rightSource.Expression,
                leftKeySelector,
                rightKeySelector,
                resultSelector
            );

            return leftSource.Provider.CreateQuery(call);
        }


        public static LambdaExpression BuildRightKeySelector(
            ParameterExpression param,
            Expression property,
            Type targetKeyType)
        {
            Expression body = property;

            // Si le type de la propriété ne correspond pas au type attendu,
            // on convertit (ex: long -> long?)
            if (property.Type != targetKeyType)
            {
                body = Expression.Convert(property, targetKeyType);
            }

            return Expression.Lambda(body, param);
        }

        private static Expression BuildLeftKeySelector(ParameterExpression param, string keyName, Type leftType)
        {
            Expression target = param;

            // Si le type est un type anonyme généré par ton JoinBuilder
            if (param.Type.Name.StartsWith("Anon_"))
            {
                PropertyInfo prop = FindSourceProperty(param.Type, leftType);
                var p = Expression.Parameter(param.Type, target.ToString()); 
                var leftEntity = Expression.Property(p, prop); 
                var leftKey = Expression.Property(leftEntity,keyName);
                return leftKey;
                // On descend dans la propriété Right
                //target = Expression.PropertyOrField(param, "Right");
            }

            return Expression.PropertyOrField(target, keyName);
        }

        private static PropertyInfo FindSourceProperty(Type anonType, Type leftType)
        {
            using (StreamWriter file = new StreamWriter(@"E:\logtest.txt", true)) { 
                file.WriteLine("=== FindSourceProperty ===");
                file.WriteLine("LeftType = " + leftType.FullName);
                file.WriteLine("AnonType = " + anonType.FullName);
                file.WriteLine("Anon properties:");
                foreach (var prop in anonType.GetProperties())
                {
                    file.WriteLine(" - " + prop.Name + " : " + prop.PropertyType.FullName);
                    if (prop.PropertyType == leftType)
                        return prop;

                    // Cas où le type anonyme contient un wrapper (Left, Right)
                    if (prop.PropertyType.IsGenericType &&
                        prop.PropertyType.GetGenericArguments().Contains(leftType))
                        return prop;
                } 
            }

            throw new InvalidOperationException($"Impossible de trouver {leftType} dans {anonType}");
        }



    }

}
