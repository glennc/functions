using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

// Method for summarizing text
static async Task<string> AISummarizeText(TextAnalyticsClient client, string document, ILogger logger)
{
    var summarizedText = new StringBuilder();
    
    // Prepare analyze operation input. You can add multiple documents to this list and perform the same
    // operation to all of them.
    var batchInput = new List<string>
    {
        document
    };

    TextAnalyticsActions actions = new TextAnalyticsActions()
    {
        ExtractSummaryActions = new List<ExtractSummaryAction>() { new ExtractSummaryAction() }
    };

    // Start analysis process.
    AnalyzeActionsOperation operation = await client.StartAnalyzeActionsAsync(batchInput, actions);
    await operation.WaitForCompletionAsync();
    // View operation status.

    summarizedText.AppendLine($@"AnalyzeActions operation has completed
Created On   : {operation.CreatedOn}
Expires On   : {operation.ExpiresOn}
Id           : {operation.Id}
Status       : {operation.Status}");

    //If you like it indenting in code better you can also do this of course (this is probably more idiomatic):
    /*
    summarizedText.AppendLine("AnalyzeActions operation has completed");
    summarizedText.AppendLine($"Created On   : {operation.CreatedOn}");
    summarizedText.AppendLine($"Expires On   : {operation.ExpiresOn}");
    summarizedText.AppendLine($"Id           : {operation.Id}");
    summarizedText.AppendLine($"Status       : {operation.Status}");
    */

    // View operation results.
    await foreach (AnalyzeActionsResult documentsInPage in operation.Value)
    {
        IReadOnlyCollection<ExtractSummaryActionResult> summaryResults = documentsInPage.ExtractSummaryResults;

        foreach (ExtractSummaryActionResult summaryActionResults in summaryResults)
        {
            if (summaryActionResults.HasError)
            {
                logger.LogError($"  Error!");
                logger.LogError($"  Action error code: {summaryActionResults.Error.ErrorCode}.");
                logger.LogError($"  Message: {summaryActionResults.Error.Message}");
                continue;
            }

            foreach (ExtractSummaryResult documentResults in summaryActionResults.DocumentsResults)
            {
                if (documentResults.HasError)
                {
                    logger.LogError($"  Error!");
                    logger.LogError($"  Document error code: {documentResults.Error.ErrorCode}.");
                    logger.LogError($"  Message: {documentResults.Error.Message}");
                    continue;
                }

                summarizedText.AppendLine($"  Extracted the following {documentResults.Sentences.Count} sentence(s):");


                foreach (SummarySentence sentence in documentResults.Sentences)
                {
                    summarizedText.AppendLine($"  Sentence: {sentence.Text}");
                }
            }
        }
    }

    logger.LogInformation("Returning summarized text: {0}{1}", Environment.NewLine, summarizedText.ToString());
    return summarizedText.ToString();
}

//app should now have DI, Logging, Config, and Environment info. Including all env vars, user secrets, and appsettings files if they exist.
//env vars was fine, you don't have to do this, but I thought I would show you in case you wanted the capabilities.
var app = Host.CreateApplicationBuilder().Build();

//In WebApps these are properties on app, we should change that in .NET so these 2 lines and their usings go away.
var config = app.Services.GetService<IConfiguration>();
var logger = app.Services.GetService<ILogger<Program>>();

var credentials = new AzureKeyCredential(config["AI_SECRET"] ?? "SETCONFIG!");
var endpoint = new Uri(config["AI_URL"] ?? "SETCONFIG!");

string document = @"The extractive summarization feature uses natural language processing techniques to locate key sentences in an unstructured text document. 
    These sentences collectively convey the main idea of the document. This feature is provided as an API for developers. 
    They can use it to build intelligent solutions based on the relevant information extracted to support various use cases. 
    In the public preview, extractive summarization supports several languages. It is based on pretrained multilingual transformer models, part of our quest for holistic representations. 
    It draws its strength from transfer learning across monolingual and harness the shared nature of languages to produce models of improved quality and efficiency.";

var client = new TextAnalyticsClient(endpoint, credentials);

// analyze document text using Azure Cognitive Language Services
var summarizedText = await AISummarizeText(client, document, logger);

Console.WriteLine(summarizedText);