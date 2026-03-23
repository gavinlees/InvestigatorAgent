using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
var builder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0070
builder.AddGoogleAIGeminiChatCompletion("gemini-1.5-pro", "key");
#pragma warning restore SKEXP0070
var kernel = builder.Build();
var chatService = kernel.GetRequiredService<IChatCompletionService>();
Console.WriteLine(chatService.GetType().Name);
