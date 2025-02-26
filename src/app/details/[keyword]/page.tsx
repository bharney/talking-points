import ArticleCard from "../../components/article";
import NoResults from "../../components/no-results";
import Pagination from "../../../components/pagination";
import { Article } from "../../models/models";
import { getKeywordDetails } from "../../services/keyword-service";

export default async function Page({
  params,
  searchParams,
}: {
  params: Promise<{ keyword: string }>;
  searchParams: { [key: string]: string | string[] | undefined };
}) {
  const { keyword } = await params;
  const page = Number(searchParams.page) || 1;
  const pageSize = Number(searchParams.pageSize) || 10;

  const keywordsViewModel = await getKeywordDetails(keyword, page, pageSize);

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center text-light pt-5 mb-4">
        <h2>{decodeURIComponent(keyword)}</h2>
        <span className="text-light">
          {keywordsViewModel.totalArticles} results found
        </span>
      </div>
      <div className="row g-4">
        {keywordsViewModel.articles.map((article: Article) => (
          <ArticleCard key={article.id} article={article} />
        ))}
        {keywordsViewModel.articles.length === 0 && (
          <NoResults query={keyword} />
        )}
      </div>
      {keywordsViewModel.totalPages > 1 && (
        <Pagination
          currentPage={keywordsViewModel.page}
          totalPages={keywordsViewModel.totalPages}
          baseUrl={`/details/${keyword}`}
        />
      )}
    </div>
  );
}
