"use client";
import { useState, memo } from "react";
import { CirclePacking } from "@nivo/circle-packing";
import { RootData, TreeViewModel } from "../app/models/models";
import Loading from "../app/common/loading";
import { useRouter } from "next/navigation";

export const CirclePackingChart = memo(function CirclePackingChart() {
  const [tree, setTree] = useState<RootData | null>(null);
  const [zoomedId, setZoomedId] = useState<string | null>(null);
  const router = useRouter();
  async function loadArticles() {
    const res = await fetch("https://localhost:7040/Home");
    const treeViewModel = (await res.json()) as TreeViewModel[];
    const transformData = (data: TreeViewModel[]) => {
      return data
        .filter((x) => x.keywords)
        .map((d) => ({
          name: d.articleDetails.title,
          children: d.keywords.map((k) => ({
            name: k.keyword,
            loc: k.count,
            color: "hsl(240, 6.20%, 22.20%)",
          })),
        }));
    };
    // Wrap the array in a root object.
    setTree({
      name: "Talking Points",
      loc: 0,
      color: "hsl(240, 6.20%, 22.20%)",
      children: transformData(treeViewModel),
    });
  }
  loadArticles();
  // set width/height to 50vw, this needs to be calculated
  let width = (window?.innerWidth ?? 1) / 2;
  // if mobile, set height to 25vh, else the fill the screen
  let height = window?.innerHeight / 2;
  if (window?.innerWidth < 768) {
    height = window.innerHeight / 3;
    width = window.innerWidth / 1.5;
  }
  const commonProperties = {
    width,
    height,
    data: tree,
    padding: 2,
    id: "name",
    value: "loc",
    labelsSkipRadius: 1,
  };
  if (!tree) {
    return <Loading />;
  }
  return (
    tree && (
      <CirclePacking
        {...commonProperties}
        enableLabels
        labelsFilter={(label) => label.node.height === 0}
        labelTextColor={{
          from: "color",
          modifiers: [["darker", 2]],
        }}
        zoomedId={zoomedId}
        motionConfig="slow"
        onClick={(node) => {
          setZoomedId(zoomedId === node.id ? null : node.id);
          router.push(`/details/${encodeURIComponent(node.id)}`);
        }}
        theme={{
          tooltip: {
            container: {
              background: "#FFFFFF",
            },
            basic: {
              color: "#000000",
            },
          },
        }}
        leavesOnly
      />
    )
  );
});
