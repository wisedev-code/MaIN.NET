// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MaIN.Examples.Examples;

var examples = new Dictionary<string, IExample>
{
    ["Intro"] = new IntroductionExample(),
    ["Local LLM"] = new LocalModelExample(),
    ["Local Skills"] = new LocalModelSkillsExample()
};

Console.WriteLine("Welcome to the MaIN.NET Examples!");
Console.WriteLine("Please select an example to run:");

var exampleNames = examples.Keys.ToList();
for (int i = 0; i < exampleNames.Count; i++)
{
    Console.WriteLine($"{i + 1}. {exampleNames[i]}");
}

var selectedExampleName = "Local Skills"; // Default to the new example
var example = examples[selectedExampleName];

// Optional: allow user to select an example
// string? input = Console.ReadLine();
// if (int.TryParse(input, out int choice) && choice > 0 && choice <= examples.Count)
// {
//     example = examples.ElementAt(choice - 1).Value;
//     selectedExampleName = examples.ElementAt(choice - 1).Key;
// }

Console.WriteLine($"\n--- Running Example: {selectedExampleName} ---\n");
await example.RunAsync();
Console.WriteLine($"\n--- Finished Example: {selectedExampleName} ---\n");