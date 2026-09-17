using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string apiKey = configuration["AZURE_OPENAI_API_KEY"]!;
string endpoint = configuration["AZURE_OPENAI_ENDPOINT"]!;

var client = new OpenAIClient(
    new System.ClientModel.ApiKeyCredential(apiKey),
    new OpenAIClientOptions
    {
        Endpoint = new Uri(endpoint)
    }
);
string model = "gpt-5-nano"; 

string prompt = "Give me the first few lines of the document in page 10";

var chatClient = client.GetChatClient(model);

/*ChatClientAgent aiAgent = chatClient.AsAIAgent(instructions: "You are super helpful",
                        name: "Agent1");
*/

ChatClientAgentOptions agentOptions = new()
{
    ChatOptions = new ChatOptions()
    {
        RawRepresentationFactory = _ => new ChatCompletionOptions()
        {
            ReasoningEffortLevel = ChatReasoningEffortLevel.Minimal
        }
    }
};

// AI Agent with options
ChatClientAgent aiAgent = new ChatClientAgent(chatClient.AsIChatClient(),
                            agentOptions);

var pdfPath = Path.Combine(
    AppContext.BaseDirectory,
    "Documents",
    "HBR_How_Apple_Is_Organized_For_Innovation.pdf");
var pdfBytes = await File.ReadAllBytesAsync(pdfPath);
var rom = new ReadOnlyMemory<byte>(pdfBytes);
var contents = new List<AIContent>
{
    new TextContent(prompt),
    new DataContent(rom,"application/pdf")
};

var chatMessage =
    new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, contents);

/*var chatClientAgentRunOptions 
    = new ChatClientAgentRunOptions(new ChatOptions()
    {
        RawRepresentationFactory = _ => new ChatCompletionOptions()
        {
            ReasoningEffortLevel = ChatReasoningEffortLevel.Minimal
        }
    });*/
/*
AgentResponse<IEnumerable<LargestCity>> response 
    = await aiAgent.RunAsync<IEnumerable<LargestCity>>(chatMessage, 
    options: chatClientAgentRunOptions);

foreach (var city in response.Result)
{
    Console.WriteLine($"{city.Name} - {city.Population}");
}*/


await foreach (var item in aiAgent.RunStreamingAsync(chatMessage))
{
    Console.Write(item.Text);

    if (item.Contents != null && item.Contents.Any() &&
        item.Contents[0] is UsageContent usageContent)
    {
        PrintUsage(usageContent.Details);
    }
}

void PrintUsage(UsageDetails usage)
{
    Console.WriteLine();
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Total token count: {usage.TotalTokenCount}");
    Console.WriteLine($"Input token count: {usage.InputTokenCount}");
    Console.WriteLine($"Output token count: {usage.OutputTokenCount}");
    Console.WriteLine($"Reasoning token count: {usage.ReasoningTokenCount}");
    Console.ForegroundColor = ConsoleColor.Gray;
}

record LargestCity(string Name, decimal Population);