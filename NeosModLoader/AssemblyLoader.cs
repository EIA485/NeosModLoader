﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace NeosModLoader
{
    internal static class AssemblyLoader
    {
        private static string[]? GetAssemblyPathsFromDir(string dirName)
        {
            string assembliesDirectory = Path.Combine(Directory.GetCurrentDirectory(), dirName);

            Logger.MsgInternal($"loading assemblies from {dirName}");

            string[]? assembliesToLoad = null;
            try
            {
                assembliesToLoad = Directory.GetFiles(assembliesDirectory, "*.dll", SearchOption.AllDirectories);
                Array.Sort(assembliesToLoad, (a, b) => string.CompareOrdinal(a, b));
            }
            catch (Exception e)
            {
                if (e is DirectoryNotFoundException)
                {
                    Logger.MsgInternal($"{dirName} directory not found, creating it now.");
                    try
                    {
                        Directory.CreateDirectory(assembliesDirectory);
                    }
                    catch (Exception e2)
                    {
                        Logger.ErrorInternal($"Error creating ${dirName} directory:\n{e2}");
                    }
                }
                else
                {
                    Logger.ErrorInternal($"Error enumerating ${dirName} directory:\n{e}");
                }
            }
            return assembliesToLoad;
        }

        private static Assembly? LoadAssembly(string filepath)
        {
            string filename = Path.GetFileName(filepath);
            SplashChanger.SetCustom($"Loading file: {filename}");
            Assembly assembly;
            try
            {
                Logger.DebugFuncInternal(() => $"load assembly {filename}");
                assembly = Assembly.LoadFile(filepath);
            }
            catch (Exception e)
            {
                Logger.ErrorInternal($"error loading assembly from {filepath}: {e}");
                return null;
            }
            if (assembly == null)
            {
                Logger.ErrorInternal($"unexpected null loading assembly from {filepath}");
                return null;
            }
            return assembly;
        }

        internal static AssemblyFileWithHash[] LoadAssembliesFromDir(string dirName)
        {
            List<AssemblyFileWithHash> assemblyFiles = new();
            if (GetAssemblyPathsFromDir(dirName) is string[] assemblyPaths)
            {
                foreach (string assemblyFilepath in assemblyPaths)
                {
                    string sha256 = string.Empty;
                    try
                    {
                        sha256 = Util.GenerateSHA256(assemblyFilepath);
                        if (LoadAssembly(assemblyFilepath) is Assembly assembly)
                        {
                            assemblyFiles.Add(new AssemblyFileWithHash(sha256, new AssemblyFile(assemblyFilepath, assembly)));
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorInternal($"Unexpected exception loading assembly from {assemblyFilepath}{(sha256 == string.Empty ? "" : $"({sha256})")}:\n{e}");
                    }
                }
            }
            return assemblyFiles.ToArray();
        }
    }
    internal struct AssemblyFileWithHash
    {
        public string sha256;
        public AssemblyFile asm;
        public AssemblyFileWithHash(string sha256, AssemblyFile asm)
        {
            this.sha256 = sha256;
            this.asm = asm;
        }
    }
}
