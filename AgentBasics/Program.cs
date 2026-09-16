using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

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
string prompt = "What are the more populated cities in the world? Respond only with NAME and TOTAL POPULATION";

var chatClient = client.GetChatClient(model);
var chatClientInterface = chatClient.AsIChatClient();

/*ChatClientAgent aiAgent = chatClient.AsAIAgent(instructions: "You are super helpful",
                        name: "Agent1");*/

ChatClientAgent aiAgent = new ChatClientAgent(chatClient.AsIChatClient(),
    instructions: "You are super helpful", name: "Agent 1");

var chatMessage =
    new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, prompt);

var response = await aiAgent.RunAsync<IEnumerable<LargestCity>>(chatMessage);

foreach (var city in response.Result)
{
    Console.WriteLine($"{city.Name} - {city.Population}");
}

record LargestCity(string Name, decimal Population);

