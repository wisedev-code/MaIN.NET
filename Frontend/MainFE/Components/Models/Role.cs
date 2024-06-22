using System.Runtime.Serialization;

namespace MainFE.Components.Models;

public enum Role
{
    [EnumMember(Value = "system")] System = 1,
    [EnumMember(Value = "assistant")] Assistant = 2,
    [EnumMember(Value = "user")] User = 3,
}