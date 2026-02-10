using System.Reflection;

namespace SetupIts.Infrastructure;

public static class InfrastructureAssemblyReference
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();
}