# Program to distribute CEP-18 tokens

## Setup

You need .NET 8 SDK to run the program.
If it's not installed in your system, follow the instructions from [here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

Access to the program path and restore nuget packages:

```bash
cd AIDistribution
dotnet restore
```
##

Create a `.env` file with following values:

```
NODE_ADDRESS=http://127.0.0.1:7777/rpc
CHAIN_NAME=casper
CONTRACT_HASH=36322350b8d859c3529e81a0e6a8a87819a37f118e776658308de40ef0a8ffa4
PAYMENT_FEE=500000000
USER_1=/path/to/secret_key.pem 
```

## Prepare the input file

The input file must be a CSV with ';' as column delimiter.
First column is the account hash of the recipient. Second column is the amount of tokens to send (with all decimals).

## Run the distribution token program

Run the program to start sending the transfers to the configured Casper node:

```
dotnet run --project ./input.csv ./output.csv
```

The program will go through the input file and will create an output file with deploy hash for each transfer sent.

When the program terminates sending the transfers, it starts a process to check if 
the transfers have been completed successfully. Pay attention to the console output 
to check for any errors that may appear.