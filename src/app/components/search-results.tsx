import { Article } from "../models/models";
import ArticleCard from "./article";
import NoResults from "./no-results";

interface SearchResultsProps {
  articles: Article[];
  searchQuery: string;
}

export default function SearchResults({
  articles,
  searchQuery,
}: SearchResultsProps) {
  if (articles.length === 0) {
    return <NoResults query={searchQuery} />;
  }

  return (
    <div className="row g-4">
      {articles.map((article) => (
        <ArticleCard key={article.id} article={article} />
      ))}
    </div>
  );
}
