import { KeywordsViewModel } from "../models/view-models";

export async function getKeywordDetails(
  keyword: string
): Promise<KeywordsViewModel> {
  try {
    const res = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL}/Keyword?keyword=${keyword}`
    );
    return await res.json();
  } catch (error) {
    console.error("Error fetching keyword details:", error);
    return {
      keywords: {
        keyword,
        id: "0",
        articleId: "0",
        count: 0,
      },
      articleDetails: [],
    };
  }
}
