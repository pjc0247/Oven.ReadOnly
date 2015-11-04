using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using System.Reflection.Emit;

namespace Oven
{
    public class ReadOnlyOven
    {
        private class BakeInfo
        {
            public FieldInfo target { get; set; }
        }

        private ReadOnlyOven()
        {
        }

        private static TypeBuilder CreateType(
            BakeInfo info,
            Type intf)
        {
            var implName = intf.Name + "Impl";

            var assemblyBuilder =
                AppDomain.CurrentDomain.DefineDynamicAssembly(
                    new AssemblyName(implName),
                    AssemblyBuilderAccess.Run);
            var moduleBuilder =
                assemblyBuilder.DefineDynamicModule("Module");
            var typeBuilder = moduleBuilder.DefineType(
                implName,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout,
                null,
                new Type[] { intf });

            ConstructorBuilder ctor =
                typeBuilder.DefineConstructor(
                    MethodAttributes.Public,
                    CallingConventions.Standard,
                    Type.EmptyTypes);
            ILGenerator ilGen =
                ctor.GetILGenerator();

            info.target = typeBuilder.DefineField("target", typeof(object), FieldAttributes.Public);

            ilGen.Emit(OpCodes.Ret);

            return typeBuilder;
        }
        private static MethodBuilder CreateMethod(
            BakeInfo info,
            Type intf, Type impl,
            TypeBuilder typeBuilder, MethodInfo method)
        {
            var paramTypes =
                    method.GetParameters().Select(m => m.ParameterType).ToArray();
            var methodBuilder = typeBuilder.DefineMethod(
                method.Name,
                MethodAttributes.Public |
                MethodAttributes.Virtual |
                MethodAttributes.NewSlot |
                MethodAttributes.HideBySig |
                MethodAttributes.Final,
                method.ReturnType,
                paramTypes);
            var ilGen = methodBuilder.GetILGenerator();

            /* ld_this */
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, info.target);

            foreach (var param in method.GetParameters())
                ilGen.Emit(OpCodes.Ldarg, param.Position + 1);
            ilGen.Emit(OpCodes.Callvirt,
                impl.GetMethod(
                    method.Name,
                    method.GetParameters().Select(m => m.ParameterType).ToArray()));
            ilGen.Emit(OpCodes.Ret);

            return methodBuilder;
        }
        private static MethodBuilder CreateProperty(
            BakeInfo info,
            Type intf, Type impl,
            TypeBuilder typeBuilder, PropertyInfo prop)
        {
            var methodBuilder = typeBuilder.DefineMethod(
                "get_" + prop.Name,
                MethodAttributes.Public |
                MethodAttributes.Virtual |
                MethodAttributes.NewSlot |
                MethodAttributes.HideBySig |
                MethodAttributes.SpecialName |
                MethodAttributes.Final,
                prop.PropertyType,
                null);
            var ilGen = methodBuilder.GetILGenerator();

            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, info.target);
            ilGen.Emit(OpCodes.Callvirt,
                impl.GetProperty(prop.Name).GetGetMethod(true));
            ilGen.Emit(OpCodes.Ret);

            return methodBuilder;
        }

        static List<PropertyInfo> GetProperties(Type intf)
        {
            List<PropertyInfo> props = new List<PropertyInfo>();
            HashSet<Type> processed = new HashSet<Type>();
            var q = new Queue<Type>();

            q.Enqueue(intf);
            while (q.Count > 0)
            {
                var v = q.Dequeue();

                processed.Add(v);
                foreach (var i in v.GetInterfaces())
                {
                    if (processed.Contains(i))
                        continue;

                    q.Enqueue(i);
                }

                props.AddRange(v.GetProperties());
            }

            return props.Distinct().ToList();
        }
        static List<MethodInfo> GetMethods(Type intf)
        {
            List<MethodInfo> props = new List<MethodInfo>();
            HashSet<Type> processed = new HashSet<Type>();
            var q = new Queue<Type>();

            q.Enqueue(intf);
            while (q.Count > 0)
            {
                var v = q.Dequeue();

                processed.Add(v);
                foreach (var i in v.GetInterfaces())
                {
                    if (processed.Contains(i))
                        continue;

                    q.Enqueue(i);
                }

                props.AddRange(v.GetMethods());
            }

            return props.Distinct().ToList();
        }

        public static TBakeInterface Bake<TBakeInterface, TBakeImpl>(TBakeImpl impl)
        {
            var info = new BakeInfo();
            var typeBuilder = CreateType(info, typeof(TBakeInterface));

            /* black magic */
            foreach (var prop in GetProperties(typeof(TBakeInterface)))
            {
                CreateProperty(
                    info,
                    typeof(TBakeInterface), typeof(TBakeImpl),
                    typeBuilder, prop);
            }
            foreach (var method in GetMethods(typeof(TBakeInterface)))
            {
                Console.WriteLine(method.Name);
                CreateMethod(
                    info,
                    typeof(TBakeInterface), typeof(TBakeImpl),
                    typeBuilder, method);
            }

            Type type = typeBuilder.CreateType();
            object obj = Activator.CreateInstance(type);

            type.GetField("target").SetValue(obj, impl);

            return (TBakeInterface)obj;
        }
    }
}
