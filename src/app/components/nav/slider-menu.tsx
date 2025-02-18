import * as React from "react";
import { NavContext } from "./nav-wrapper";
import Link from "next/link";
interface NavProps {
  onUpdate: () => void;
}

export default function SliderMenu() {
  return (
    <NavContext.Consumer>
      {({ onUpdate }: NavProps) => (
        <React.Fragment>
          <Link
            className={"list-group-item"}
            href={"/about"}
            onClick={onUpdate}
          >
            About
          </Link>
          <Link
            className={"list-group-item"}
            href={"/contact"}
            onClick={onUpdate}
          >
            Contact
          </Link>
        </React.Fragment>
      )}
    </NavContext.Consumer>
  );
}
