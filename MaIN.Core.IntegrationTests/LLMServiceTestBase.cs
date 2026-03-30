using MaIN.Core.IntegrationTests.Fakes;
using MaIN.Domain.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Core.IntegrationTests;

public class LLMServiceTestBase : IntegrationTestBase
{
    protected readonly FakeHttpClientFactory FakeClientFactory = new();
    protected FakeHttpMessageHandler HttpHandler => FakeClientFactory.Handler;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IHttpClientFactory>(FakeClientFactory);
        services.AddSingleton(new MaINSettings
        {
            OpenAiKey = "test-openai-key",
            AnthropicKey = "test-anthropic-key",
            GeminiKey = "test-gemini-key",
            DeepSeekKey = "test-deepseek-key",
            GroqCloudKey = "test-groq-key",
            XaiKey = "test-xai-key",
        });
    }

    protected static string OpenAiResponse(string content, string model = "gpt-4o-mini") =>
       $$"""
        {
          "choices": [
            {
              "message": {
                "role": "assistant",
                "content": "{{content}}"
              }
            }
          ],
          "model": "{{model}}"
        }
        """;

    protected static string AnthropicResponse(string content) =>
       $$"""
        {
          "content": [
            {
              "type": "text",
              "text": "{{content}}"
            }
          ],
          "model": "claude-sonnet-4-5",
          "id": "msg_test"
        }
        """;
    protected static string OpenAiStreamResponse(string content) =>
       $$$"""
        data: {"choices":[{"delta":{"content":"{{{content}}}"}}]}
        data: [DONE]

        """;
    protected static string AnthropicStreamResponse(string content) =>
       $$$"""
        event: content_block_delta
        data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"{{{content}}}"}}
        event: message_stop
        data: {"type":"message_stop"}

        """;
}
