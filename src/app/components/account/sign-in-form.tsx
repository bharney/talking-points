"use client";
import Link from "next/link";
import { handleSignIn } from "../../common/auth-actions";
import { useContext } from "react";
import { UserContext } from "./user-context";
import { redirect } from "next/navigation";

export default function SignInForm() {
  const { forceUpdate } = useContext(UserContext);

  const handleSubmit = async (formData: FormData) => {
    const success = await handleSignIn(formData);
    if (success) {
      forceUpdate();
      redirect("/");
    }
  };

  return (
    <form action={handleSubmit} method="post" className="form-wrapper">
      <div className="form-label-group">
        <input
          name="email"
          id="loginEmail"
          placeholder="Email"
          required
          className="form-control"
          type="email"
        />
        <label htmlFor="loginEmail">Email</label>
      </div>
      <div className="form-label-group">
        <input
          name="password"
          id="loginPassword"
          placeholder="Password"
          required
          className="form-control"
          type="password"
        />
        <label htmlFor="loginPassword">Password</label>
        <Link href="/forgotPassword" className="pull-right">
          Forgot Password
        </Link>
      </div>
      <button className="btn btn-lg btn-primary btn-block" type="submit">
        Sign-In
      </button>
    </form>
  );
}
