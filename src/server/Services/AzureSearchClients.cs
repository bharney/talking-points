using System;
using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;

namespace talking_points.Services
{
	public interface IAzureSearchClients
	{
		SearchIndexClient IndexClient { get; }
		SearchClient ArticlesIndexClient { get; }
		SearchClient ChunksIndexClient { get; }
	}

	public class AzureSearchClients : IAzureSearchClients
	{
		public SearchIndexClient IndexClient { get; }
		public SearchClient ArticlesIndexClient { get; }
		public SearchClient ChunksIndexClient { get; }

		public AzureSearchClients(IConfiguration config)
		{
			var endpointValue = config["AzureSearchEndpoint"] ?? throw new InvalidOperationException("AzureSearchEndpoint missing");
			var endpoint = new Uri(endpointValue);
			var articlesIndex = config["AzureSearchArticlesIndexName"] ?? "articles-index";
			var chunksIndex = config["AzureSearchChunkIndexName"] ?? "article-chunks-index";
			var apiKey = config["AzureSearchApiKey"];
			if (!string.IsNullOrWhiteSpace(apiKey))
			{
				var credential = new AzureKeyCredential(apiKey);
				IndexClient = new SearchIndexClient(endpoint, credential);
				ArticlesIndexClient = new SearchClient(endpoint, articlesIndex, credential);
				ChunksIndexClient = new SearchClient(endpoint, chunksIndex, credential);
			}
			else
			{
				var defaultCred = new DefaultAzureCredential(new DefaultAzureCredentialOptions
				{
					ManagedIdentityClientId = config["ManagedIdentityClientId"]
				});
				IndexClient = new SearchIndexClient(endpoint, defaultCred);
				ArticlesIndexClient = new SearchClient(endpoint, articlesIndex, defaultCred);
				ChunksIndexClient = new SearchClient(endpoint, chunksIndex, defaultCred);
			}
		}
	}
}
