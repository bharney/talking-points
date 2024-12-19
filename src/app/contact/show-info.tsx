// use client side rendering
"use client";
import React from "react";

export const ShowInfo = () => {
  const [showInfo, setShowInfo] = React.useState(false);
  return (
    <>
      <button
        className="btn btn-secondary"
        type="button"
        onClick={() => setShowInfo(!showInfo)}
      >
        {showInfo ? "Hide Info" : "Show Info"}
      </button>
      {showInfo ? (
        <ul className="list-unstyled fs-4">
          <li>
            <a className="text-white" href="mailto:bharney0@gmail.com">
              bharney0@gmail.com
            </a>
          </li>
        </ul>
      ) : (
        ""
      )}
    </>
  );
};
