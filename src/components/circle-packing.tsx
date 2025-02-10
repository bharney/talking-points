"use client";
import { useState } from "react";
import { CirclePacking } from "@nivo/circle-packing";
import { RootData } from "../app/models/models";

export const CirclePackingChart = ({ data }: { data: RootData }) => {
  const commonProperties = {
    width: 900,
    height: 500,
    data,
    padding: 2,
    id: "name",
    value: "loc",
    labelsSkipRadius: 16,
  };
  const [zoomedId, setZoomedId] = useState<string | null>(null);

  return (
    <CirclePacking
      {...commonProperties}
      enableLabels
      labelsSkipRadius={16}
      labelsFilter={(label) => label.node.height === 0}
      labelTextColor={{
        from: "color",
        modifiers: [["darker", 2]],
      }}
      zoomedId={zoomedId}
      motionConfig="slow"
      onClick={(node) => {
        setZoomedId(zoomedId === node.id ? null : node.id);
      }}
    />
  );
};
