using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

public static class AnonymousTypeFactory
{
    private static readonly ConcurrentDictionary<string, Type> _cache = new();

    // Nouveau : cache des propriétés pour chaque type anonyme généré
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _propertyCache = new();

    public static Type Create(IEnumerable<(string Name, Type Type)> properties)
    {
        var props = new List<(string Name, Type Type)>(properties);

        // Clé de cache basée sur les noms et types
        var key = string.Join(";", props.Select(p => $"{p.Name}:{p.Type.FullName}"));

        return _cache.GetOrAdd(key, _ =>
        {
            var asmName = new AssemblyName("Linqlite_AnonymousTypes");
            var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            var moduleBuilder = asmBuilder.DefineDynamicModule("MainModule");

            var typeBuilder = moduleBuilder.DefineType(
                "Anon_" + Guid.NewGuid().ToString("N"),
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);

            // Champs privés
            var fields = new List<FieldBuilder>();
            foreach (var p in props)
            {
                var field = typeBuilder.DefineField("_" + p.Name, p.Type, FieldAttributes.Private);
                fields.Add(field);
            }

            // Constructeurs
            var ctorParams = props.Select(p => p.Type).ToArray();
            var ctorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                ctorParams);

            var il = ctorBuilder.GetILGenerator();

            // Constructeur par défaut
            var defaultCtor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                Type.EmptyTypes);

            var ilDefault = defaultCtor.GetILGenerator();
            ilDefault.Emit(OpCodes.Ldarg_0);
            ilDefault.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            ilDefault.Emit(OpCodes.Ret);


            // base()
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));

            // Affectation des champs
            for (int i = 0; i < fields.Count; i++)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg, i + 1);
                il.Emit(OpCodes.Stfld, fields[i]);
            }

            il.Emit(OpCodes.Ret);

            // Propriétés publiques
            for (int i = 0; i < props.Count; i++)
            {
                var propBuilder = typeBuilder.DefineProperty(
                    props[i].Name,
                    PropertyAttributes.HasDefault,
                    props[i].Type,
                    null);

                var getter = typeBuilder.DefineMethod(
                    "get_" + props[i].Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    props[i].Type,
                    Type.EmptyTypes);

                var ilGetter = getter.GetILGenerator();
                ilGetter.Emit(OpCodes.Ldarg_0);
                ilGetter.Emit(OpCodes.Ldfld, fields[i]);
                ilGetter.Emit(OpCodes.Ret);

                var setter = typeBuilder.DefineMethod(
                    "set_" + props[i].Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    null,
                    new[] { props[i].Type });

                var ilSetter = setter.GetILGenerator();
                ilSetter.Emit(OpCodes.Ldarg_0);
                ilSetter.Emit(OpCodes.Ldarg_1);
                ilSetter.Emit(OpCodes.Stfld, fields[i]);
                ilSetter.Emit(OpCodes.Ret);

                propBuilder.SetGetMethod(getter);
                propBuilder.SetSetMethod(setter);
            }

            var anonType = typeBuilder.CreateType();

            // Nouveau : on met en cache les propriétés du type généré
            _propertyCache[anonType] = anonType
                .GetProperties()
                .ToDictionary(p => p.Name, p => p);

            return anonType;
        });
    }

    public static PropertyInfo GetPropertyFor(Type anonType, string propertyName)
    {
        if (_propertyCache.TryGetValue(anonType, out var map)
            && map.TryGetValue(propertyName, out var prop))
        {
            return prop;
        }

        throw new InvalidOperationException(
            $"Property '{propertyName}' not found in anonymous type '{anonType.Name}'.");
    }

    public static PropertyInfo GetPropertyFor(Type anonType, Type propertyType)
    {
        if (_propertyCache.TryGetValue(anonType, out var map))
        {
            var match = map.Values.FirstOrDefault(p => p.PropertyType == propertyType);
            if (match != null)
                return match;
        }

        throw new InvalidOperationException(
            $"No property of type '{propertyType.Name}' found in anonymous type '{anonType.Name}'.");
    }
}
