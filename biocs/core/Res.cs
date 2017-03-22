using System.Globalization;
using System.Resources;

namespace Biocs
{
    internal static class Res
    {
        private static readonly ResourceManager rm = new ResourceManager(typeof(Res));

        public static string GetString(string name) => rm.GetString(name);

        public static string GetString(string name, object arg0) => GetStringInternal(name, arg0);

        public static string GetString(string name, object arg0, object arg1) => GetStringInternal(name, arg0, arg1);

        public static string GetString(string name, object arg0, object arg1, object arg2) 
            => GetStringInternal(name, arg0, arg1, arg2);

        private static string GetStringInternal(string name, params object[] args)
            => string.Format(CultureInfo.CurrentCulture, GetString(name), args);
    }
}