import { Article } from "../models/models";

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
