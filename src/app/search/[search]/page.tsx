import * as React from "react";
import { Article } from "../../models/models";
import Link from "next/link";

export default async function Page({
  params,
}: {
  params: Promise<{ search: string }>;
}) {
  const { search } = await params;
  let articles: Article[] = [];
  try {
    const res = await fetch(
      `https://localhost:7040/Search?searchPhrase=${search}`
    );
    articles = (await res.json()) as Article[];
  } catch (error) {
    console.log(error);
  }
  return (
    <div className="p-5 mb-4 rounded-3 text-white">
      <div className="container-fluid py-5">
        <h1 className="display-5 fw-bold">Search results</h1>
        <ul>
          {articles.map((article) => (
            <li key={article.id}>
              <Link href={article.url}>{article.title}</Link>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
