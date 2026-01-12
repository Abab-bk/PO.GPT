using PO.GPT.Commands;
using Spectre.Console.Cli;

var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellationTokenSource.Cancel();
    Console.WriteLine("Cancellation requested...");
};

var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<TranslateCommand>("translate");
    config.AddCommand<VersionCommand>("version");
});
return await app.RunAsync(args, cancellationTokenSource.Token);