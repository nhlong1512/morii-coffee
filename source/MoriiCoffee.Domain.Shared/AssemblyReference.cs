using System.Reflection;

namespace MoriiCoffee.Domain.Shared;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
