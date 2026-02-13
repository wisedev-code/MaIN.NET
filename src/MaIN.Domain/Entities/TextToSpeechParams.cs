using MaIN.Domain.Models.Abstract;

namespace MaIN.Domain.Entities;

public sealed record TextToSpeechParams(AIModel Model, Voice Voice, bool Playback = false);