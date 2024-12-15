
using System.Diagnostics;
using Casper.Network.SDK;
using Casper.Network.SDK.JsonRpc;
using Casper.Network.SDK.Types;
using DotNetEnv;

namespace AIDistribution;

public static class Values
{
    private static bool _loaded;
    private static readonly object _lock = new();

    public static void Load()
    {
        lock (_lock)
        {
            if (_loaded)
                return;

            Env.Load();

            _loaded = true;
        }
    }
    
    public static string NodeAddress 
    {
        get
        {
            Load();
            string value = Environment.GetEnvironmentVariable("NODE_ADDRESS");
            Debug.Assert(value is not null);
            return value;
        }
    }

    public static NetCasperClient GetCasperClient(bool logging = false)
    {
        RpcLoggingHandler loggingHandler = null;
        if(logging)
            loggingHandler = new RpcLoggingHandler(new HttpClientHandler())
            {
                LoggerStream = new StreamWriter(Console.OpenStandardOutput())
            };
        
        var httpClient = loggingHandler != null ? new HttpClient(loggingHandler) : new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", "55f79117-fc4d-4d60-9956-65423f39a06a");
        
        return new NetCasperClient(NodeAddress, httpClient);
    }

    static string _apiVersion = string.Empty;
    static readonly SemaphoreSlim _semaphore = new(1, 1);

    public static async Task<string> GetApiVersion(ICasperClient casperSdk)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!string.IsNullOrWhiteSpace(_apiVersion)) 
                return _apiVersion;
            
            var nodeStatusResponse = await casperSdk.GetNodeStatus();

            _apiVersion = nodeStatusResponse.Parse().ApiVersion.Substring(0, 1);

            return _apiVersion;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public static string ChainName
    {
        get
        {
            Load();
            var value = Environment.GetEnvironmentVariable("CHAIN_NAME");
            Debug.Assert(value is not null);
            return value;
        }
    }
    
    public static string ContractHash
    {
        get
        {
            Load();
            var value = Environment.GetEnvironmentVariable("CONTRACT_HASH");
            Debug.Assert(value is not null);
            return value;
        }
    }
    public static ulong PaymentFee
    {
        get
        {
            Load();
            var value = Environment.GetEnvironmentVariable("PAYMENT_FEE");
            Debug.Assert(value is not null);
            return ulong.Parse(value);
        }
    }
    
    private static KeyPair GetKeyPair(string envVar)
    {
        Load();
        var file = Environment.GetEnvironmentVariable(envVar);
        Debug.Assert(file is not null);
        var value = KeyPair.FromPem(file);
        Debug.Assert(value is not null);
        return value;
    }
    
    public static KeyPair User1KeyPair => GetKeyPair("USER_1");
    public static KeyPair User2KeyPair => GetKeyPair("USER_2");
    public static KeyPair User3KeyPair => GetKeyPair("USER_3");
    public static KeyPair User4KeyPair => GetKeyPair("USER_4");
    public static KeyPair User5KeyPair => GetKeyPair("USER_5");
    public static KeyPair User6KeyPair => GetKeyPair("USER_6");
    public static KeyPair User7KeyPair => GetKeyPair("USER_7");
    public static KeyPair User8KeyPair => GetKeyPair("USER_8");
    public static KeyPair User9KeyPair => GetKeyPair("USER_9");
    public static KeyPair User10KeyPair => GetKeyPair("USER_10");

    public static KeyPair Node1KeyPair => GetKeyPair("NODE_1");
    public static KeyPair Node2KeyPair => GetKeyPair("NODE_2");
    public static KeyPair Node3KeyPair => GetKeyPair("NODE_3");
    public static KeyPair Node4KeyPair => GetKeyPair("NODE_4");
    public static KeyPair Node5KeyPair => GetKeyPair("NODE_5");
    
    public static void TryToDeleteFile(string path)
    {
        if(File.Exists(path))
            File.Delete(path);
    }
}