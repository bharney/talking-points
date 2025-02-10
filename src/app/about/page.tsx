import * as React from "react";

export default function About() {
  return (
    <div className="p-5 mb-4 rounded-3 text-white">
      <div className="container-fluid py-5">
        <h1 className="display-5 fw-bold">About talking points.</h1>
        <p className="col fs-4">
          We try to understand where talking ponts come from and visual the
          details about what statements came from where. We want to show the
          world how statements proliforate and where they come from.
        </p>
        <p className="col fs-4">
          This site creates visualizations about where topics originate from
          various news sources like NYTimes, AP, Routers, Washington Post, BBC
          News, Wall Street Journal, NPR, and NBC News. We take the data and
          build word trees from them to visualize and link to news articles.
        </p>
        <p className="col fs-4">
          We hope you enjoy the site and find it useful. If you have any
          questions or comments, please feel free to reach out to us at{" "}
          <a href="mailto:talkingpoints.com">talkingpoints.com</a>
        </p>
      </div>
    </div>
  );
}
