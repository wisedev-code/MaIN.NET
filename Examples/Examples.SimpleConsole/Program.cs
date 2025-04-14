using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Services.Services.Models.Commands;

MaINBootstrapper.Initialize();

// await AIHub.Chat()
//     .WithModel("gemma2:2b")
//     .WithMessage("Hello, World!")
//     .CompleteAsync(interactive: true);

await ExtractDataFromFile("./3.pdf");


async Task<string> ExtractDataFromFile(string tempFilePath)
{
    const string messagePrompt = """                                   
                                 Analyze the invoice file you have in memory.                                   
                                 You are an expert invoice analyzer. Your task is to carefully extract all information from the invoice provided                                                                      
                                 Please extract the following information (when available):                                                                      
                                 1. Basic invoice details:                                      
                                 - Invoice number                                      
                                 - Invoice date                                     
                                  - Due date                                      
                                 - Total amount                                      
                                 - Tax amount                                      
                                 - Currency                                                                      
                                 2. Vendor information:                                      
                                 - Name                                      
                                 - Address                                     
                                  - Contact information (phone, email)                                      
                                 - Tax ID / VAT number                                     
                                  - NIP                                                                      
                                 3. Customer information:                                     
                                  - Name                                      
                                 - Address                                      
                                 - Customer ID                                      
                                 - Contact information                                      
                                 - NIP                                                                      
                                 4. Line items - for each product/service: (always provide complete list)                                    
                                 - Description (without numbers)                                      
                                 - Quantity                                      
                                 - Unit price                                      
                                 - Total price                                      
                                 - Tax rate (if applicable)                                                                      
                                 5. Payment information:                                      
                                 - Payment terms                                      
                                 - Payment method                                      
                                 - Bank details (if available)                                                           
""";
    const string secondMessagePrompt = """      
                                       Take previously provided response and verify if all data is correct. If not, please correct it.              
                                       Analyze service and products list is correct you should analyze it and if there are any mistakes, please correct them.      
                                       """;

    var context = AIHub.Agent()
        .WithModel("gemma3:4b")
        .WithInitialPrompt(messagePrompt)
        .WithMemoryParams(new MemoryParams()
        {
            AnswerTokens = 1024,
            MaxMatchesCount = 5,
        })
        .WithSource(new AgentFileSourceDetails()
            {
                Name = "3.pdf", Path = tempFilePath
            },
            AgentSourceType.File)
        .WithSteps(StepBuilder.Instance
            .FetchData(FetchResponseType.AS_System)
            .Answer()
            .Build())
        .Create();

// var context = AIHub.Chat()    
//     .WithModel("gemma3:4b")    
//     .WithFiles([stream])    
//     .WithMemoryParams(new MemoryParams()    
//     {    
//         AnswerTokens = 1100
// /
//})

//     .WithInferenceParams(new InferenceParams()    
//     {    //         ContextSize = 4000    
    var result = await context.ProcessAsync("""
Provide invoice data from this chat in valid JSON format.
Format all dates as YYYY-MM-DD and ensure all numerical values are properly formatted as numbers (not strings) where appropriate.                                   
REMEMBER: Your output must be ONLY valid JSON that can be directly parsed by JsonDocument. Invalid JSON will cause system failures. Do not include any explanations or text outside of the JSON structure.                                   
CRITICAL: Your response must contain ONLY properly formatted JSON that can be directly parsed by a JsonDocument parser without any modifications. Do not include any explanations, introductions, or additional text outside the JSON structure.                                                                      
""");
    
   //var result2 = await context.ProcessAsync("show me line items collection)");
// var result = await context    
//     .WithMessage(secondMessagePrompt)    
//     .CompleteAsync();        
    return result.Message.Content;
}