import * as React from "react";
import { Article } from "../../models/models";
import Link from "next/link";
import ArticleCard from "../../components/article";

export default async function Page({
  params,
}: {
  params: Promise<{ search: string }>;
}) {
  const { search } = await params;
  let articles: Article[] = [];
  try {
    const res = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL}/Search?searchPhrase=${search}`
    );
    articles = (await res.json()) as Article[];
  } catch (error) {
    console.log(error);
  }
  return (
    <div className="container py-4">
      <h1 className="display-5 fw-bold text-white mb-4">
        Search results for &quot;{search}&quot;
      </h1>
      <div className="row g-4">
        {articles.map((article) => (
          <ArticleCard key={article.id} article={article} />
        ))}
        {articles.length === 0 && (
          <div className="col-12">
            <div className="p-5 text-center bg-dark border rounded-3">
              <p className="fs-4 text-light">
                No results found for &quot;{search}&quot;
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
