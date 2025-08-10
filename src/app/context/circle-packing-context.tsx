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

        // Backend (ASP.NET) likely returns PascalCase (ArticleDetails, Keywords)
        // while front-end interfaces expect camelCase. Normalize both forms.
        interface RawKeyword {
          id?: string;
          Id?: string;
          keyword?: string;
          Keyword?: string;
          articleId?: string;
          ArticleId?: string;
          count?: number;
          Count?: number;
        }
        interface RawArticleDetails {
          id?: string;
          Id?: string;
          title?: string;
          Title?: string;
          description?: string;
          Description?: string;
          source?: string;
          Source?: string;
          url?: string;
          URL?: string;
          abstract?: string | null;
          Abstract?: string | null;
        }
        interface RawTreeItem {
          articleDetails?: RawArticleDetails;
          ArticleDetails?: RawArticleDetails;
          keywords?: RawKeyword[];
          Keywords?: RawKeyword[];
        }
        const treeViewModel = raw as RawTreeItem[];

        const normalizeItem = (d: RawTreeItem) => {
          if (!d) return null;
          // prefer camelCase if present, else map from PascalCase
          const articleDetails = d.articleDetails || d.ArticleDetails;
          const keywords = d.keywords || d.Keywords;
          if (!articleDetails || !Array.isArray(keywords)) return null;
          return {
            articleDetails: {
              id: articleDetails.id || articleDetails.Id,
              title: articleDetails.title || articleDetails.Title,
              description:
                articleDetails.description || articleDetails.Description,
              source: articleDetails.source || articleDetails.Source,
              url: articleDetails.url || articleDetails.URL,
              abstract:
                articleDetails.abstract || articleDetails.Abstract || null,
            },
            keywords: keywords.map((k: RawKeyword) => ({
              id: k.id || k.Id,
              keyword: k.keyword || k.Keyword,
              articleId: k.articleId || k.ArticleId,
              count: k.count || k.Count || 0,
            })),
          } as TreeViewModel;
        };

        const validItems: TreeViewModel[] = treeViewModel
          .map((d, idx) => {
            const norm = normalizeItem(d);
            if (!norm) {
              console.warn(
                "circle-packing: dropping invalid item (shape mismatch)",
                { index: idx, d }
              );
            }
            return norm;
          })
          .filter(Boolean) as TreeViewModel[];

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
