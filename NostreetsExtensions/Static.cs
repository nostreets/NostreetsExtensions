﻿using NostreetsExtensions.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Web.Configuration;

namespace NostreetsExtensions
{
    public static class Static
    {
        #region Static Methods

        /// <summary>
        /// Gets the local ip addresses.
        /// </summary>
        /// <returns></returns>
        public static IPAddress[] GetLocalIPAddresses()
        {
            string hostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(hostName);

            IPAddress[] addr = ipEntry.AddressList;

            return addr;
        }

        /// <summary>
        /// Gets the objects with attribute.
        /// </summary>
        /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="types">The types.</param>
        /// <param name="assembliesToSkip">The assemblies to skip.</param>
        /// <returns></returns>
        public static List<Tuple<TAttribute, object, Assembly>> GetObjectsWithAttribute<TAttribute>(Func<Assembly, bool> predicate, ClassTypes section) where TAttribute : Attribute
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            List<Tuple<TAttribute, object, Assembly>> result = new List<Tuple<TAttribute, object, Assembly>>();
            IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(predicate);

            using (AttributeScanner<TAttribute> scanner = new AttributeScanner<TAttribute>())
                foreach (Assembly ass in assemblies)
                    foreach (var item in scanner.ScanForAttributes(ass, section))
                        result.Add(new Tuple<TAttribute, object, Assembly>(item.Item1, item.Item2, item.Item4));

            return result.Distinct().ToList();
        }

        /// <summary>
        /// Gets the objects by attribute.
        /// </summary>
        /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="section">The section.</param>
        /// <param name="type">The type.</param>
        /// <param name="assembliesToSkip">The assemblies to skip.</param>
        /// <returns></returns>
        public static List<object> GetObjectsByAttribute<TAttribute>(Func<Assembly, bool> predicate, ClassTypes section) where TAttribute : Attribute
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            List<object> result = new List<object>();
            IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(predicate);

            using (AttributeScanner<TAttribute> scanner = new AttributeScanner<TAttribute>())
                foreach (Assembly ass in assemblies)
                    foreach (var item in scanner.ScanForAttributes(ass, section))
                        result.Add(item.Item2);

            return result.Distinct().ToList();
        }

        /// <summary>
        /// Gets the methods by attribute.
        /// </summary>
        /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="type">The type.</param>
        /// <param name="assembliesToSkip">The assemblies to skip.</param>
        /// <returns></returns>
        public static List<MethodInfo> GetMethodsByAttribute<TAttribute>(Func<Assembly, bool> predicate) where TAttribute : Attribute
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            List<MethodInfo> result = new List<MethodInfo>();
            IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(predicate);

            using (AttributeScanner<TAttribute> scanner = new AttributeScanner<TAttribute>())
                foreach (Assembly ass in assemblies)
                    foreach (var item in scanner.ScanForAttributes(ass, ClassTypes.Methods))
                        result.Add((MethodInfo)item.Item2);

            return result.Distinct().ToList();
        }

        /// <summary>
        /// Gets the types by attribute.
        /// </summary>
        /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="type">The type.</param>
        /// <param name="assembliesToSkip">The assemblies to skip.</param>
        /// <returns></returns>
        public static List<Type> GetTypesByAttribute<TAttribute>(Func<Assembly, bool> predicate) where TAttribute : Attribute
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            List<Type> result = new List<Type>();
            IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(predicate);

            using (AttributeScanner<TAttribute> scanner = new AttributeScanner<TAttribute>())
                foreach (Assembly ass in assemblies)
                    foreach (var item in scanner.ScanForAttributes(ass, ClassTypes.Type))
                        result.Add((Type)item.Item2);

            return result.Distinct().ToList();
        }

        /// <summary>
        /// Gets the properties by attribute.
        /// </summary>
        /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="type">The type.</param>
        /// <param name="assembliesToSkip">The assemblies to skip.</param>
        /// <returns></returns>
        public static List<PropertyInfo> GetPropertiesByAttribute<TAttribute>(Func<Assembly, bool> predicate) where TAttribute : Attribute
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            List<PropertyInfo> result = null;
            IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(predicate);

            using (AttributeScanner<TAttribute> scanner = new AttributeScanner<TAttribute>())
                foreach (Assembly ass in assemblies)
                    foreach (var item in scanner.ScanForAttributes(ass, ClassTypes.Properties))
                        result.Add((PropertyInfo)item.Item2);

            return result.Distinct().ToList();
        }

        public static void UpdateWebConfig(string key, string value)
        {
            // Get the configuration.
            Configuration config = WebConfigurationManager.OpenWebConfiguration("~");
            bool doesKeyExist = false;

            foreach (KeyValueConfigurationElement item in config.AppSettings.Settings)
                if (item.Key == key)
                    doesKeyExist = true;


            if (!doesKeyExist)
                config.AppSettings.Settings.Add(key, value);
            else
                config.AppSettings.Settings[key].Value = value;

            // Save to the file,
            config.Save(ConfigurationSaveMode.Minimal);
        }

        public static string SolutionPath()
        {
            string solutionDirPath = Assembly.GetCallingAssembly().CodeBase.StepOutOfDirectory(3);

            return solutionDirPath.ScanForFilePath(null, "sln");
        }

        public static Solution GetSolution()
        {
            return new Solution(SolutionPath());
        }

        public static List<Project> GetSolutionProjects()
        {
            return GetSolution().Projects;
        }

        public static T Wait<T>(int miliseconds, Func<T> func)
        {
            Thread.Sleep(miliseconds);
            return func();
        }

        public static void Wait(int miliseconds, Action func)
        {
            Thread.Sleep(miliseconds);
            func();
        }

        public static string GetOSDrive()
        {
            return Path.GetPathRoot(Environment.SystemDirectory);
        }

        public static string GetLocalPath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static void CreateFolder(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return;
        }

        public static IEnumerable<T> ParseStackTrace<T>(string text,
            Func<string, string, string, string, IEnumerable<KeyValuePair<string, string>>, string, string, T> func)
        {
           return StackTraceParser.Parse(text, func);
        }

        #endregion
    }
}
