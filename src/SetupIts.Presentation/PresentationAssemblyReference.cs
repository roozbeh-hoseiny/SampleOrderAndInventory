using System.Reflection;

namespace SetupIts.Presentation;

public static class PresentationAssemblyReference
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();
}