import Image from "next/image";
import SearchForm from "./components/search-form";
import { CirclePackingChart } from "./components/circle-packing";

export default function Home() {
  return (
    <div className="container my-5">
      <div
        className="row align-items-center rounded-3 border shadow-lg mb-5"
        style={{
          background:
            "linear-gradient(90deg, rgb(33, 37, 41) 0%, rgb(33, 37, 41) 60%, rgba(33, 37, 41, 0.1) 100%)",
        }}
      >
        <div className="col-lg-7 p-5">
          <h1 className="display-3 fw-bold lh-1 text-white">Talking Points</h1>
          <p className="lead display-6 text-white">
            Find out where talking points originate and proliferate. We
            aggregate news articles from diverse sources.
          </p>
          <div className="d-grid gap-2 d-md-flex justify-content-md-start mb-4 mb-lg-3 w-100">
            <div className="w-100">
              <SearchForm />
            </div>
          </div>
        </div>
        <div
          className="col-lg-4 offset-lg-1 p-0 position-relative"
          style={{
            minHeight: "500px",
            position: "relative",
          }}
        >
          <div
            style={{
              position: "absolute",
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              background:
                "linear-gradient(90deg, rgba(33, 37, 41, 0.95) 0%, rgba(33, 37, 41, 0.5) 50%, rgba(33, 37, 41, 0) 100%)",
              zIndex: 1,
              pointerEvents: "none",
            }}
          />
          <Image
            className="rounded-end-3"
            src="/images/hero.jpg"
            alt="Hero image"
            fill
            style={{
              objectFit: "cover",
              zIndex: 0,
            }}
            priority
          />
        </div>
      </div>

      <div className="row mt-5">
        <div className="col-12">
          <div className="bg-dark p-4 rounded-3 border shadow-lg">
            <CirclePackingChart />
          </div>
        </div>
      </div>
    </div>
  );
}
