"use client";
import { useState, useEffect } from "react";
import WordTree, { Article, Keywords } from "../components/WordTree";

interface TreeViewModel {
  articleDetails: Article;
  keywords: Keywords[];
}

export default function Home() {
  const [tree, setTree] = useState<TreeViewModel[]>([]);

  useEffect(() => {
    async function loadArticles() {
      const res = await fetch("https://localhost:7040/Home");
      const treeViewModel = (await res.json()) as TreeViewModel[];
      setTree(treeViewModel);
    }
    loadArticles();
  }, []);

  return (
    <>
      <main className="container pt-5 pb-2">
        <div className="col-xxl-10">
          <h1 className="hero">Talking points</h1>
          <div className="border hero-border border-light w-25 my-4"></div>
        </div>
        <div className="col-lg-10 col-xxl-8">
          <p className="mb-4 hero-text">
            We are looking to find where talking points originate and
            proliforate.
          </p>
          <p className="mb-4 hero-text">
            We take news articles from various sources and build word trees to
            visualize and link to news articles.
          </p>
          <div></div>
          {tree
            .filter((x) => x.keywords)
            .map((item, i) => (
              <WordTree key={i} keywords={item.keywords} />
            ))}
        </div>
      </main>
    </>
  );
}
