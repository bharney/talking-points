import { Article } from "../models/models";

export const vectorSearchArticles = async (
  searchPhrase: string
): Promise<Article[]> => {
  // Robust strategy: try chunk-hybrid first, then hybrid, then classic search.
  const encoded = encodeURIComponent(searchPhrase);

  type AnyResultItem = {
    // Common shapes across endpoints
    Id?: string | number;
    id?: string | number;
    Title?: string;
    title?: string;
    Description?: string | null;
    description?: string | null;
    SourceName?: string;
    source?: string;
    Url?: string; // some APIs
    URL?: string; // server sends this casing currently
    url?: string;
    Snippet?: string; // chunk-hybrid
    snippet?: string;
  };

  const toArticles = (items: AnyResultItem[]): Article[] =>
    items.map((r) => ({
      id: r.Id?.toString?.() ?? r.id?.toString?.() ?? "",
      title: r.Title ?? r.title ?? "Untitled",
      // Prefer snippet when available; fall back to Description
      description:
        (r.Snippet ?? r.snippet ?? r.Description ?? r.description ?? "") || "",
      source: r.SourceName ?? r.source ?? "",
      url: r.Url ?? r.URL ?? r.url ?? "#",
      abstract: null,
    }));

  // Helper to safely fetch JSON
  const tryFetch = async (path: string) => {
    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}${path}`);
      if (!res.ok) return null;
      return res.json();
    } catch {
      return null;
    }
  };

  // 1) chunk-hybrid (often better recall for short phrases)
  const chunkPayload = await tryFetch(
    `/VectorSearch/chunk-hybrid?query=${encoded}&topChunks=30&topArticles=12`
  );
  let results: AnyResultItem[] = Array.isArray(chunkPayload?.results)
    ? (chunkPayload.results as AnyResultItem[])
    : [];
  if (results.length > 0) return toArticles(results);

  // 2) standard hybrid
  const hybridPayload = await tryFetch(`/VectorSearch/hybrid?query=${encoded}`);
  results = Array.isArray(hybridPayload?.results)
    ? (hybridPayload.results as AnyResultItem[])
    : [];
  if (results.length > 0) return toArticles(results);

  // 3) fallback to lexical search API
  try {
    const classicRes = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL}/Search?searchPhrase=${encoded}`
    );
    if (!classicRes.ok) return [];
    const classic = await classicRes.json();
    return Array.isArray(classic)
      ? (classic as AnyResultItem[]).map((r) => ({
          id: r.Id?.toString?.() ?? r.id?.toString?.() ?? "",
          title: r.Title ?? r.title ?? "Untitled",
          description: (r.Description ?? r.description ?? "") || "",
          source: r.SourceName ?? r.source ?? "",
          url: r.Url ?? r.URL ?? r.url ?? "#",
          abstract: null,
        }))
      : [];
  } catch (error) {
    console.error("Error fetching vector search articles (fallback):", error);
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
