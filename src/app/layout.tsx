import type { Metadata } from "next";
import {
  Fira_Sans_Condensed,
  Fjalla_One,
  Playfair_Display,
} from "next/font/google";
import "./styles/index.scss";
import layoutStyles from "./page.module.scss";
import Footer from "./common/footer";
import React from "react";
import NavWrapper from "./components/nav/nav-wrapper";
import UserWrapper from "./components/account/user-context";
import { CirclePackingProvider } from "./context/circle-packing-context";
import BootstrapClient from "./components/bootstrap-client";

const firaSansCondensed = Fira_Sans_Condensed({
  variable: "--font-fira-sans-condensed",
  subsets: ["latin"],
  display: "swap",
  weight: "600",
});

const fjallaOne = Fjalla_One({
  variable: "--font-fjalla-one",
  subsets: ["latin"],
  display: "swap",
  weight: "400",
});

const playfairDisplay = Playfair_Display({
  variable: "--font-playfair-display",
  subsets: ["latin"],
  display: "swap",
  weight: "400",
});

export const metadata: Metadata = {
  title: "Create Next App",
  description: "Generated by create next app",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body
        className={`${firaSansCondensed.variable} ${fjallaOne.variable} ${playfairDisplay.variable} ${layoutStyles.bodyClassName} ${layoutStyles.padBody} container`}
      >
        <UserWrapper>
          <NavWrapper>
            <CirclePackingProvider>{children}</CirclePackingProvider>
          </NavWrapper>
        </UserWrapper>
        <Footer />
        <BootstrapClient />
      </body>
    </html>
  );
}
