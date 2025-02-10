"use client";
import { useState, useEffect } from "react";
import { CirclePackingChart } from "../components/circle-packing";
import { RootData, TreeViewModel } from "./models/models";

export default function Home() {
  const [tree, setTree] = useState<RootData | null>(null);

  useEffect(() => {
    async function loadArticles() {
      const res = await fetch("https://localhost:7040/Home");
      const treeViewModel = (await res.json()) as TreeViewModel[];
      const transformData = (data: TreeViewModel[]) => {
        return data
          .filter((x) => x.keywords)
          .map((d) => ({
            name: d.articleDetails.title,
            children: d.keywords.map((k) => ({
              name: k.keyword,
              loc: k.count,
              color: "hsl(240, 6.20%, 22.20%)",
            })),
          }));
      };
      // Wrap the array in a root object.
      setTree({
        name: "Talking Points",
        loc: 0,
        color: "hsl(240, 6.20%, 22.20%)",
        children: transformData(treeViewModel),
      });
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
          {tree && <CirclePackingChart data={tree} />}
        </div>
      </main>
    </>
  );
}
