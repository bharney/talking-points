"use client";
import * as React from "react";
import { UserContext } from "./user-context";
import Link from "next/link";
import { useEffect, useState } from "react";

export default function UserMenu() {
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Check if we have authentication cookie
    const hasCookie = document.cookie.includes("talking-points=");
    if (hasCookie) setIsLoading(false);
  }, []);

  if (isLoading) {
    return null;
  }

  return (
    <UserContext.Consumer>
      {({ authenticated, forceUpdate }) => (
        <>
          {authenticated ? (
            <Link
              className="dropdown-item"
              href="/logout"
              onClick={() => {
                document.cookie =
                  "talking-points=; path=/; expires=Thu, 01 Jan 1970 00:00:01 GMT;";
                forceUpdate();
              }}
            >
              Logout
            </Link>
          ) : (
            <Link className="dropdown-item" href="/signin">
              Sign-in
            </Link>
          )}
        </>
      )}
    </UserContext.Consumer>
  );
}
