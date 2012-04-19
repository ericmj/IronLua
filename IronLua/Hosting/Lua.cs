using System;
using System.Collections.Generic;
using IronLua.Runtime;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;
using Microsoft.Scripting.Utils;

namespace IronLua.Hosting
{
    /// <summary>
    /// Provides helpers for interacting with IronLua.
    /// </summary>
    public static class Lua
    {
        /// <summary>
        /// Creates a LanguageSetup object which includes the Lua script engine with the specified options.
        /// The LanguageSetup object can be used with other LanguageSetup objects from other languages to  configure a ScriptRuntimeSetup object.
        /// </summary>
        public static LanguageSetup CreateLanguageSetup(IDictionary<string, object> options = null)
        {
            var languageSetup = new LanguageSetup(
                typeof(LuaContext).AssemblyQualifiedName,
                "IronLua",
                "IronLua;Lua;lua".Split(';'),
                ".lua".Split(';')
            );

            if (options != null)
            {
                foreach (KeyValuePair<string, object> keyValuePair in options)
                    languageSetup.Options.Add(keyValuePair.Key, keyValuePair.Value);
            }
            return languageSetup;
        }

        /// <summary>
        /// Creates a ScriptRuntimeSetup object which includes the Lua script engine with the specified options.
        /// The ScriptRuntimeSetup object can then be additionally configured and used to create a ScriptRuntime.
        /// </summary>
        public static ScriptRuntimeSetup CreateRuntimeSetup(IDictionary<string, object> options = null)
        {
            var scriptRuntimeSetup = new ScriptRuntimeSetup();
            scriptRuntimeSetup.LanguageSetups.Add(Lua.CreateLanguageSetup(options));
            if (options != null)
            {
                object obj;
                
                if (options.TryGetValue("Debug", out obj) && obj is bool && (bool)obj)
                    scriptRuntimeSetup.DebugMode = true;

                if (options.TryGetValue("PrivateBinding", out obj) && obj is bool && (bool)obj)
                    scriptRuntimeSetup.PrivateBinding = true;
            }
            return scriptRuntimeSetup;
        }

        /// <summary>
        /// Creates a new ScriptRuntime with the IronLua scipting engine pre-configured.
        /// </summary>
        public static ScriptRuntime CreateRuntime(IDictionary<string, object> options = null)
        {
            return new ScriptRuntime(Lua.CreateRuntimeSetup(options));
        }

        /// <summary>
        /// Creates a new ScriptRuntime with the IronLua scripting engine pre-configured
        /// in the specified AppDomain with additional options. The remote ScriptRuntime may
        /// be manipulated from the local domain but all code will run in the remote domain.
        /// </summary>
        public static ScriptRuntime CreateRuntime(AppDomain domain, IDictionary<string, object> options = null)
        {
            ContractUtils.RequiresNotNull(domain, "domain");
            return ScriptRuntime.CreateRemote(domain, Lua.CreateRuntimeSetup(options));
        }

        /// <summary>
        /// Creates a new ScriptRuntime and returns the ScriptEngine for IronLua. 
        /// If the ScriptRuntime is required it can be acquired from the Runtime property on the engine.
        /// </summary>
        public static ScriptEngine CreateEngine(IDictionary<string, object> options = null)
        {
            return Lua.GetEngine(Lua.CreateRuntime(options));
        }

        /// <summary>
        /// Creates a new ScriptRuntime with the specified options and returns the ScriptEngine for IronLua. 
        /// If the ScriptRuntime is required it can be acquired from the Runtime property on the engine.
        /// 
        /// The remote ScriptRuntime may be manipulated from the local domain but all code will run in the remote domain.
        /// </summary>
        public static ScriptEngine CreateEngine(AppDomain domain, IDictionary<string, object> options = null)
        {
            return Lua.GetEngine(Lua.CreateRuntime(domain, options));
        }

        /// <summary>
        /// Given a ScriptRuntime gets the ScriptEngine for IronLua.
        /// </summary>
        public static ScriptEngine GetEngine(ScriptRuntime runtime)
        {
            return runtime.GetEngineByTypeName(typeof(LuaContext).AssemblyQualifiedName);
        }

        internal static LuaContext GetLuaContext(ScriptEngine engine)
        {
            return HostingHelpers.GetLanguageContext(engine) as LuaContext;
        }
    }
}
