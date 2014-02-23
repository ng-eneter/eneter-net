/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

#if !COMPACT_FRAMEWORK20

using System;
using System.Collections.Generic;
using System.Reflection;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.EndPoints.Rpc
{
    internal static class ServiceInterfaceChecker
    {
        public static void CheckForClient<T>()
        {
            Check<T>();

            // In addition the interface must be public.
            // Otherwise the runtime implementation of the proxy will fails because the implemented interface will be not accessible.
            if (!typeof(T).IsPublic)
            {
                string anErrorMessage = "The type '" + typeof(T).Name + "' is not public.";
                EneterTrace.Error(anErrorMessage);
                throw new InvalidOperationException(anErrorMessage);
            }
        }

        public static void Check<T>()
        {
            // It must be an interface.
            if (!typeof(T).IsInterface)
            {
                string anErrorMessage = "The type '" + typeof(T).Name + "' is not an interface.";
                EneterTrace.Error(anErrorMessage);
                throw new InvalidOperationException(anErrorMessage);
            }

            HashSet<string> aUsedNames = new HashSet<string>();

            // Check declared methods and arguments of all public methods.
            foreach (MethodInfo aMethodInfo in typeof(T).GetMethods())
            {
                // Overloading is not allowed.
                if (aUsedNames.Contains(aMethodInfo.Name))
                {
                    string anErrorMessage = "The interface already contains method or event with the name '" + aMethodInfo.Name + "'.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                aUsedNames.Add(aMethodInfo.Name);
            }
        }
    }
}

#endif