import * as React from "react";
import { ShowInfo } from "./show-info";
export default function Contact() {
  return (
    <div className="p-5 mb-4 rounded-3 text-white">
      <div className="container-fluid py-5">
        <div className="col text-center">
          <h1 className="display-5 fw-bold">Have questions for me?</h1>
          <p className="fs-3">
            Feel free to reach out using my information below.
          </p>
          <ShowInfo />
        </div>
      </div>
    </div>
  );
}
