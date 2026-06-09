// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using MaIN.Core.Skills;

namespace MaIN.Examples.Skills;

[Skill]
public class LocalInformationSkill
{
    [SkillMember, Description("Gets the name of the local machine.")]
    public string GetMachineName()
    {
        return Environment.MachineName;
    }

    [SkillMember, Description("Gets the name of the current user.")]
    public string GetCurrentUser()
    {
        return Environment.UserName;
    }
}