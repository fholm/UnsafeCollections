namespace UnsafeCollections
{
    internal static class UDebug
    {
        [System.Diagnostics.Conditional("DEBUG")]
        [System.Diagnostics.DebuggerStepThrough]
        public static void Assert(bool condition)
        {
            if (!condition)
                throw new AssertException();
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [System.Diagnostics.DebuggerStepThrough]
        public static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new AssertException(message);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [System.Diagnostics.DebuggerStepThrough]
        public static void Assert(bool condition, string format, params object[] args)
        {
            Assert(condition, string.Format(format, args));
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [System.Diagnostics.DebuggerStepThrough]
        public static void Assert(bool condition, string message, string format, params object[] args)
        {
            Assert(condition, message + " : " + string.Format(format, args));
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [System.Diagnostics.DebuggerStepThrough]
        public static void Fail(string message)
        {
            throw new AssertException(message);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [System.Diagnostics.DebuggerStepThrough]
        public static void Fail(string format, params object[] args)
        {
            throw new AssertException(string.Format(format, args));
        }

        public static void Always(bool condition)
        {
            if (!condition)
                throw new AssertException();
        }

        public static void Always(bool condition, string error)
        {
            if (!condition)
                throw new AssertException(error);
        }

        public static void Always(bool condition, string format, params object[] args)
        {
            if (!condition)
                throw new AssertException(string.Format(format, args));
        }
    }
}
