using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Biocs.TestTools
{
    /// <summary>
    /// Provides some utilitary functions for unit tests.
    /// </summary>
    public static class BiocsAssert
    {
        /// <summary>
        /// Verifies that the expected exception is thrown.
        /// </summary>
        /// <typeparam name="TException">
        /// An expected type of exception to be thrown by a method. The derived types are allowed.
        /// </typeparam>
        /// <param name="action">The test method.</param>
        /// <exception cref="AssertFailedException">The test method did not throw the expected exception.</exception>
        public static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            Assert.Fail("The test method did not throw the expected exception: " + typeof(TException) + ".");
        }

        /// <summary>
        /// Verifies that the expected exception is thrown.
        /// </summary>
        /// <typeparam name="TException">
        /// An expected type of exception to be thrown by a method. The derived types are allowed.
        /// </typeparam>
        /// <param name="func">The test method.</param>
        /// <exception cref="AssertFailedException">The test method did not throw the expected exception.</exception>
        public static void Throws<TException>(Func<object> func) where TException : Exception
        {
            try
            {
                func();
            }
            catch (TException)
            {
                return;
            }
            Assert.Fail("The test method did not throw the expected exception: " + typeof(TException) + ".");
        }

#if NET45
        /// <summary>
        /// Creates directories for unit tests of specified type.
        /// </summary>
        /// <param name="testContext"><see cref="TestContext"/>.</param>
        /// <param name="type">The type to test.</param>
        /// <returns>The path of created directory.</returns>
        /// <remarks>Creates directories using the fully qualified name of the type.</remarks>
        public static string CreateDirectory(TestContext testContext, Type type)
        {
            if (type.IsGenericType)
                type = type.GetGenericTypeDefinition();

            string path = Path.Combine(testContext.DeploymentDirectory, type.Namespace, type.Name);
            Directory.CreateDirectory(path);
            return path;
        }
#endif
    }
}
