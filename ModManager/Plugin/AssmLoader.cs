using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;

namespace ModManager.Plugin
{
    #region AssmInfo
    public class AssmInfo
    {
        #region Variables
        private string m_assemblyPath;
        private Type m_type;
        private byte[] m_publicKey;
        private object m_instance;
        #endregion

        #region Constructor
        public AssmInfo(string assemblyPath, Type type, byte[] publicKey)
        {
            this.m_assemblyPath = assemblyPath;
            this.m_type = type;
            this.m_publicKey = publicKey;
        }
        #endregion

        #region Properties
        public string AssemblyPath
        {
            get => this.m_assemblyPath;
        }

        public Assembly Assembly
        {
            get => this.m_type.Assembly;
        }

        public string Name
        {
            get => this.m_type.Assembly.FullName;
        }

        public Type Type
        {
            get => this.m_type;
        }

        public byte[] PublicKey
        {
            get => this.m_publicKey;
        }
        #endregion

        #region Methods
        public object CreateInstance()
        {
            if (this.m_instance == null)
            {
                try
                {
                    Assembly asm = m_type.Assembly;
                    this.m_instance = asm.CreateInstance(m_type.FullName);
                }
                catch
                { }
            }

            return this.m_instance;
        }
        #endregion
    }
    #endregion

    #region AssmLoader
    internal static class AssmLoader
    {
        public static AssmInfo LoadAssembly(string assemblyPath, Type wantedType)
        {
            Type foundType = null;

            try
            {
                if (!File.Exists(assemblyPath)) return null;

                Assembly asm = Assembly.LoadFrom(assemblyPath);

                Type[] assemblyTypes = asm.GetTypes();

                foreach (Type type in assemblyTypes)
                {
                    if (wantedType.IsAssignableFrom(type) && type.IsClass)
                    {
                        foundType = type;
                        break;
                    }
                }

                if (foundType != null)
                {
                    AssemblyName assemblyName = asm.GetName();
                    byte[] publicKey = assemblyName.GetPublicKey();
                    return new AssmInfo(assemblyPath, foundType, publicKey);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return null;
        }

        public static AssmInfo LoadAssembly(string assemblyPath, string typeName)
        {
            if (!File.Exists(assemblyPath)) return null;
            if (string.IsNullOrEmpty(typeName)) return null;

            Type foundType = null;

            try
            {
                Assembly asm = Assembly.LoadFrom(assemblyPath);

                Type[] assemblyTypes = asm.GetTypes();
                foreach (Type type in assemblyTypes)
                {
                    System.Diagnostics.Debug.WriteLine($"Type {type.FullName}");
                    if (type.FullName == typeName)
                    {
                        foundType = type;
                        break;
                    }
                }

                if (foundType != null)
                {
                    AssemblyName assemblyName = asm.GetName();
                    byte[] publicKey = assemblyName.GetPublicKey();
                    return new AssmInfo(assemblyPath, foundType, publicKey);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return null;
        }

        public static AssmInfo[] LoadDirectory(string path, string searchPattern, Type wantedType)
        {
            List<AssmInfo> assmInfos = new List<AssmInfo>();

            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path, searchPattern);
                if (files.Length == 0) return assmInfos.ToArray();

                foreach (string file in files)
                {
                    string assemblyPath = Path.Combine(path, file);
                    var assm = LoadAssembly(assemblyPath, wantedType);
                    if (assm != null)
                    {
                        assmInfos.Add(assm);
                    }
                }
            }

            return assmInfos.ToArray();
        }

        public static void LoadWithEntryPoint(string assemblyPath, string entryPoint)
        {
            try
            {
                if (!File.Exists(assemblyPath)) return;
                if (string.IsNullOrEmpty(entryPoint)) return;

                string typeName = string.Empty;
                string method = string.Empty;

                var entryPoints = entryPoint.Split('.');
                if (entryPoints.Length > 1)
                {
                    method = entryPoints.Last();
                    typeName = string.Join('.', entryPoints.SkipLast(1).ToArray());
                }
                if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(method)) return;

                Assembly asm = Assembly.LoadFrom(assemblyPath);

                AssemblyName assemblyName = asm.GetName();
                byte[] publicKey = assemblyName.GetPublicKey();

                Type[] assemblyTypes = asm.GetTypes();
                Type foundType = null;
                foreach (Type type in assemblyTypes)
                {
                    if (type.FullName == typeName)
                    {
                        foundType = type;
                        break;
                    }
                }

                if (foundType != null)
                {
                    var methods = foundType.FindMembers(MemberTypes.Method, BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly, Type.FilterName, method);
                    if (methods != null && methods.Length > 0)
                    {
                        foreach (var m in methods)
                        {
                            var paramsInfo = (m as MethodBase).GetParameters();
                            if (paramsInfo.Length == 0)
                            {
                                // Found
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }
    #endregion
}
