import * as React from "react";
import { NavContext } from "./nav-wrapper";
import {
  faFacebook,
  faTwitter,
  faLinkedin,
  faGithub,
  faDocker,
  faStackOverflow,
  faPaypal,
  faInstagram,
} from "@fortawesome/fontawesome-free-brands";
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { useEffect } from "react";
import Link from "next/link";

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
                bharney.com
              </Link>
              <div
                className="collapse navbar-collapse"
                id="navbarsExampleDefault"
              >
                <ul className="navbar-nav me-auto">
                  <li className="nav-item">
                    <Link
                      className={"nav-link root"}
                      href={"/portfolio"}
                      onClick={onUpdate}
                    >
                      Portfolio
                    </Link>
                  </li>
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
                <div className="d-none d-md-block d-lg-block d-xl-block">
                  <ul className="navbar-nav">
                    <li className="nav-item">
                      <a
                        className="nav-link root"
                        href="https://www.facebook.com/brian.harney.12"
                        target="_blank"
                      >
                        <FontAwesomeIcon
                          icon={faFacebook as IconProp}
                          transform="grow-6"
                        />
                      </a>
                    </li>
                    <li className="nav-item">
                      <a
                        className="nav-link root"
                        href="https://twitter.com/bharney0"
                        target="_blank"
                      >
                        <FontAwesomeIcon
                          icon={faTwitter as IconProp}
                          transform="grow-6"
                        />
                      </a>
                    </li>
                    <li className="nav-item">
                      <a
                        className="nav-link root"
                        href="https://www.instagram.com/porkchop.12/"
                        target="_blank"
                      >
                        <FontAwesomeIcon
                          icon={faInstagram as IconProp}
                          transform="grow-6"
                        />
                      </a>
                    </li>
                    <li className="nav-item">
                      <a
                        className="nav-link root"
                        href="https://www.linkedin.com/in/bharney0/"
                        target="_blank"
                      >
                        <FontAwesomeIcon
                          icon={faLinkedin as IconProp}
                          transform="grow-6"
                        />
                      </a>
                    </li>
                    <li className="nav-item">
                      <a
                        className="nav-link root"
                        href="https://github.com/bharney"
                        target="_blank"
                      >
                        <FontAwesomeIcon
                          icon={faGithub as IconProp}
                          transform="grow-6"
                        />
                      </a>
                    </li>
                    <li className="nav-item">
                      <a
                        className="nav-link root"
                        href="https://hub.docker.com/u/bharney0"
                        target="_blank"
                      >
                        <FontAwesomeIcon
                          icon={faDocker as IconProp}
                          transform="grow-6"
                        />
                      </a>
                    </li>
                    <li className="nav-item">
                      <a
                        className="nav-link root"
                        href="https://stackoverflow.com/users/4740497/bharney"
                        target="_blank"
                      >
                        <FontAwesomeIcon
                          icon={faStackOverflow as IconProp}
                          transform="grow-6"
                        />
                      </a>
                    </li>
                    <li className="nav-item">
                      <a
                        className="nav-link root"
                        href="https://paypal.me/BrianHarney?locale.x=en_US"
                        target="_blank"
                      >
                        <FontAwesomeIcon
                          icon={faPaypal as IconProp}
                          transform="grow-6"
                        />
                      </a>
                    </li>
                  </ul>
                </div>
              </div>
              <div className="d-inline-flex">
                <div className="d-md-none d-lg-none d-xl-none">
                  <ul className="navbar-nav mobile-nav">
                    <li className="nav-item">
                      <a
                        className="nav-link root"
                        href="https://www.facebook.com/brian.harney.12"
                        target="_blank"
                      >
                        <FontAwesomeIcon
                          icon={faFacebook as IconProp}
                          transform="grow-6"
                        />
                      </a>
                    </li>
                    <li className="nav-item">
                      <a
                        className="nav-link root"
                        href="https://twitter.com/bharney0"
                        target="_blank"
                      >
                        <FontAwesomeIcon
                          icon={faTwitter as IconProp}
                          transform="grow-6"
                        />
                      </a>
                    </li>
                    <li className="nav-item">
                      <a
                        className="nav-link root"
                        href="https://www.instagram.com/porkchop.12/"
                        target="_blank"
                      >
                        <FontAwesomeIcon
                          icon={faInstagram as IconProp}
                          transform="grow-6"
                        />
                      </a>
                    </li>
                    <li className="nav-item">
                      <a
                        className="nav-link root"
                        href="https://www.linkedin.com/in/bharney0/"
                        target="_blank"
                      >
                        <FontAwesomeIcon
                          icon={faLinkedin as IconProp}
                          transform="grow-6"
                        />
                      </a>
                    </li>
                    <li className="nav-item">
                      <a
                        className="nav-link root"
                        href="https://github.com/bharney"
                        target="_blank"
                      >
                        <FontAwesomeIcon
                          icon={faGithub as IconProp}
                          transform="grow-6"
                        />
                      </a>
                    </li>
                  </ul>
                </div>
                <button
                  className="navbar-toggler navbar-toggler-right"
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
