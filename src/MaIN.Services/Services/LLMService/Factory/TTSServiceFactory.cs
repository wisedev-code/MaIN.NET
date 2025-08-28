﻿using MaIN.Domain.Configuration;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.TTSService;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Services.Services.LLMService.Factory;

public class TTSServiceFactory(IServiceProvider serviceProvider) : ITTSServiceFactory
{
    public ITextToSpeechService CreateService(BackendType backendType)
    {
        return backendType switch
        {
            BackendType.Self => new TTSService.TextToSpeechService(serviceProvider.GetRequiredService<MaINSettings>()),
            _ => throw new ArgumentOutOfRangeException(nameof(backendType))
        };
    }
}