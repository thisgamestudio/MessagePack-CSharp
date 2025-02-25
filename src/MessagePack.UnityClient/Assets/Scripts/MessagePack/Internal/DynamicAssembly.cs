﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !UNITY_2018_3_OR_NEWER

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace MessagePack.Internal
{
    internal class DynamicAssembly
    {
#if NETFRAMEWORK // We don't ship a net472 target, but we might add one for debugging purposes
        private readonly string moduleName;
#endif
        private readonly AssemblyBuilder assemblyBuilder;
        private readonly ModuleBuilder moduleBuilder;

        // don't expose ModuleBuilder
        //// public ModuleBuilder ModuleBuilder { get { return moduleBuilder; } }

        private readonly object gate = new object();

        public DynamicAssembly(string moduleName)
        {
#if NETFRAMEWORK // We don't ship a net472 target, but we might add one for debugging purposes
            AssemblyBuilderAccess builderAccess = AssemblyBuilderAccess.RunAndSave;
            this.moduleName = moduleName;
#else
            AssemblyBuilderAccess builderAccess = AssemblyBuilderAccess.Run;
#endif
            this.assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(moduleName), builderAccess);
            this.moduleBuilder = this.assemblyBuilder.DefineDynamicModule(moduleName + ".dll");
        }

        /* requires lock on mono environment. see: https://github.com/neuecc/MessagePack-CSharp/issues/161 */

        public TypeBuilder DefineType(string name, TypeAttributes attr)
        {
            lock (this.gate)
            {
                return this.moduleBuilder.DefineType(name, attr);
            }
        }

        public TypeBuilder DefineType(string name, TypeAttributes attr, Type parent)
        {
            lock (this.gate)
            {
                return this.moduleBuilder.DefineType(name, attr, parent);
            }
        }

        public TypeBuilder DefineType(string name, TypeAttributes attr, Type parent, Type[] interfaces)
        {
            lock (this.gate)
            {
                return this.moduleBuilder.DefineType(name, attr, parent, interfaces);
            }
        }

#if NETFRAMEWORK

        public AssemblyBuilder Save()
        {
            this.assemblyBuilder.Save(this.moduleName + ".dll");
            return this.assemblyBuilder;
        }

#endif
    }
}

#endif
