using System.Numerics;
using Casper.Network.SDK;
using Casper.Network.SDK.JsonRpc;
using Casper.Network.SDK.Types;
using Casper.Network.SDK.Utils;

namespace AIDistribution;

public struct WalletRecord
{
    public string AccountHash { get; set; }
    public BigInteger Amount { get; set; }
    
    public string _line { get; set; }

    public override string ToString()
    {
        return _line;
    }
}

public class AIDistribution
{
    public static List<WalletRecord> ReadCsvFile(string filePath)
    {
        var records = new List<WalletRecord>();

        using var reader = new StreamReader(filePath);


        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();

            var values = line.Split(';');

            var record = new WalletRecord()
            {
                AccountHash = values[0],
                Amount = BigInteger.Parse(values[1]),
                _line = line,
            };

            records.Add(record);
        }

        return records;
    }
    
    public async Task<string?> Send(
        NetCasperClient casperSdk,
        KeyPair senderKey,
        string contractHash,
        ulong paymentMotes,
        string chainName,
        string recipientKey,
        BigInteger amount
    )
    {
        string? deployHash = null;
        try
        {
            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("recipient", CLValue.Key(new AccountHashKey("account-hash-"+recipientKey))),
                new NamedArg("amount", CLValue.U256(amount))
            };
    
            var deploy = DeployTemplates.ContractCall(new HashKey("hash-" + contractHash),
                "transfer",
                namedArgs,
                senderKey.PublicKey,
                paymentMotes,
                chainName,
                1,
                1_800_000);
            deploy.Sign(senderKey);

            deployHash = deploy.Hash;
            
            var response = await casperSdk.PutDeploy(deploy);

            // extract the deploy hash and use it to wait (up to 2mins) for the execution results
            //
            var hash = response.GetDeployHash();

            if (hash.ToLower() != deployHash.ToLower())
            {
                throw new Exception($"Deploy hash from network {hash} does not match calculated deploy hash {deployHash}");
            }
        }
        catch (RpcClientException e)
        {
            Console.WriteLine("ERROR:\n" + e.RpcError.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return deployHash.ToLower();
    }

    public async Task DistributeAITokens(string inputFile, string outputFile)
    {
        var casperSdk = Values.GetCasperClient(false);
        var sender = Values.User1KeyPair;
        var contractHash = Values.ContractHash;
        var chainName = Values.ChainName;
        var paymentFee = Values.PaymentFee;
        var records = ReadCsvFile(inputFile);

        var deployHashes = new List<string>();
        var count = 0;

        using (var writer = new StreamWriter(outputFile, append: true))
        {
            foreach (var record in records)
            {
                var hash = await Send(casperSdk,
                    sender,
                    contractHash,
                    paymentFee,
                    chainName,
                    record.AccountHash,
                    record.Amount
                );
                deployHashes.Add(hash);
                count++;

                writer.WriteLine($"{hash},{record.ToString()}");
                Console.WriteLine($"{hash},{record.ToString()}");
                if (count % 10 == 0)
                {
                    Console.WriteLine("Progress: " + count);
                    // Thread.Sleep(300);
                }
            }
        }
        
        Console.WriteLine("Transfers done: " + count);
        Console.WriteLine("Checking successful execution of deploys");
        var deploysChecked = 0;
        
        foreach (var hash in deployHashes)
        {
            try
            {
                var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                var response  = await casperSdk.GetDeploy(hash, tokenSource.Token);
                var result = response.Parse();
                if (result.ExecutionResults.Count > 0 &&
                    result.ExecutionResults[0].Cost > 0)
                {
                    deploysChecked++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error with deploy hash: " + hash);
                Console.WriteLine("  Error: " + e.Message);
            }
            
            if (deploysChecked % 10 == 0)
            {
                Console.WriteLine("Progress: " + deploysChecked);
                // Thread.Sleep(300);
            }
        }
        Console.WriteLine("Progress: " + deploysChecked);
    }
}