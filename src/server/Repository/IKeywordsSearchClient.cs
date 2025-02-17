using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using talking_points.Models;

namespace talking_points.Repository
{
    public interface IKeywordsSearchClient
    {
        Task<SearchResults<T>> SearchAsync<T>(string query);
    }
}