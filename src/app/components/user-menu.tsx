import * as React from "react";
import { UserContext } from "./user-context";
import Link from "next/link";

export default function UserMenu() {
  return (
    <UserContext.Consumer>
      {({ authenticated }) => (
        <>
          {authenticated ? (
            <Link className="dropdown-item" href="/logout">
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
