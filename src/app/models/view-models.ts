import { Article, Keywords } from "./models";

export interface KeywordsViewModel {
  keywords: Keywords;
  articleDetails: Article[];
}
