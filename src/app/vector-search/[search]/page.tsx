import { vectorSearchArticles } from "../../services/article-service";
import VectorSearchResults from "../../components/vector-search-results";

export default async function VectorSearchPage({
  params,
}: {
  params: { search: string };
}) {
  const { search } = params;
  const articles = await vectorSearchArticles(search);

  return (
    <div className="container py-4">
      <h1 className="display-5 fw-bold text-white mb-4">
        Vector search results for &quot;{decodeURIComponent(search)}&quot;
      </h1>
      <VectorSearchResults articles={articles} searchQuery={search} hybrid />
    </div>
  );
}
