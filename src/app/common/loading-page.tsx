import { IconProp } from "@fortawesome/fontawesome-svg-core";
import { faSpinner } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import * as React from "react";

export default function LoadingRoute() {
  return (
    <div>
      <FontAwesomeIcon icon={faSpinner as IconProp} spin size="2x" />
    </div>
  );
}
