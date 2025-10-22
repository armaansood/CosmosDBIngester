using CosmosDBIngester.Models;
using CosmosDBIngester.Services;

namespace CosmosDBIngester;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
