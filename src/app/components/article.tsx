import Link from "next/link";
import { Article } from "../models/models";

interface ArticleCardProps {
  article: Article;
}

export default function ArticleCard({ article }: ArticleCardProps) {
  return (
    <div className="col-md-6">
      <div className="h-100 p-5 bg-dark border rounded-3">
        <h3 className="fs-2 text-white mb-3">{article.title}</h3>
        {article.description && (
          <p className="text-light mb-4">{article.description}</p>
        )}
        <Link
          href={article.url}
          className="btn btn-outline-light"
          target="_blank"
        >
          Read More
        </Link>
      </div>
    </div>
  );
}
