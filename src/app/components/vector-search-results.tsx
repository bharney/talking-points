import { Article } from "../models/models";
import ArticleCard from "./article";
import NoResults from "./no-results";

interface VectorSearchResultsProps {
  articles: Article[];
  searchQuery: string;
  hybrid?: boolean;
}

// Server component (no 'use client') mirroring SearchResults but tailored for vector / hybrid results.
export default function VectorSearchResults({
  articles,
  searchQuery,
  hybrid = true,
}: VectorSearchResultsProps) {
  if (!articles || articles.length === 0) {
    return <NoResults query={searchQuery} />;
  }

  return (
    <div className="row g-4">
      <div className="col-12">
        <p className="text-secondary small mb-2">
          {hybrid ? "Hybrid Vector" : "Vector"} search returned{" "}
          {articles.length} result
          {articles.length === 1 ? "" : "s"} for &quot;
          {decodeURIComponent(searchQuery)}&quot;.
        </p>
      </div>
      {articles.map((article) => (
        <ArticleCard key={article.id} article={article} />
      ))}
    </div>
  );
}
