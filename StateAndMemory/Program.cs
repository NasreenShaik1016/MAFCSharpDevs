using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;

//string apikey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string apiKey = configuration["AZURE_OPENAI_API_KEY"]!;
string endpoint = configuration["AZURE_OPENAI_ENDPOINT"]!;
string azureDbConnectionString = configuration["COSMOS_DB_CONNECTION_STRING"]!;


var client = new OpenAIClient(
    new System.ClientModel.ApiKeyCredential(apiKey),
    new OpenAIClientOptions
    {
        Endpoint = new Uri(endpoint)
    }
);

string model = "gpt-5-nano";

string? prompt = null;




var chatClient = client.GetChatClient(model);

var historyProvider = new CosmosChatHistoryProvider(
    azureDbConnectionString!,
    "AgentDatabase",
    "ChatHistory",
    stateInitializer: session => new CosmosChatHistoryProvider.State(
        conversationId: "conv-123",
        tenantId: "tenant-a",
        userId: "user-1"
        )

    );

var pipeline = new PipelineCompactionStrategy(new SummarizationCompactionStrategy(chatClient.AsIChatClient(),
    CompactionTriggers.TokensExceed(100),
    summarizationPrompt: """Summarize the following conversation in a concise manner, while preserving important details and context."""
    ));

ChatClientAgentOptions agentOptions = new()
{
    AIContextProviders = [new CompactionProvider(pipeline)],
    ChatHistoryProvider = historyProvider,
    ChatOptions = new ChatOptions()
    {
        RawRepresentationFactory = _ => new ChatCompletionOptions()
        {
            ReasoningEffortLevel = ChatReasoningEffortLevel.Minimal

        }
    }
};

ChatClientAgent aiAgent = new ChatClientAgent(chatClient.AsIChatClient(), agentOptions);
var session = await aiAgent.CreateSessionAsync();

while (true)
{
    Console.WriteLine("Enter your prompt (or type 'exit' to quit):");
    prompt = Console.ReadLine();
    if (prompt?.ToLower() == "exit")
    {
        break;
    }
    var contents = new List<AIContent>
{
    new TextContent(prompt),
   // new DataContent(rom,"application/pdf")
};

    var messages = new List<Microsoft.Extensions.AI.ChatMessage>() { new(ChatRole.User,prompt)
};

    var chatMessage = new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, contents);

    await foreach (var item in aiAgent.RunStreamingAsync(chatMessage, session))
    {
        Console.Write(item.Text);
        //if (item.Contents != null && item.Contents.Any() && item.Contents.First() is UsageContent usageContent)
        //{
        //    PrintUsageDetails(usageContent.Details);
        //}
    }

}



void PrintUsageDetails(UsageDetails usageDetails)
{
    Console.WriteLine("Usage Details:");
    Console.WriteLine($"Total Token Count: {usageDetails.TotalTokenCount}");
    Console.WriteLine($"Total Input Count: {usageDetails.InputTokenCount}");
    Console.WriteLine($"Total Output Count: {usageDetails.OutputTokenCount}");
    Console.WriteLine($"Total Reasoning Count: {usageDetails.ReasoningTokenCount}");
}

record LargestCity(string Name, decimal Population);
