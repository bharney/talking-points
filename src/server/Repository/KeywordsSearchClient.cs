using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using talking_points.Controllers;
using talking_points.Models;

namespace talking_points.Repository
{

    public class KeywordsSearchClient : IKeywordsSearchClient
    {
        private readonly SearchClient _searchClient;
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        public KeywordsSearchClient(
            ILogger<KeywordsSearchClient> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            var searchEndpoint = _config["SearchEndpoint"];
            var searchApiKey = _config["SearchApiKey"];
            var searchIndexKeywords = _config["SearchIndexKeywords"];
            if (!string.IsNullOrEmpty(searchEndpoint)
                && !string.IsNullOrEmpty(searchApiKey)
                && !string.IsNullOrEmpty(searchIndexKeywords))
            {
                _searchClient =
                    new SearchClient(
                        new Uri(searchEndpoint),
                        searchIndexKeywords,
                        new AzureKeyCredential(searchApiKey)
                    );
            }
            else
            {
                throw new ArgumentNullException("SearchClient is not initialized.");
            }

        }

        public async Task<SearchResults<T>> SearchAsync<T>(string query)
        {
            var options = new SearchOptions
            {
                IncludeTotalCount = true
            };
            var response = await _searchClient.SearchAsync<T>(query, options);
            return response.Value;
        }

        async Task<SearchResults<T>> IKeywordsSearchClient.SearchAsync<T>(string query)
        {
            return await this.SearchAsync<T>(query);
        }
    }
}