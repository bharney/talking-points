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
    if (typeof window !== "undefined" ? window.innerWidth : 0) {
      const w = (window?.innerWidth ?? 1) / 2;
      let h = window?.innerHeight;
      if (window?.innerWidth < 768) {
        h = window.innerHeight / 3;
        return { width: window.innerWidth / 1.5, height: h };
      }
      return { width: w, height: h };
    }
    return { width: 0, height: 0 };
    // eslint-disable-next-line react-hooks/exhaustive-deps
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
          modifiers: [["darker", 4]],
        }}
        borderWidth={1}
        borderColor={{
          from: "color",
          modifiers: [["darker", 0.2]],
        }}
        zoomedId={zoomedId}
        motionConfig="gentle"
        onClick={(node) => {
          setZoomedId(zoomedId === node.id ? null : node.id);
          router.push(
            `/details/${encodeURIComponent(node.id)}?page=1&pageSize=10`
          );
        }}
        theme={{
          tooltip: {
            container: {
              background: "#1f2937", // gray-800
              color: "#f3f4f6", // gray-100
              fontSize: "14px",
              borderRadius: "6px",
              boxShadow: "0 4px 6px rgba(0,0,0,0.2)",
              padding: "8px 12px",
            },
          },
          labels: {
            text: {
              fontSize: 11,
              fontWeight: 500,
              letterSpacing: 0.3,
              wordWrap: "break-word",
              width: "60px",
              textAlign: "center",
              fill: "#f3f4f6",
            },
          },
        }}
        leavesOnly
        colorBy="id"
        isInteractive={true}
        labelsSkipRadius={24}
        animate={true}
      />
    )
  );
});
