"use client";
import React, { useEffect, useState } from "react";
import { jwtDecode } from "jwt-decode";

export const UserContext = React.createContext({
  authenticated: false,
  user: { email: "" },
  forceUpdate: () => {},
});

const getCookies = function (): { [key: string]: string } {
  const pairs = document.cookie.split(";");
  const cookies: { [key: string]: string } = {};
  for (let i = 0; i < pairs.length; i++) {
    const pair = pairs[i].split("=");
    cookies[(pair[0] + "").trim()] = unescape(pair.slice(1).join("="));
  }
  return cookies;
};

function decodeToken(token: string | undefined) {
  try {
    if (!token) return null;
    const decoded = jwtDecode(token);
    return decoded;
  } catch (error) {
    console.error("Error decoding token:", error);
    return null;
  }
}

export default function UserWrapper({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const [authState, setAuthState] = useState({
    authenticated: false,
    user: { email: "" },
  });
  const [cookieUpdateCounter, setCookieUpdateCounter] = useState(0);

  const forceUpdate = () => {
    setCookieUpdateCounter((prev) => prev + 1);
  };

  useEffect(() => {
    try {
      const cookieStore = getCookies();
      const cookie = cookieStore["talking-points"];
      const decodedJwt = decodeToken(cookie);
      const email = decodedJwt?.sub ?? "";
      const authenticated = decodedJwt !== null;

      setAuthState({ authenticated, user: { email } });
    } catch (error) {
      console.error("Error accessing cookies:", error);
    }
  }, [cookieUpdateCounter]); // Re-run when cookieUpdateCounter changes

  return (
    <UserContext.Provider value={{ ...authState, forceUpdate }}>
      {children}
    </UserContext.Provider>
  );
}
