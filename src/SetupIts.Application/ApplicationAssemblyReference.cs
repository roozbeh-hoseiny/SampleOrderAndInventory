using System.Reflection;

namespace SetupIts.Application;

public static class ApplicationAssemblyReference
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();
}