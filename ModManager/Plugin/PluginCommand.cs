using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace ModManager.Plugin
{
    public class PluginCommand : ICommand
    {
        #region Variables
        private readonly WeakEventManager weakEventManager = new WeakEventManager();
        private PluginWrapper _plugin = null;
        private readonly string[] _arguments;
        #endregion

        #region Constructors
        public PluginCommand(string path, string title, string[] args)
        {
            this.Name = path;
            this.Title = title;
            this._arguments = args;

            if (!string.IsNullOrEmpty(path))
            {
                this._plugin = new PluginWrapper(path);
            }

            this.OnPluginLoaded();
        }

        public PluginCommand(string name, string title, string[] args, AssmInfo assm, PluginMethodInfo methodInfo)
        {
            this.Name = name;
            this.Title = title;
            this._arguments = args;

            try
            {
                if (methodInfo != null)
                {
                    if (methodInfo.IsStatic)
                    {
                        this._plugin = new PluginWrapper(assm.Type, methodInfo.Name);
                    }
                    else
                    {
                        var inst = assm.CreateInstance();
                        if (inst != null)
                        {
                            this._plugin = new PluginWrapper(inst, methodInfo.Name);
                        }
                    }
                }
                else
                {
                    var inst = assm.CreateInstance();
                    if (inst != null)
                    {
                        this._plugin = new PluginWrapper(inst);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            this.OnPluginLoaded();
        }

        public PluginCommand(AssmInfo assm, string[] args)
        {
            this._arguments = args;
            var name = assm.Name;

            try
            {
                var inst = assm.CreateInstance();
                if (inst != null)
                {
                    this._plugin = new PluginWrapper(inst);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            this.OnPluginLoaded();
        }
        #endregion

        #region Events
        /// <summary>
        /// Event handler raised when CanExecute changes.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { weakEventManager.AddEventHandler(value); }
            remove { weakEventManager.RemoveEventHandler(value); }
        }
        #endregion

        #region Properties
        public string Name { get; private set; }

        public string Title { get; private set; }
        #endregion

        #region Methods
        private void OnPluginLoaded()
        {
            try
            {
                if (this._plugin != null)
                {
                    this._plugin.OnPluginLoaded();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Invoke the CanExecute method to determine if it can be executed.
        /// </summary>
        /// <param name="parameter">Parameter to test and pass to CanExecute.</param>
        /// <returns>If it can be executed.</returns>
        public bool CanExecute(object parameter)
        {
            if (this._plugin != null)
            {
                if (parameter is IPluginParameter pp)
                {
                    var args = pp.ParsePluginArguments(this, this._arguments);
                    return this._plugin.CanExecute(args);
                }

                return this._plugin.CanExecute(parameter);
            }
            return false;
        }

        /// <summary>
        /// Execute the command with or without a parameter.
        /// </summary>
        /// <param name="parameter">Parameter to pass to execute method.</param>
        public void Execute(object parameter)
        {
            try
            {
                if (this._plugin != null)
                {
                    if (parameter is IPluginParameter pp)
                    {
                        var args = pp.ParsePluginArguments(this, this._arguments);
                        this._plugin.Execute(args);
                    }
                    else
                    {
                        this._plugin.Execute(parameter);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Manually raise a CanExecuteChanged event.
        /// </summary>
        public void RaiseCanExecuteChanged() => weakEventManager.HandleEvent(this, EventArgs.Empty, nameof(CanExecuteChanged));
        #endregion

        #region NestedTypes
        private class PluginWrapper : IPlugin
        {
            #region Variables
            private Func<object, bool> _canExecute = null;
            private Action _pluginLoaded = null;
            private Action<object> _execute = null;
            #endregion

            #region Constructors
            public PluginWrapper(object inst)
            {
                if (inst is IPlugin)
                {
                    var plugin = (IPlugin)inst;

                    this.Name = plugin.Name;
                    this._pluginLoaded = () => plugin.OnPluginLoaded();
                    this._execute = o => plugin.Execute(o);
                }
                else
                {
                    var instType = inst.GetType();
                    this.Name = instType.Name;

                    MethodInfo executeMethod = FindMethod(instType, "Execute", false, 1, typeof(void));
                    if (executeMethod != null)
                    {
                        this._execute = o => executeMethod.Invoke(inst, [o]);
                    }

                    MethodInfo canExecuteMethod = FindMethod(instType, "CanExecute", false, 1, typeof(bool));
                    if (canExecuteMethod != null)
                    {
                        this._canExecute = o => (bool)canExecuteMethod.Invoke(inst, [o]);
                    }

                    MethodInfo loadedMethod = FindMethod(instType, "OnPluginLoaded", false, 0, typeof(void));
                    if (loadedMethod != null)
                    {
                        this._pluginLoaded = () => loadedMethod.Invoke(inst, null);
                    }
                }
            }

            public PluginWrapper(Type type, string methodName)
            {
                this.Name = type.Name;

                MethodInfo methodInfo = FindMethod(type, methodName, true, 1, typeof(void));
                if (methodInfo != null)
                {

                    this._execute = o => methodInfo.Invoke(null, [o]);
                }
                else
                {
                    methodInfo = FindMethod(type, methodName, true, 0, typeof(void));
                    if (methodInfo != null)
                    {
                        this._execute = o => methodInfo.Invoke(null, null);
                    }
                }
            }

            public PluginWrapper(object inst, string methodName)
            {
                var instType = inst.GetType();
                this.Name = instType.Name;

                MethodInfo methodInfo = FindMethod(instType, methodName, true, 1, typeof(void));
                if (methodInfo != null)
                {
                    this._execute = o => methodInfo.Invoke(null, [o]);
                }
                else
                {
                    methodInfo = FindMethod(instType, methodName, true, 0, typeof(void));
                    if (methodInfo != null)
                    {
                        this._execute = o => methodInfo.Invoke(null, null);
                    }
                }
            }

            public PluginWrapper(string filePath)
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    this.Name = fileInfo.Name;

                    if (fileInfo.Exists && fileInfo.Extension == ".exe")
                    {
                        this._execute = o =>
                        {
                            if (o is string args)
                            {
                                System.Diagnostics.Process.Start(fileInfo.FullName, args);
                            }
                            else
                            {
                                System.Diagnostics.Process.Start(fileInfo.FullName);
                            }
                        };
                    }
                }
            }
            #endregion

            #region Properties
            public string Name { get; private set; } = string.Empty;
            #endregion

            #region Methods
            public void OnPluginLoaded()
            {
                if (this._pluginLoaded != null)
                {
                    this._pluginLoaded();
                }
            }

            public bool CanExecute(object parameter)
            {
                Func<object, bool> fn = this._canExecute ?? (o => this._execute != null);
                return fn(parameter);
            }

            public void Execute(object parameter)
            {
                if (this._execute != null)
                {
                    this._execute(parameter);
                }
            }

            private static MethodInfo FindMethod(Type type, string methodName, bool isStatic, int argsCount, Type returnType)
            {
                var bindingFlags = isStatic ? BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly : BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance;
                var methods = type.FindMembers(MemberTypes.Method, bindingFlags, Type.FilterName, methodName);
                if (methods != null && methods.Length > 0)
                {
                    var infos = methods.Cast<MethodInfo>().ToArray();

                    MethodInfo methodInfo = null;
                    foreach (var m in infos)
                    {
                        var paramsInfo = m.GetParameters();
                        if (paramsInfo.Length == argsCount && m.ReturnType == returnType)
                        {
                            methodInfo = m;
                            break;
                        }
                    }

                    return methodInfo;
                }

                return null;
            }
            #endregion
        }
        #endregion
    }
}
