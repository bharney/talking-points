"use server";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";

export async function handleSignIn(formData: FormData) {
  const email = formData.get("email");
  const password = formData.get("password");

  const response = await fetch("https://localhost:7040/Account/Login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      Email: email,
      Password: password,
      RememberMe: false,
    }),
  });

  if (response.ok) {
    const jwt = await response.json();
    (await cookies()).set("talking-points", jwt.token, { path: "/" });
    redirect("/");
  }
}

export async function handleSignOut() {
  const token = (await cookies()).get("talking-points")?.value ?? "{}";
  fetch("https://localhost:7040/Account/Logout", {
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
