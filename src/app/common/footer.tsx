import { IconProp, config } from "@fortawesome/fontawesome-svg-core";
import { faHeart } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import * as React from "react";
config.autoAddCss = false;
export default function Footer() {
  return (
    <footer className="container text-center">
      <hr />
      <div className="row">
        <div className="col">
          <p>
            Made with{" "}
            <FontAwesomeIcon
              icon={faHeart as IconProp}
              size="1x"
              className="svg-inline--fa fa-w-16 fa-lg"
            />{" "}
            by Brian Harney
          </p>
        </div>
      </div>
    </footer>
  );
}
