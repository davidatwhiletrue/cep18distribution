using Casper.Network.SDK;
using Casper.Network.SDK.JsonRpc;
using Casper.Network.SDK.Types;
using Casper.Network.SDK.Utils;

namespace AIDistribution;

public struct WalletRecord
{
    public string AccountHash { get; set; }
    public ulong Amount { get; set; }
    
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
                Amount = ulong.Parse(values[1]),
                _line = line,
            };

            records.Add(record);
        }

        return records;
    }

    public AIDistribution()
    {
    }
    
    public Deploy TransferAITokenDeploy(
        KeyPair senderKey,
        string contractHash,
        ulong paymentMotes,
        string chainName,
        string recipientKey,
        ulong amount)
    {
        var namedArgs = new List<NamedArg>()
        {
            new NamedArg("recipient", CLValue.Key(new AccountHashKey(recipientKey))),
            new NamedArg("amount", CLValue.U256(amount))
        };
    
        var deploy = DeployTemplates.ContractCall(new HashKey(contractHash),
            "transfer",
            namedArgs,
            senderKey.PublicKey,
            paymentMotes,
            chainName,
            1,
            1_800_000);
        deploy.Sign(senderKey);
        return deploy;
    }
    
    public async Task<string?> Send(
        NetCasperClient casperSdk,
        KeyPair senderKey,
        string contractHash,
        ulong paymentMotes,
        string chainName,
        string recipientKey,
        ulong amount
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

        using (var writer = new StreamWriter(outputFile, append: true))
        {
            var count = 0;

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
                count++;

                writer.WriteLine($"{hash},{record.ToString()}");
                Console.WriteLine($"{hash},{record.ToString()}");
                if (count % 100 == 0)
                {
                    Console.WriteLine("Progress: " + count);
                    // Thread.Sleep(300);
                }
            }
        }
    }
}