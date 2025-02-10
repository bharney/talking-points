import React, { JSX } from "react";

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
}

interface TreeNode {
  children: { [key: string]: TreeNode };
  endCount: number;
}

function buildTree(keywords: Keywords[]): TreeNode {
  const root: TreeNode = { children: {}, endCount: 0 };
  keywords.forEach(({ keyword }) => {
    // Split title into words, remove punctuation and lowercase all words
    const words = keyword
      .split(" ")
      .map((w) => w.replace(/[^\w]/g, "").toLowerCase())
      .filter(Boolean);
    words.forEach((word) => {
      let node = root;
      for (const letter of word) {
        if (!node.children[letter]) {
          node.children[letter] = { children: {}, endCount: 0 };
        }
        node = node.children[letter];
      }
      node.endCount++;
    });
  });
  return root;
}

function renderTree(node: TreeNode, prefix = ""): JSX.Element {
  return (
    <ul>
      {Object.entries(node.children).map(([letter, child]) => (
        <li key={prefix + letter}>
          <span>
            {letter}
            {child.endCount > 0 ? ` (${child.endCount})` : ""}
          </span>
          {Object.keys(child.children).length > 0 &&
            renderTree(child, prefix + letter)}
        </li>
      ))}
    </ul>
  );
}

interface WordTreeProps {
  keywords: Keywords[];
}

const WordTree: React.FC<WordTreeProps> = ({ keywords }) => {
  const tree = buildTree(keywords);
  return <div>{renderTree(tree)}</div>;
};

export default WordTree;
