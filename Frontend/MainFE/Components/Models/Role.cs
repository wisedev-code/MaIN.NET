using System.Runtime.Serialization;

namespace MainFE.Components.Models;

public enum Role
{
    [EnumMember(Value = "System")] System = 1,
    [EnumMember(Value = "Assistant")] Assistant = 2,
    [EnumMember(Value = "User")] User = 3,
}