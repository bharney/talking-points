"use client";
import React, { useEffect, useState, useRef } from "react";
import SliderMenu from "./slider-menu";
import NavMenu from "./nav-menu";

export const NavContext = React.createContext({
  on: false,
  toggle: () => {},
  onUpdate: () => {},
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  handleOverlayToggle: (_e: Event) => {},
});

export default function NavWrapper({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const [state, setState] = useState({ on: false });
  const sidebarRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    sidebarRef.current = document.getElementById("sidebar") as HTMLElement;
    const handleResize = () => {
      if (window.innerWidth > 767) {
        setState({ on: false });
        handleSidebarToggle();
      }
    };
    window.addEventListener("resize", handleResize);
    return () => window.removeEventListener("resize", handleResize);
  }, []);
  const toggle = () => {
    setState({ on: !state.on });
    if (state.on) {
      handleSidebarToggle();
    } else {
      handleSidebarPosition();
    }
  };
  const onUpdate = () => {
    setState({ on: false });
    handleSidebarToggle();
    window.scrollTo(0, 0);
  };
  const handleSidebarPosition = () => {
    const sidebar = sidebarRef.current as HTMLElement;
    const bounding = sidebarRef.current?.getBoundingClientRect();
    const offset = (bounding?.top ?? 0) + document.body.scrollTop;
    let totalOffset = (offset - 100) * -1;
    totalOffset = totalOffset < 0 ? 0 : totalOffset;
    (sidebar as HTMLElement).style.top = totalOffset + "px";
    document.getElementsByTagName("html")[0].style.overflowY = "hidden";
  };
  const handleSidebarToggle = () => {
    const sidebar = sidebarRef.current as HTMLElement;
    if (sidebar) {
      sidebar.removeAttribute("style");
    }
    document.getElementsByTagName("html")[0].style.overflowY = "auto";
  };
  const handleOverlayToggle = (e: Event) => {
    const target = e.target as HTMLElement;
    if (
      target.classList.contains("overlay") ||
      target.classList.contains("subMenu")
    ) {
      setState({ on: false });
      handleSidebarToggle();
    }
  };

  return (
    <NavContext.Provider
      value={{
        on: state.on,
        toggle,
        onUpdate,
        handleOverlayToggle,
      }}
    >
      <NavMenu />
      <div
        id="slider"
        className={`row row-offcanvas row-offcanvas-right ${
          state.on ? "active" : ""
        }`}
      >
        <div className="col-12 col-md-12 col-lg-12">{children}</div>
        <div
          id="sidebar"
          className="col-8 d-md-none d-lg-none d-xl-none sidebar-offcanvas"
        >
          <div className="list-group">
            <SliderMenu />
          </div>
        </div>
      </div>
    </NavContext.Provider>
  );
}
