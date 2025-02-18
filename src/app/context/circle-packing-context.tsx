"use client";
import { createContext, useContext, useState, useEffect } from "react";
import { RootData, TreeViewModel } from "../models/models";

type CirclePackingContextType = {
  tree: RootData | null;
};

const CirclePackingContext = createContext<CirclePackingContextType>({
  tree: null,
});

export function CirclePackingProvider({
  children,
}: {
  children: React.ReactNode;
}) {
  const [tree, setTree] = useState<RootData | null>(null);

  useEffect(() => {
    async function loadArticles() {
      if (tree) return; // Don't fetch if we already have data
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
    <CirclePackingContext.Provider value={{ tree }}>
      {children}
    </CirclePackingContext.Provider>
  );
}

export const useCirclePacking = () => useContext(CirclePackingContext);
