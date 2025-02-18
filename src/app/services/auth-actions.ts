"use server";
import { cookies } from "next/headers";

export async function handleSignIn(formData: FormData) {
  const email = formData.get("email");
  const password = formData.get("password");

  try {
    const response = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL}/Account/Login`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          Email: email,
          Password: password,
          RememberMe: false,
        }),
      }
    );

    if (response.ok) {
      const jwt = await response.json();
      (await cookies()).set("talking-points", jwt.token, { path: "/" });
      return true;
    }
    return false;
  } catch (error) {
    console.error("Sign-in error:", error);
    return false;
  }
}

export async function handleSignOut() {
  const token = (await cookies()).get("talking-points")?.value ?? "{}";
  fetch(`${process.env.NEXT_PUBLIC_API_URL}/Account/Logout`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
      Accept: "application/json, text/plain, */*",
    },
    credentials: "include",
  }).catch((error) => {
    console.error("Error:", error);
  });
}
