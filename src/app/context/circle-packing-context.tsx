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
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Home`);
      const treeViewModel = (await res.json()) as TreeViewModel[];
      const transformData = (data: TreeViewModel[]) => {
        return data
          .filter((x) => x.keywords)
          .map((d) => ({
            id: d.articleDetails.id,
            name: d.articleDetails.title,
            children: d.keywords.map((k) => ({
              id: k.id,
              name: k.keyword,
              loc: k.count,
              color: "hsl(240, 6.20%, 22.20%)",
            })),
          }));
      };
      setTree({
        id: "root",
        name: "Talking Points",
        loc: 0,
        color: "hsl(240, 6.20%, 22.20%)",
        children: transformData(treeViewModel),
      });
    }
    loadArticles();
  }, [tree]);

  return (
    <CirclePackingContext.Provider value={{ tree }}>
      {children}
    </CirclePackingContext.Provider>
  );
}

export const useCirclePacking = () => useContext(CirclePackingContext);
