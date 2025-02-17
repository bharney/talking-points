import * as React from "react";
import Link from "next/link";
import SignInForm from "../components/sign-in-form";
export default function SignIn() {
  return (
    <div className="container pt-4">
      <div className="row justify-content-center pt-4">
        <div className="col-12 col-sm-8 col-md-6 col-lg-5">
          <h2 className="text-center display-4 text-white">Sign-In.</h2>
          <SignInForm />
          <div className="bottom text-center text-white">
            New here? <Link href="/register">Register</Link>
          </div>
        </div>
      </div>
    </div>
  );
}
