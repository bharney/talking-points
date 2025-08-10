import { NextResponse } from "next/server";

export async function GET() {
  const baseUrl = process.env.NEXT_PUBLIC_API_URL;
  if (!baseUrl) {
    console.error("circle-packing: NEXT_PUBLIC_API_URL is not set at runtime");
    return NextResponse.json(
      {
        error: "API base URL missing",
        hint: "Set NEXT_PUBLIC_API_URL in your deployment environment",
      },
      { status: 500 }
    );
  }

  const target = `${baseUrl.replace(/\/$/, "")}/Home`;
  try {
    const res = await fetch(target, {
      method: "GET",
      headers: { "Content-Type": "application/json" },
      // You could add next: { revalidate: 300 } here if you want ISR-like caching
    });

    const contentType = res.headers.get("content-type") || "";
    if (!res.ok) {
      let errorBody: object | string | undefined = undefined;
      try {
        if (contentType.includes("application/json"))
          errorBody = await res.json();
        else errorBody = await res.text();
      } catch {
        /* swallow */
      }
      console.error("circle-packing upstream error", {
        status: res.status,
        target,
        errorBody,
      });
      return NextResponse.json(
        {
          error: "Upstream API error",
          status: res.status,
          target,
          upstreamBody: errorBody,
        },
        { status: 500 }
      );
    }

    const data = await res.json();
    return NextResponse.json(data, {
      headers: { "x-circle-packing-upstream": target },
    });
  } catch (error) {
    console.error("circle-packing fetch exception", { target, error });
    return NextResponse.json(
      { error: "Exception contacting upstream", target },
      { status: 500 }
    );
  }
}
