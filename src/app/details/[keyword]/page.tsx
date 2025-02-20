import ArticleCard from "../../components/article";
import NoResults from "../../components/no-results";
import { Article } from "../../models/models";
import { getKeywordDetails } from "../../services/keyword-service";

export default async function Page({
  params,
}: {
  params: Promise<{ keyword: string }>;
}) {
  const { keyword } = await params;
  const keywordsViewModel = await getKeywordDetails(keyword);

  return (
    <div className="row g-4">
      {keywordsViewModel.articleDetails.map((article: Article) => (
        <ArticleCard key={article.id} article={article} />
      ))}
      {keywordsViewModel.articleDetails.length === 0 && (
        <NoResults query={keyword} />
      )}
    </div>
  );
}
