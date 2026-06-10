
using System.Reflection;

// Get all examples from the current assembly
var examples = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => t.IsClass && t.Namespace == "Examples" && t.Name.EndsWith("Example"))
    .Select(t => new
    {
        Name = t.Name.Replace("Example", ""),
        Type = t
    })
    .ToArray();

// Write the examples to the console
for (var i = 0; i < examples.Length; i++)
{
    Console.WriteLine($"{i + 1}. {examples[i].Name}");
}

// Get the user's choice
Console.Write("Enter the number of the example to run: ");
var choice = Console.ReadLine();

// Run the selected example
if (int.TryParse(choice, out var index) && index > 0 && index <= examples.Length)
{
    var example = examples[index - 1];
    var method = example.Type.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
    method?.Invoke(null, new object[] { Array.Empty<string>() });
}
else
{
    Console.WriteLine("Invalid choice.");
}

// Add a new namespace for the local skill example
namespace Examples
{
    internal class AgentWithLocalSkillExample
    {
        internal static async Task Main(string[] args)
        {
            await MaIN.Core.Program.Main(args);
        }
    }
}
