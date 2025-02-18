"use client";
import { useState, memo, useMemo } from "react";
import { CirclePacking } from "@nivo/circle-packing";
import Loading from "../common/loading";
import { useRouter } from "next/navigation";
import { useCirclePacking } from "../context/circle-packing-context";

export const CirclePackingChart = memo(function CirclePackingChart() {
  const { tree } = useCirclePacking();
  const [zoomedId, setZoomedId] = useState<string | null>(null);
  const router = useRouter();

  const { width, height } = useMemo(() => {
    const w = (window?.innerWidth ?? 1) / 2;
    let h = window?.innerHeight / 2;
    if (window?.innerWidth < 768) {
      h = window.innerHeight / 3;
      return { width: window.innerWidth / 1.5, height: h };
    }
    return { width: w, height: h };
  }, [typeof window !== "undefined" ? window.innerWidth : 0]);

  const commonProperties = useMemo(
    () => ({
      width,
      height,
      data: tree,
      padding: 2,
      id: "name",
      value: "loc",
      labelsSkipRadius: 1,
    }),
    [width, height, tree]
  );

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
