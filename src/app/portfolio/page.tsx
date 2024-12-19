import * as React from "react";
import { faArrowRight } from "@fortawesome/free-solid-svg-icons";
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import Link from "next/link";
import Image from "next/image";

export default function Portfolio() {
  return (
    <div className="album">
      <div className="container">
        <div className="row row-cols-1 row-cols-sm-2 row-cols-md-2 g-3">
          <div className="col">
            <div className="card shadow-sm">
              <Image
                src="/images/ChicagoInABox.png"
                className="img-fluid rounded-top"
                role="img"
                aria-label="Chicago in a box"
                alt="Chicago in a box"
                sizes="100vw"
                style={{
                  width: "100%",
                  height: "auto",
                }}
                width={500}
                height={300}
              ></Image>
              <div className="card-body border rounded-bottom">
                <Link
                  className="icon-link icon-link-hover"
                  target="_blank"
                  href="https://chicagoinabox.com/"
                >
                  <h5 className="card-title">Chicago In A Box</h5>
                  <FontAwesomeIcon
                    icon={faArrowRight as IconProp}
                    className="bi pb-1"
                    transform="shrink-6"
                    pull="left"
                  />
                </Link>
                <p className="card-text">
                  Send Iconic Chicago items to loved ones! Select your Chicago
                  In A Box goodies, then we take care of the rest!
                </p>
              </div>
            </div>
          </div>
          <div className="col">
            <div className="card shadow-sm">
              <Image
                src="/images/ColetteMills.jpg"
                className="img-fluid rounded-top"
                role="img"
                aria-label="Collete Mills"
                alt="Collete Mills"
                sizes="100vw"
                style={{
                  width: "100%",
                  height: "auto",
                }}
                width={500}
                height={300}
              ></Image>
              <div className="card-body">
                <Link
                  className="icon-link icon-link-hover"
                  target="_blank"
                  href="https://colettemills.com/"
                >
                  <h5 className="card-title">Collete Mills</h5>
                  <FontAwesomeIcon
                    icon={faArrowRight as IconProp}
                    className="bi pb-1"
                    transform="shrink-6"
                    pull="left"
                  />
                </Link>
                <p className="card-text">
                  Art Portfolio for Collete Mills. Collete is Link Irish painter
                  who has paintings that have been bought from all over the
                  world.
                </p>
              </div>
            </div>
          </div>
          <div className="col">
            <div className="card shadow-sm">
              <Image
                src="/images/GoSurfer.jpg"
                className="img-fluid rounded-top"
                role="img"
                aria-label="Go Surfer"
                alt="Go Surfer"
                sizes="100vw"
                style={{
                  width: "100%",
                  height: "auto",
                }}
                width={500}
                height={300}
              ></Image>
              <div className="card-body border rounded-bottom">
                <Link
                  className="icon-link icon-link-hover"
                  target="_blank"
                  href="#"
                >
                  <h5 className="card-title">Go Surfer</h5>
                  <FontAwesomeIcon
                    icon={faArrowRight as IconProp}
                    className="bi pb-1"
                    transform="shrink-6"
                    pull="left"
                  />
                </Link>
                <p className="card-text">
                  When the roosters crowin&apos; I start Link scratchin&apos; my
                  head, Gotta flop over get myself outta bed. Grab Link cup o
                  joe and in the car I roll, Y&apos;Link know I want to get
                  movin&apos;, I&apos;m on dawn patrol.
                </p>
              </div>
            </div>
          </div>
          <div className="col">
            <div className="card shadow-sm">
              <Image
                src="/images/HarneyHall.jpg"
                className="img-fluid rounded-top"
                role="img"
                aria-label="Harney Hall Wedding"
                alt="Harney Hall Wedding"
                sizes="100vw"
                style={{
                  width: "100%",
                  height: "auto",
                }}
                width={500}
                height={300}
              ></Image>
              <div className="card-body">
                <Link
                  className="icon-link icon-link-hover"
                  target="_blank"
                  href="https://harneyhall.azurewebsites.net/"
                >
                  <h5 className="card-title">Harney Hall Wedding</h5>
                  <FontAwesomeIcon
                    icon={faArrowRight as IconProp}
                    className="bi pb-1"
                    transform="shrink-6"
                    pull="left"
                  />
                </Link>
                <p className="card-text">
                  I got married! And we needed an online presence, so I built
                  Link Wedding Website for RSVP, Wall Posts, Information and
                  Directions.
                </p>
              </div>
            </div>
          </div>
          <div className="col">
            <div className="card shadow-sm">
              <Image
                src="/images/JMS.jpg"
                className="img-fluid rounded-top"
                role="img"
                aria-label="JMS Auto Repair"
                alt="JMS Auto Repair"
                sizes="100vw"
                style={{
                  width: "100%",
                  height: "auto",
                }}
                width={500}
                height={300}
              ></Image>
              <div className="card-body">
                <Link
                  className="icon-link icon-link-hover"
                  target="_blank"
                  href="https://jmsautorepair.com/"
                >
                  <h5 className="card-title">JMS Auto Repair</h5>
                  <FontAwesomeIcon
                    icon={faArrowRight as IconProp}
                    className="bi pb-1"
                    transform="shrink-6"
                    pull="left"
                  />
                </Link>
                <p className="card-text">
                  At JMS Auto Repair our mission is simple: To provide our
                  customers with the highest qualityservice at the best possible
                  price.
                </p>
              </div>
            </div>
          </div>
          <div className="col">
            <div className="card shadow-sm">
              <Image
                src="/images/PCHFarms.jpg"
                className="img-fluid rounded-top"
                role="img"
                aria-label="PCH Farms Collective"
                alt="PCH Farms Collective"
                sizes="100vw"
                style={{
                  width: "100%",
                  height: "auto",
                }}
                width={500}
                height={300}
              ></Image>
              <div className="card-body">
                <Link
                  className="icon-link icon-link-hover"
                  target="_blank"
                  href="https://pchfarms.azurewebsites.net/"
                >
                  <h5 className="card-title">PCH Farms Collective</h5>
                  <FontAwesomeIcon
                    icon={faArrowRight as IconProp}
                    className="bi pb-1"
                    transform="shrink-6"
                    pull="left"
                  />
                </Link>
                <p className="card-text">
                  Here at PCH Farms, we are Link non-profit medical marijuana
                  marketplace dedicated to connecting medical marijuana patients
                  21 years or older to local marijuana collectives located right
                  here in Santa Cruz.
                </p>
              </div>
            </div>
          </div>
          <div className="col">
            <div className="card shadow-sm">
              <Image
                src="/images/bharneyportfolio.png"
                className="img-fluid rounded-top"
                role="img"
                aria-label="bharney Portfolio"
                alt="bharney Portfolio"
                sizes="100vw"
                style={{
                  width: "100%",
                  height: "auto",
                }}
                width={500}
                height={300}
              ></Image>
              <div className="card-body">
                <Link
                  className="icon-link icon-link-hover"
                  target="_blank"
                  href="https://bharneyportfolio.azurewebsites.net/"
                >
                  <h5 className="card-title">bharney Portfolio</h5>
                  <FontAwesomeIcon
                    icon={faArrowRight as IconProp}
                    className="bi pb-1"
                    transform="shrink-6"
                    pull="left"
                  />
                </Link>
                <p className="card-text">
                  Here in the wilderness theres plenty to see. You just might
                  find a black bear out there. This site has been created by
                  Brian Harney to reflect the types of things he enjoys.
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
