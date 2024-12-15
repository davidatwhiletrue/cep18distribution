await AIAgent.DistributeAITokens(args[0], args[1]);

public class AIAgent
{
    public static async Task DistributeAITokens(string input, string output)
    {
        var aiDistribution = new AIDistribution.AIDistribution();
        await aiDistribution.DistributeAITokens(input, output);
    }
}
