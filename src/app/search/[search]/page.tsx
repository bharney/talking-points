import * as React from "react";
import { searchArticles } from "../../services/article-service";
import SearchResults from "../../components/search-results";

export default async function Page({
  params,
}: {
  params: { search: string };
}) {
  const { search } = params;
  const articles = await searchArticles(search);

  return (
    <div className="container py-4">
      <h1 className="display-5 fw-bold text-white mb-4">
        Search results for &quot;{decodeURIComponent(search)}&quot;
      </h1>
      <SearchResults articles={articles} searchQuery={search} />
    </div>
  );
}
