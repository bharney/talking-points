import { Article } from "../models/models";

export const vectorSearchArticles = async (
  searchPhrase: string
): Promise<Article[]> => {
  try {
    interface VectorResultItem {
      Id?: string | number;
      id?: string | number;
      Title?: string;
      title?: string;
      Description?: string | null;
      description?: string | null;
      SourceName?: string;
      source?: string;
      Url?: string;
      url?: string;
    }
    const encoded = encodeURIComponent(searchPhrase);
    const res = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL}/VectorSearch/hybrid?query=${encoded}`
    );
    if (!res.ok) {
      throw new Error("Failed to fetch vector search articles");
    }
    const payload = await res.json();
    const results = Array.isArray(payload?.results) ? payload.results : [];
    // Map results to Article shape expected by UI
    return (results as VectorResultItem[]).map((r) => ({
      id: r.Id?.toString?.() ?? r.id ?? "",
      title: r.Title ?? r.title ?? "Untitled",
      description: r.Description ?? r.description ?? null,
      source: r.SourceName ?? r.source ?? "",
      url: r.Url ?? r.url ?? "#",
      abstract: null,
    })) as Article[];
  } catch (error) {
    console.error("Error fetching vector search articles:", error);
    return [];
  }
};

export const searchArticles = async (
  searchPhrase: string
): Promise<Article[]> => {
  try {
    const res = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL}/Search?searchPhrase=${searchPhrase}`
    );
    if (!res.ok) {
      throw new Error("Failed to fetch articles");
    }
    return res.json();
  } catch (error) {
    console.error("Error fetching articles:", error);
    return [];
  }
};
