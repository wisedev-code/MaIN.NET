using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Domain.Models.Concrete;

namespace Examples.Chat;

public class ChatGrammarExampleGemini : IExample
{
    public async Task Start()
    {
        GeminiExample.Setup(); //We need to provide Gemini API key
        Console.WriteLine("(Gemini) ChatExample is running!");

        var grammarValue = """
                           {
                             "$schema": "https://json-schema.org/draft/2020-12/schema",
                             "title": "User",
                             "type": "object",
                             "properties": {
                               "name": {
                                 "type": "string",
                                 "description": "Full name of the user."
                               },
                               "age": {
                                 "type": "integer",
                                 "minimum": 0,
                                 "description": "User's age in years."
                               },
                               "email": {
                                 "type": "string",
                                 "format": "email",
                                 "description": "User's email address."
                               }
                             },
                             "required": ["name", "email"]
                           }
                           """;

        await AIHub.Chat()
          .WithModel<Gemini2_5Flash>()
          .WithMessage("Generate random person")
          .WithInferenceParams(new InferenceParams
          {
            Grammar = new Grammar(grammarValue, GrammarFormat.JSONSchema)
          })
          .CompleteAsync(interactive: true);
    }
}