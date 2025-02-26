import { Article, Keywords } from "./models";

export interface KeywordsViewModel {
  keywords: Keywords;
  articles: Article[];
  totalArticles: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
