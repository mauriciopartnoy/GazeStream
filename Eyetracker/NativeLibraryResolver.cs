using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GazeStream.Eyetracker
{
    /// <summary>
    /// .NET admite un solo DllImportResolver por ensamblado (un segundo SetDllImportResolver lanza excepción).
    /// Los SDK que necesitan elegir de dónde se carga su DLL nativa (Skyle, Joaco) se registran acá.
    /// Si el resolver devuelve IntPtr.Zero, .NET sigue con la búsqueda normal.
    /// </summary>
    internal static class NativeLibraryResolver
    {
        static readonly object gate = new();
        static readonly Dictionary<string, Func<IntPtr>> resolvers = new(StringComparer.OrdinalIgnoreCase);
        static bool installed;

        /// <param name="libraryName">El nombre tal como aparece en el [DllImport], por ejemplo "aSeeX.dll".</param>
        public static void Register(string libraryName, Func<IntPtr> resolve)
        {
            lock (gate)
            {
                resolvers[libraryName] = resolve;
                if (!installed)
                {
                    NativeLibrary.SetDllImportResolver(typeof(NativeLibraryResolver).Assembly, Resolve);
                    installed = true;
                }
            }
        }

        static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            Func<IntPtr>? resolve;
            lock (gate)
            {
                resolvers.TryGetValue(libraryName, out resolve);
            }
            return resolve?.Invoke() ?? IntPtr.Zero;
        }
    }
}
