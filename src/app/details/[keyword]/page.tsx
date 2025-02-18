import Link from "next/link";
import { Article, Keywords } from "../../models/models";
import ArticleCard from "../../components/article";

interface KeywordsViewModel {
  keywords: Keywords;
  articleDetails: Article[];
}

export default async function Page({
  params,
}: {
  params: Promise<{ keyword: string }>;
}) {
  const { keyword } = await params;
  let keywordsViewModel: KeywordsViewModel = {
    keywords: {
      keyword,
      id: "0",
      articleId: "0",
      count: 0,
    },
    articleDetails: [],
  };
  try {
    const res = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL}/Keyword?keyword=${keyword}`
    );
    keywordsViewModel = await res.json();
  } catch (error) {
    console.log(error);
  }
  return (
    <div className="p-5 mb-4 rounded-3 text-white">
      <ul>
        {keywordsViewModel.articleDetails.map((article) => (
          <ArticleCard key={article.id} article={article} />
        ))}
      </ul>
    </div>
  );
}
