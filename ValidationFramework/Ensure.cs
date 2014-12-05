using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ValidationFramework
{
    public static class Ensure
    {

//        private static ILogger GetLogger()
//        {
//            return LogHost.LogManager.Logger(typeof(Ensure));
//        }

        #region Null
        [Conditional("DEBUG")]
        public static void Null<T>(T value)
            where T : class
        {
            if (value == null) return;
            throw new ArgumentNullException("value", @"Expected null");
        }

        [Conditional("DEBUG")]
        public static void Null<T>(T value, string message, params object[] args)
            where T : class
        {
            if (value == null) return;
            if (args.Length < 1)
            {
//                GetLogger().Error("Expected null: {0}", message);
                throw new ArgumentNullException(message);
            }

//            GetLogger().Error(message, args);
            throw new ArgumentNullException(string.Format(message, args));
        }

        #endregion

        #region NotNull

        [Conditional("DEBUG")]
        public static void NotNull<T>(T value, [CallerMemberName] string callerName = null)
            where T : class
        {
            if (value != null) return;
            throw new ArgumentNullException("value", string.Format(@"Failed Null Check [From: {0}]", callerName));
        }

        [Conditional("DEBUG")]
        public static void NotNull<T>(T value, string message, params object[] args)
            where T : class
        {
            if (value != null) return;


            if (args.Length < 1)
            {
//                GetLogger().Error("Failed Null Check: {0}", message);
                throw new ArgumentNullException(message);
            }

//            GetLogger().Error(message, args);
            throw new ArgumentNullException(string.Format(message, args));
        }

        #endregion

        #region NotNullOrEmpty

        [Conditional("DEBUG")]
        public static void NotNullOrEmpty(string value, [CallerMemberName] string callerName = null)
        {
            if (!string.IsNullOrEmpty(value)) return;

            throw new ArgumentNullException("value", string.Format(@"Failed Null or Empty Check [From: {0}]", callerName));
        }

        [Conditional("DEBUG")]
        public static void NotNullOrEmpty(string value, string message, params object[] args)
        {
            if (!string.IsNullOrEmpty(value)) return;

            if (args.Length < 1)
            {
                args = new object[] { message };
                message = "Failed Null Or Empty Check: {0}";
            }

//            GetLogger().Error(message, args);
            throw new ArgumentException(string.Format(message, args));
        }

        #endregion

        #region Argument

        [Conditional("DEBUG")]
        public static void Argument(bool expression)
        {
            if (expression) return;

            throw new ArgumentException("Failed Argument Check");
        }

        [Conditional("DEBUG")]
        public static void Argument(bool expression, string message, params object[] args)
        {
            if (expression) return;

            if (args.Length < 1)
            {
                args = new object[] { message };
                message = "Failed Argument Check: {0}";
            }

//            GetLogger().Error(message, args);
            throw new ArgumentException(string.Format(message, args));
        }

        #endregion
    }
}
