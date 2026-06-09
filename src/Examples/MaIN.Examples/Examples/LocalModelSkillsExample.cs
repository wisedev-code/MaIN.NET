// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Core.Models;
using MaIN.Examples.Skills;

namespace MaIN.Examples.Examples;

public class LocalModelSkillsExample : IExample
{
    public string Name => "Local Model with Skills";

    public async Task RunAsync()
    {
        var hub = new AIHub()
            .UseLocal()
            .WithModel(Local.Phi3Mini)
            .WithSkill<LocalInformationSkill>()
            .Build();

        var prompt = "What is my machine name and who is the current user?";
        Console.WriteLine($"[User]: {prompt}");

        var result = await hub.ChatAsync(prompt);
        Console.WriteLine($"[AI]: {result}");
    }
}