using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Biocs.TestTools;

/// <summary>
/// Provides some utilitary functions for unit tests of <see cref="string"/> resources.
/// </summary>
public static class StringResourceTester
{
    /// <summary>
    /// Tests whether string resources is expectedly used using <see cref="StringResourceUsageAttribute"/> data.
    /// </summary>
    /// <param name="resourceClass">The resource manager class of an assembly.</param>
    [Conditional("DEBUG")]
    public static void CheckStringResource(Type resourceClass)
    {
        var rm = new ResourceManager(resourceClass);
        var getStringMethods = FindGetStringMethods(resourceClass);
        var usedNames = new HashSet<string>(StringComparer.Ordinal);

        // Enumerates each method member.
        foreach (var module in resourceClass.Assembly.GetModules())
        {
            foreach (var type in module.GetTypes())
            {
                if (type == resourceClass || IsCompilerGenerated(type))
                    continue;

                foreach (var member in type.GetMembers(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    var method = member as MethodBase;

                    if (method == null || IsCompilerGenerated(method))
                        continue;

                    // Check method bodies and gets the StringResourceUsageAttribute collection.
                    var usages = CheckResourceUsage(module, method, getStringMethods);

                    if (usages.Count > 0)
                    {
                        CheckResource(rm, usages.Values);
                        usedNames.UnionWith(usages.Keys);
                    }
                }
            }
        }

        // Are there unused resources?
        var declaredNames = GetResourceNames(resourceClass);
        bool setEquals = usedNames.SetEquals(declaredNames);
        declaredNames.ExceptWith(usedNames);
        Assert.IsTrue(setEquals, string.Join(", ", declaredNames) + " is not used anywhere.");
    }

    /// <summary>
    /// Reconciles the IL instruction and the data of <see cref="StringResourceUsageAttribute"/>.
    /// </summary>
    private static Dictionary<string, StringResourceUsageAttribute> CheckResourceUsage(
        Module module, MethodBase method, Dictionary<int, int> getStringMethods)
    {
        var declaredUsages = GetUsageAttributes(method);
        var actualUsages = new HashSet<string>(StringComparer.Ordinal);
        var loadedName = new HashSet<string>(StringComparer.Ordinal);

        foreach (var inst in Instruction.ReadIL(method))
        {
            if (inst.IsLoadString)
            {
                string str = module.ResolveString(inst.Operand);

                if (declaredUsages.ContainsKey(str))
                    loadedName.Add(str);
            }
            else if (inst.IsCallMethod && getStringMethods.ContainsKey(inst.Operand))
            {
                Assert.AreNotEqual(0, loadedName.Count, "The name of the string resource is not found in " + 
                    method.DeclaringType.FullName + "." + method.Name + ".");

                foreach (string name in loadedName)
                {
                    if (declaredUsages[name].ResourceCheckOnly)
                    {
                        Console.WriteLine("The usage of '" + name + "' in " + method.DeclaringType.FullName + "." + 
                            method.Name + " is not checked.");
                        continue;
                    }
                    Assert.AreEqual(declaredUsages[name].FormatItemCount, getStringMethods[inst.Operand],
                        "The usage of '" + name + "' in " + method.DeclaringType.FullName + "." + method.Name + " is wrong.");
                }
                actualUsages.UnionWith(loadedName);
                loadedName.Clear();
            }
        }

        if (actualUsages.Count != declaredUsages.Count)
        {
            int escaped = 0;

            foreach (var usage in declaredUsages)
            {
                if (usage.Value.ResourceCheckOnly)
                {
                    escaped++;
                    Console.WriteLine("The usage of '" + usage.Key + "' in " + method.DeclaringType.FullName + "." + 
                        method.Name + " is not checked.");
                }
            }
            Assert.AreEqual(declaredUsages.Count, actualUsages.Count + escaped,
                "The usage information in " + method.DeclaringType.FullName + "." + method.Name + " is inconsistent.");
        }
        return declaredUsages;
    }

    /// <summary>
    /// Reconciles the resource and the data of <see cref="StringResourceUsageAttribute"/>s.
    /// </summary>
    private static void CheckResource(ResourceManager resources, ICollection<StringResourceUsageAttribute> usages)
    {
        foreach (var usage in usages)
        {
            // Does the manifest resource define the name of the string resource?
            string str = resources.GetString(usage.Name);
            Assert.IsNotNull(str, "The resource '" + usage.Name + "' is not defined.");

            // Does the format string have the valid number of format items?
            var args = new object[usage.FormatItemCount];
            try
            {
                _ = string.Format(CultureInfo.InvariantCulture, str, args);
            }
            catch (FormatException)
            {
                // #{format item} > #{arg}
                Assert.Fail("The number of format items in resource '" + 
                    usage.Name + "' is more than " + usage.FormatItemCount + ".");
            }

            for (int i = 0; i < usage.FormatItemCount; i++)
            {
                if (!str.Contains("{" + i))
                {
                    // #{format item} < #{arg}
                    Console.WriteLine("The resource '" + usage.Name + 
                        "' doesn't contains the format item whose index is " + i + ".");
                }
            }
        }
    }

    /// <summary>
    /// Finds 'GetString' methods of the resource manager class and gets the metadata token and the number of format items.
    /// </summary>
    /// <remarks>Finds 'public static string GetString(string, ...);'.</remarks>
    private static Dictionary<int, int> FindGetStringMethods(Type resourceClass)
    {
        var methods = resourceClass.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
        var map = new Dictionary<int, int>();

        foreach (var method in methods)
        {
            var paramInfos = method.GetParameters();
            map[method.MetadataToken] = paramInfos.Length - 1;
        }
        return map;
    }

    /// <summary>
    /// Reads string resource names from the default resource.
    /// </summary>
    private static HashSet<string> GetResourceNames(Type resourceClass)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        using (var stream =
            resourceClass.Assembly.GetManifestResourceStream(resourceClass, resourceClass.Name + ".resources"))
        using (var reader = new ResourceReader(stream))
        {
            var enumerator = reader.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (enumerator.Value is string)
                    names.Add(enumerator.Key.ToString());
            }
        }
        return names;
    }

    private static Dictionary<string, StringResourceUsageAttribute> GetUsageAttributes(MemberInfo member)
    {
        var attrs = member.GetCustomAttributes(typeof(StringResourceUsageAttribute), false);
        var map = new Dictionary<string, StringResourceUsageAttribute>(attrs.Length, StringComparer.Ordinal);

        for (int i = 0; i < attrs.Length; i++)
        {
            var attr = attrs[i] as StringResourceUsageAttribute;
            map[attr.Name] = attr;
        }
        return map;
    }

    private static bool IsCompilerGenerated(MemberInfo member) => member.IsDefined(typeof(CompilerGeneratedAttribute), false);
}
