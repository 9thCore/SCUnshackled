using System.Diagnostics;
using System.Reflection;
using System;
using System.Security.Policy;

namespace SCUnshackled
{
    public enum ConsoleLevel
    {
        Normal,
        Warning,
        Error
    }

    public class Utils
    {

        static public void Print(object str, ConsoleLevel level = ConsoleLevel.Normal)
        {
            StackTrace stackTrace = new StackTrace();

            MethodBase callingMethod = stackTrace.GetFrame(1).GetMethod();
            Type callingClass = callingMethod.DeclaringType;

            string prefix = callingClass.Name + "." + callingMethod.Name;

            string line = $"[{prefix}] " + str;

            switch (level)
            {
                case ConsoleLevel.Normal:
                    Base.logger.LogInfo(line);
                    break;
                case ConsoleLevel.Warning:
                    Base.logger.LogWarning(line);
                    break;
                case ConsoleLevel.Error:
                    Base.logger.LogError(line);
                    break;
            }
        }

        static public FieldInfo GetField(object o, string name)
        {
            FieldInfo info = o.GetType().GetField(name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return info;
        }

        static public PropertyInfo GetProperty(object o, string name)
        {
            PropertyInfo info = o.GetType().GetProperty(name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return info;
        }

        static public MethodInfo GetMethod(object o, string name)
        {
            MethodInfo info = o.GetType().GetMethod(name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return info;
        }

    }

}