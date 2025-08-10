"use client";
import {
  createContext,
  useContext,
  useState,
  useEffect,
  type ReactNode,
} from "react";

import { RootData, TreeViewModel } from "../models/models";

type CirclePackingContextType = {
  tree: RootData | null;
  loading: boolean;
  error: string | null;
};

const CirclePackingContext = createContext<CirclePackingContextType>({
  tree: null,
  loading: false,
  error: null,
});

export function CirclePackingProvider({ children }: { children: ReactNode }) {
  const [tree, setTree] = useState<RootData | null>(null);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadArticles() {
      if (tree || loading) return; // Avoid duplicate fetches
      setLoading(true);
      setError(null);
      try {
        const res = await fetch("/api/circle-packing");
        if (!res.ok) {
          throw new Error(`HTTP error! status: ${res.status}`);
        }
        let raw: unknown;
        try {
          raw = await res.json();
        } catch {
          throw new Error("Failed to parse JSON for circle packing data");
        }

        if (!Array.isArray(raw)) {
          console.warn("circle-packing: expected array response, got", raw);
          throw new Error("Unexpected data shape (not array)");
        }

        const treeViewModel = raw as Partial<TreeViewModel>[];

        const validItems = treeViewModel.filter((d, idx) => {
          const ok = !!(
            d &&
            d.articleDetails &&
            d.keywords &&
            Array.isArray(d.keywords)
          );
          if (!ok) {
            console.warn("circle-packing: dropping invalid item", {
              index: idx,
              item: d,
            });
          }
          return ok;
        }) as TreeViewModel[];

        const transformData = (data: TreeViewModel[]) =>
          data.map((d) => ({
            id: d.articleDetails.id ?? "unknown", // fallback safety
            name: d.articleDetails.title ?? "Untitled",
            children: d.keywords.map((k) => ({
              id: k.id,
              name: k.keyword,
              loc: k.count,
              color: "hsl(240, 6.20%, 22.20%)",
            })),
          }));

        setTree({
          id: "root",
          name: "Talking Points",
          loc: 0,
          color: "hsl(240, 6.20%, 22.20%)",
          children: transformData(validItems),
        });
      } catch (error: unknown) {
        console.error("Error fetching tree data:", error);
        if (error instanceof Error) {
          setError(error.message);
        } else {
          setError("Failed to load data");
        }
      } finally {
        setLoading(false);
      }
    }
    loadArticles();
  }, [tree, loading]);

  return (
    <CirclePackingContext.Provider value={{ tree, loading, error }}>
      {children}
    </CirclePackingContext.Provider>
  );
}

export const useCirclePacking = () => useContext(CirclePackingContext);
