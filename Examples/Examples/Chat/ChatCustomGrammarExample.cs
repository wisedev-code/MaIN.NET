using MaIN.Core.Hub;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using Grammar = MaIN.Domain.Models.Grammar;

namespace Examples.Chat;

public class ChatCustomGrammarExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with grammar is running!");

        var personGrammar = new Grammar("""
                                        root ::= person
                                        person ::= "{" ws "\"name\":" ws name "," ws "\"age\":" ws age "," ws "\"city\":" ws city ws "}"
                                        name ::= "\"" [A-Za-z ]+ "\""
                                        age ::= [1-9] | [1-9][0-9]
                                        city ::= "\"" [A-Za-z ]+ "\""
                                        ws ::= [ \t]*
                                        """, GrammarFormat.GBNF);

        await AIHub.Chat()
            .WithInferenceParams(new InferenceParams
            {
                Grammar = personGrammar
            })
            .WithModel("gemma2:2b")
            .WithMessage("Generate random person")
            .CompleteAsync(interactive: true);
    }
}