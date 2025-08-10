import { NextResponse } from "next/server";

export async function GET() {
  const value = process.env.NEXT_PUBLIC_API_URL || null;
  return NextResponse.json({ NEXT_PUBLIC_API_URL: value });
}
