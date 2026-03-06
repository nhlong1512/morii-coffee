using System.Reflection;

namespace MoriiCoffee.Infrastructure.Persistence;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
