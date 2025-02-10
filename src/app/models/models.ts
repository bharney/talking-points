export interface TreeViewModel {
  articleDetails: Article;
  keywords: Keywords[];
}

export interface RootData {
  name: string;
  loc?: number;
  color?: string;
  children?: RootData[];
}

export interface Article {
  id: string;
  description: string;
  title: string;
  source: string;
  url: string;
  abstract: string | null;
}

export interface Keywords {
  id: string;
  keyword: string;
  articleId: string;
  count: number;
}
