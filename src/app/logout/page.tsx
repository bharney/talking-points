import * as React from "react";
import { handleSignOut } from "../services/auth-actions";
export default async function Logout() {
  handleSignOut();
  return (
    <div className="container pt-4">
      <div className="row justify-content-center pt-4">
        <div className="col-12 col-sm-8 col-md-6 col-lg-5">
          <h2 className="text-center display-4 text-white">
            You have been logged out.
          </h2>
        </div>
      </div>
    </div>
  );
}
