import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* config options here */
};

if (process.env.NODE_ENV === "development") {
  console.log("Rejecting node tls");
  process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
}

export default nextConfig;
