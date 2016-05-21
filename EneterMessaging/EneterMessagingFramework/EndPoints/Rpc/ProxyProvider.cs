/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT && !COMPACT_FRAMEWORK && !XAMARIN_IOS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.EndPoints.Rpc
{
    internal static class ProxyProvider
    {
        private class SoftwareEngineer
        {
            public Type ImplementProxy(Type interfaceType)
            {
                using (EneterTrace.Entering())
                {
                    myTypeBuilder = CreateTypeBuilder(interfaceType);

                    // Implement private fields.
                    myPrivateFields["myRemoteCaller"] = myTypeBuilder.DefineField("myRemoteCaller", typeof(Func<string, object[], object>), FieldAttributes.Private);
                    myPrivateFields["mySubscribe"] = myTypeBuilder.DefineField("mySubscribe", typeof(Action<string, Delegate, Action<object, EventArgs>>), FieldAttributes.Private);
                    myPrivateFields["myUnsubscribe"] = myTypeBuilder.DefineField("myUnsubscribe", typeof(Action<string, Delegate>), FieldAttributes.Private);

                    // Implement constructor.
                    FieldBuilder[] anInputParamFields = new FieldBuilder[] { myPrivateFields["myRemoteCaller"], myPrivateFields["mySubscribe"], myPrivateFields["myUnsubscribe"] };
                    ImplementConstructor(anInputParamFields);

                    // Implement methods of the interface.
                    ImplementMethods(interfaceType.GetMethods());

                    // Implement events.
                    ImplementEvents(interfaceType.GetEvents());

                    // Return type of the class that implements the given interface.
                    Type aTypeOfImplementedClass = myTypeBuilder.CreateType();
                    return aTypeOfImplementedClass;
                }
            }

            private TypeBuilder CreateTypeBuilder(Type interfaceType)
            {
                using (EneterTrace.Entering())
                {
                    // Create the assembly
                    AssemblyName anAssemblyName = new AssemblyName("tmp_" + interfaceType.Name + "_" + Guid.NewGuid().ToString());
                    AppDomain anAppDomain = AppDomain.CurrentDomain;
                    AssemblyBuilder anAssemblyBuilder = anAppDomain.DefineDynamicAssembly(anAssemblyName,
                        AssemblyBuilderAccess.Run);

                    // Create the module = the logical collection of code.
                    ModuleBuilder aModuleBuilder = anAssemblyBuilder.DefineDynamicModule(anAssemblyName.Name, false);

                    // Create the type.
                    string aDerivedClassName = interfaceType.Namespace;
                    if (!string.IsNullOrEmpty(aDerivedClassName))
                    {
                        aDerivedClassName += ".";
                    }
                    aDerivedClassName += "Impl_" + interfaceType.Name;
                    TypeBuilder aTypeBuilder = aModuleBuilder.DefineType(aDerivedClassName, TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.NotPublic);

                    aTypeBuilder.AddInterfaceImplementation(interfaceType);

                    return aTypeBuilder;
                }
            }

            private void ImplementConstructor(FieldBuilder[] inputParamFields)
            {
                using (EneterTrace.Entering())
                {
                    // Get input parameters.
                    Type[] anInputParameters = inputParamFields.Select(x => x.FieldType).ToArray();

                    ConstructorBuilder aConstructor = myTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, anInputParameters);
                    ILGenerator aGen = aConstructor.GetILGenerator();

                    // Call constructor of the base class.
                    aGen.Emit(OpCodes.Ldarg_0);
                    aGen.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));

                    // Implement code that initializes private fields from input parameters.
                    for (int i = 0; i < inputParamFields.Length; ++i)
                    {
                        aGen.Emit(OpCodes.Ldarg_0);
                        Ldarg(aGen, i + 1);
                        aGen.Emit(OpCodes.Stfld, inputParamFields[i]);
                    }

                    // return;
                    aGen.Emit(OpCodes.Ret);
                }
            }

            private void ImplementEvents(EventInfo[] events)
            {
                using (EneterTrace.Entering())
                {


                    foreach (EventInfo anEventInfo in events)
                    {
                        // Implement Add method.
                        MethodBuilder anAddMethodImpl = ImplementSubscribeEventMethod(anEventInfo);

                        // Implement Remove method.
                        MethodBuilder aRemoveMethodImpl = ImplementUnsubscribeEventMethod(anEventInfo);

                        EventBuilder anEventImpl = myTypeBuilder.DefineEvent(anEventInfo.Name, EventAttributes.None, anEventInfo.EventHandlerType);
                        anEventImpl.SetAddOnMethod(anAddMethodImpl);
                        anEventImpl.SetRemoveOnMethod(aRemoveMethodImpl);
                    }
                }
            }

            private MethodBuilder ImplementSubscribeEventMethod(EventInfo eventInfo)
            {
                using (EneterTrace.Entering())
                {
                    MethodBuilder aMethodImpl = myTypeBuilder.DefineMethod("add_" + eventInfo.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual,
                        eventInfo.GetAddMethod().ReturnType,
                        Array.ConvertAll(eventInfo.GetAddMethod().GetParameters(), x => x.ParameterType));
                    myTypeBuilder.DefineMethodOverride(aMethodImpl, eventInfo.GetAddMethod());
                    ILGenerator aGen = aMethodImpl.GetILGenerator();

                    // If it is EventHandler. (Without generics)
                    if (!eventInfo.EventHandlerType.IsGenericType)
                    {
                        // Push the field reference to the stack.
                        aGen.Emit(OpCodes.Ldarg_0);
                        aGen.Emit(OpCodes.Ldfld, myPrivateFields["mySubscribe"]);

                        // Push the name of the event to the stack.
                        aGen.Emit(OpCodes.Ldstr, eventInfo.Name);

                        // Push reference to EventHandler.Invoke to the stack.
                        aGen.Emit(OpCodes.Ldarg_1);

                        // Push reference to EventHandler.Invoke to the stack.
                        aGen.Emit(OpCodes.Ldarg_1);
                        aGen.Emit(OpCodes.Ldftn, typeof(EventHandler).GetMethod("Invoke"));
                        aGen.Emit(OpCodes.Newobj, typeof(Action<object, EventArgs>).GetConstructor(new Type[] { typeof(object), typeof(IntPtr) }));

                        // Call mySubscribe delegate.
                        aGen.Emit(OpCodes.Callvirt, myPrivateFields["mySubscribe"].FieldType.GetMethod("Invoke"));

                        aGen.Emit(OpCodes.Ret);
                    }
                    // If it EventHandler<...>
                    else
                    {
                        // 1st implement the helper inner class to wrap event handlers.
                        TypeBuilder aHandlerTypeBuilder = myTypeBuilder.DefineNestedType(eventInfo.Name + "_Wrapper",
                        TypeAttributes.NestedPrivate | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoClass | TypeAttributes.AnsiClass);

                        // Implement public field holding the wrapped event handler.
                        FieldBuilder aValueField = aHandlerTypeBuilder.DefineField("Value", eventInfo.EventHandlerType, FieldAttributes.Public);

                        // Implement default constructor.
                        ConstructorBuilder aWrapperConstructor = aHandlerTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new Type[0]);
                        ILGenerator aConstructorGen = aWrapperConstructor.GetILGenerator();
                        aConstructorGen.Emit(OpCodes.Ldarg_0);
                        aConstructorGen.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));
                        aConstructorGen.Emit(OpCodes.Ret);

                        // Implement Invoke method.
                        MethodBuilder anInvokeMethodImpl = aHandlerTypeBuilder.DefineMethod("Invoke", MethodAttributes.Public, typeof(void), new Type[] { typeof(object), typeof(EventArgs) });
                        ILGenerator aInvokeMethodGen = anInvokeMethodImpl.GetILGenerator();
                        aInvokeMethodGen.Emit(OpCodes.Ldarg_0);
                        aInvokeMethodGen.Emit(OpCodes.Ldfld, aValueField);

                        aInvokeMethodGen.Emit(OpCodes.Ldarg_1);

                        aInvokeMethodGen.Emit(OpCodes.Ldarg_2);
                        aInvokeMethodGen.Emit(OpCodes.Castclass, eventInfo.EventHandlerType.GetGenericArguments()[0]);

                        aInvokeMethodGen.Emit(OpCodes.Callvirt, eventInfo.EventHandlerType.GetMethod("Invoke"));

                        aInvokeMethodGen.Emit(OpCodes.Ret);

                        aHandlerTypeBuilder.CreateType();


                        // 2nd implement the subscribing method for the event.
                        LocalBuilder aSubscriber = aGen.DeclareLocal(typeof(Action<object, EventArgs>));
                        LocalBuilder aHandlerWrapperBuilder = aGen.DeclareLocal(aHandlerTypeBuilder);

                        // instantiate the handler wrapper helper class and store it to the local variable with index 1.
                        aGen.Emit(OpCodes.Newobj, aWrapperConstructor);
                        aGen.Emit(OpCodes.Stloc_1);

                        // Assign the incoming event handler to the 'Value' field of instantiated helper.
                        aGen.Emit(OpCodes.Ldloc_1);
                        aGen.Emit(OpCodes.Ldarg_1);
                        aGen.Emit(OpCodes.Stfld, aValueField);

                        // Push the reference of the helper stored in the local variable with index 1 to the tack.
                        aGen.Emit(OpCodes.Ldloc_1);

                        // Push the reference to the invoke method of the helper wrapper to the stack.
                        aGen.Emit(OpCodes.Ldftn, anInvokeMethodImpl);

                        // Instantiate the lambda taking 2 arguments from the stack.
                        aGen.Emit(OpCodes.Newobj, aSubscriber.LocalType.GetConstructor(new Type[] { typeof(object), typeof(IntPtr) }));

                        // Store instantiated lambda in the local varible with index 0.
                        aGen.Emit(OpCodes.Stloc_0);

                        // Push the reference of the 'mySubscriber' delegate to the stack.
                        aGen.Emit(OpCodes.Ldarg_0);
                        aGen.Emit(OpCodes.Ldfld, myPrivateFields["mySubscribe"]);

                        // Push the name of the event to the stack.
                        aGen.Emit(OpCodes.Ldstr, eventInfo.Name);

                        // Push the value of the Value field from the helper class to the stack.
                        aGen.Emit(OpCodes.Ldloc_1);
                        aGen.Emit(OpCodes.Ldfld, aValueField);

                        // Push the reference of the lambda from the local variable with index 0 to the stack.
                        aGen.Emit(OpCodes.Ldloc_0);

                        // Call mySubscribe delegate.
                        aGen.Emit(OpCodes.Callvirt, myPrivateFields["mySubscribe"].FieldType.GetMethod("Invoke"));

                        aGen.Emit(OpCodes.Ret);
                    }

                    return aMethodImpl;
                }
            }

            private MethodBuilder ImplementUnsubscribeEventMethod(EventInfo eventInfo)
            {
                using (EneterTrace.Entering())
                {
                    MethodBuilder aMethodImpl = myTypeBuilder.DefineMethod("remove_" + eventInfo.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual,
                        eventInfo.GetAddMethod().ReturnType,
                        Array.ConvertAll(eventInfo.GetAddMethod().GetParameters(), x => x.ParameterType));
                    myTypeBuilder.DefineMethodOverride(aMethodImpl, eventInfo.GetRemoveMethod());

                    ILGenerator aGen = aMethodImpl.GetILGenerator();

                    // Push the field reference to the stack.
                    aGen.Emit(OpCodes.Ldarg_0);
                    aGen.Emit(OpCodes.Ldfld, myPrivateFields["myUnsubscribe"]);

                    // Push the name of the event to the stack.
                    aGen.Emit(OpCodes.Ldstr, eventInfo.Name);

                    // Push the delegate from the 'value' to the stack.
                    aGen.Emit(OpCodes.Ldarg_1);

                    // Call myUnsubscribe delegate.
                    aGen.Emit(OpCodes.Callvirt, myPrivateFields["myUnsubscribe"].FieldType.GetMethod("Invoke"));

                    aGen.Emit(OpCodes.Ret);

                    return aMethodImpl;
                }
            }

            private void ImplementMethods(MethodInfo[] methods)
            {
                using (EneterTrace.Entering())
                {
                    foreach (MethodInfo aMethodInfo in methods)
                    {
                        if (aMethodInfo.Name.Contains("add_") ||
                            aMethodInfo.Name.Contains("remove_"))
                        {
                            continue;
                        }

                        ParameterInfo[] aParameterInfos = aMethodInfo.GetParameters();

                        // Define method/function overriding the interface method.
                        MethodBuilder aMethodImpl = myTypeBuilder.DefineMethod(
                            aMethodInfo.Name,
                            MethodAttributes.Public | MethodAttributes.Virtual,
                            aMethodInfo.ReturnType,
                            Array.ConvertAll(aParameterInfos, x => x.ParameterType));
                        myTypeBuilder.DefineMethodOverride(aMethodImpl, aMethodInfo);


                        ILGenerator aGen = aMethodImpl.GetILGenerator();

                        // Declare local variable with index 0 for the array.
                        aGen.DeclareLocal(typeof(object[]));

                        // Push the field reference to the stack.
                        aGen.Emit(OpCodes.Ldarg_0);
                        aGen.Emit(OpCodes.Ldfld, myPrivateFields["myRemoteCaller"]);

                        // Prepare the 1. input parameter for the delegate.
                        // push reference of the string to the stack.
                        aGen.Emit(OpCodes.Ldstr, aMethodInfo.Name);

                        // Prepare the 2. input parameter for the delegate.
                        // Allocate array where input parameters of the method will be stored.
                        Newarr<object>(aGen, aParameterInfos.Length);

                        // Pop the reference of the array from the stack and store it in the local variable with index 0.
                        aGen.Emit(OpCodes.Stloc_0);

                        // Store input parameters of this method in the array.
                        ArgsToArray(aGen, 0, aParameterInfos);

                        // Push the reference of the array from the variable to the stack.
                        Ldloc(aGen, 0);

                        // Call the delegate with prepared input parameters that are stored on the stack.
                        // Note: stack contains: delegate, string, object[]
                        aGen.Emit(OpCodes.Callvirt, typeof(Func<string, object[], object>).GetMethod("Invoke"));

                        // If it is a function.
                        if (aMethodInfo.ReturnType != typeof(void))
                        {
                            // Unbox or cast to the return value type.
                            aGen.Emit(OpCodes.Unbox_Any, aMethodInfo.ReturnType);
                        }
                        else
                        {
                            aGen.Emit(OpCodes.Pop);
                        }

                        // Return from the method or function.
                        // If it is a return from the function the return value is on the stack.
                        aGen.Emit(OpCodes.Ret);
                    }
                }
            }

            private void Newarr<T>(ILGenerator gen, int length)
            {
                using (EneterTrace.Entering())
                {
                    // Push the size of the array to the stack.
                    Ldc_I4(gen, length);

                    // Allocate the array and store the reference on the stack.
                    gen.Emit(OpCodes.Newarr, typeof(T));
                }
            }

            private void ArgsToArray(ILGenerator gen, int arrayVariableIdx, ParameterInfo[] args)
            {
                using (EneterTrace.Entering())
                {
                    // Note: we need to start from index 1 because 0 is 'this'.
                    for (int i = 0; i < args.Length; ++i)
                    {
                        // Push reference of array variable to the stack.
                        Ldloc(gen, arrayVariableIdx);

                        // Push the item index to the stack.
                        Ldc_I4(gen, i);

                        // Push the value to the stack.
                        Ldarg(gen, i + 1);

                        // If the parameter is not a reference then it must be boxed.
                        if (!args[i].ParameterType.IsByRef)
                        {
                            // Pops the value from the stack and push the reference to the stack.
                            gen.Emit(OpCodes.Box, args[i].ParameterType);
                        }

                        // Pop from the stack and store it in the array.
                        gen.Emit(OpCodes.Stelem_Ref);
                    }
                }
            }

            private void Ldc_I4(ILGenerator gen, int v)
            {
                if (v <= 8)
                {
                    gen.Emit(myLdc_I4_x[v]);
                }
                else
                {
                    gen.Emit(OpCodes.Ldc_I4, v);
                }
            }

            private void Ldarg(ILGenerator gen, int paramIdx)
            {
                if (paramIdx <= 3)
                {
                    gen.Emit(myLdarg_x[paramIdx]);
                }
                else
                {
                    gen.Emit(OpCodes.Ldarg, paramIdx);
                }
            }

            private void Ldloc(ILGenerator gen, int variableIdx)
            {
                if (variableIdx <= 3)
                {
                    gen.Emit(myLdloc_x[variableIdx]);
                }
                else
                {
                    gen.Emit(OpCodes.Ldloc, variableIdx);
                }
            }

            private void Trace(ILGenerator gen, string message)
            {
                MethodInfo aMethodInfo = typeof(System.Console).GetMethod("WriteLine", new Type[] { typeof(string) });

                gen.Emit(OpCodes.Ldstr, message);
                gen.Emit(OpCodes.Call, aMethodInfo);
            }


            private readonly OpCode[] myLdc_I4_x = new OpCode[]
                {
                    OpCodes.Ldc_I4_0, OpCodes.Ldc_I4_1, OpCodes.Ldc_I4_2, OpCodes.Ldc_I4_3,
                    OpCodes.Ldc_I4_4, OpCodes.Ldc_I4_5, OpCodes.Ldc_I4_6, OpCodes.Ldc_I4_7, OpCodes.Ldc_I4_8
                };

            private readonly OpCode[] myLdarg_x = new OpCode[]
                {
                    OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3
                };

            private readonly OpCode[] myLdloc_x = new OpCode[]
                {
                    OpCodes.Ldloc_0, OpCodes.Ldloc_1, OpCodes.Ldloc_2, OpCodes.Ldloc_3
                };


            private TypeBuilder myTypeBuilder;
            private Dictionary<string, FieldBuilder> myPrivateFields = new Dictionary<string, FieldBuilder>();
        }

        public static TServiceInterface CreateInstance<TServiceInterface>(Func<string, object[], object> call,
            Action<string, Delegate, Action<object, EventArgs>> subscribe,
            Action<string, Delegate> unsubscribe)
        {
            using (EneterTrace.Entering())
            {
                Type anInterfaceType = typeof(TServiceInterface);
                if (!anInterfaceType.IsInterface)
                {
                    string anErrorMessage = "Provided service interface '" + anInterfaceType.Name + "' is not interface.";
                    EneterTrace.Error(anErrorMessage);
                    throw new ArgumentException(anErrorMessage);
                }


                Type anImplementedClass = null;
                using (ThreadLock.Lock(myImplementedInterfaces))
                {
                    myImplementedInterfaces.TryGetValue(anInterfaceType, out anImplementedClass);
                    if (anImplementedClass == null)
                    {
                        // Dynamically implement proxy for the given interface.
                        SoftwareEngineer anEngineer = new SoftwareEngineer();
                        anImplementedClass = anEngineer.ImplementProxy(anInterfaceType);

                        myImplementedInterfaces[anInterfaceType] = anImplementedClass;
                    }

                    // Instantiate the proxy.
                    TServiceInterface aProxyInstance = (TServiceInterface)Activator.CreateInstance(anImplementedClass, call, subscribe, unsubscribe);
                    return aProxyInstance;
                }
            }
        }

        private static Dictionary<Type, Type> myImplementedInterfaces = new Dictionary<Type, Type>();
    }
}


#endif