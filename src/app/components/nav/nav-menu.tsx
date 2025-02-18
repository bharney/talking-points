import * as React from "react";
import { NavContext } from "./nav-wrapper";
import { useEffect } from "react";
import Link from "next/link";
import UserMenu from "../account/user-menu";

interface NavProps {
  onUpdate: () => void;
  toggle: () => void;
}
export default function NavMenu() {
  const navbarRef = React.useRef<HTMLElement>(null);

  useEffect(() => {
    const handleScroll = () => {
      if (navbarRef.current) {
        const windowsScrollTop = window.pageYOffset;
        if (windowsScrollTop > 50) {
          navbarRef.current.classList.add("affix");
          navbarRef.current.classList.remove("top-nav-collapse");
        } else {
          navbarRef.current.classList.remove("affix");
          navbarRef.current.classList.remove("top-nav-collapse");
        }
      }
    };

    window.addEventListener("scroll", handleScroll);
    return () => window.removeEventListener("scroll", handleScroll);
  }, []);

  return (
    <NavContext.Consumer>
      {({ onUpdate, toggle }: NavProps) => (
        <>
          <nav
            id="custom-nav"
            ref={navbarRef}
            className="navbar navbar-expand-md fixed-top navbar-dark bg-dark"
          >
            <div className="container nav-links">
              <Link className="navbar-brand" onClick={onUpdate} href={"/"}>
                talking-points.com
              </Link>
              <div
                className="collapse navbar-collapse"
                id="navbarsExampleDefault"
              >
                <ul className="navbar-nav me-auto">
                  <li className="nav-item">
                    <Link
                      className={"nav-link root"}
                      href={"/about"}
                      onClick={onUpdate}
                    >
                      About
                    </Link>
                  </li>
                  <li className="nav-item">
                    <Link
                      className={"nav-link root"}
                      href={"/contact"}
                      onClick={onUpdate}
                    >
                      Contact
                    </Link>
                  </li>
                </ul>
              </div>
              <div className="d-inline-flex align-items-center">
                <UserMenu />
                <button
                  className="navbar-toggler navbar-toggler-right ms-2"
                  onClick={toggle}
                  type="button"
                  data-target="#navbarsExampleDefault"
                  aria-controls="navbarsExampleDefault"
                  aria-expanded="false"
                  aria-label="Toggle navigation"
                >
                  <span className="navbar-toggler-icon" />
                </button>
              </div>
            </div>
          </nav>
        </>
      )}
    </NavContext.Consumer>
  );
}
