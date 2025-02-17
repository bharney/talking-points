import { CirclePackingChart } from "../components/CirclePacking";
import { SearchForm } from "../components/SearchForm";

export default function Home() {
  return (
    <>
      <main className="container pt-5 pb-2">
        <div className="col">
          <h1 className="hero">Talking points</h1>
          <div className="border hero-border border-light w-25 my-4"></div>
        </div>
        <div className="col">
          <p className="mb-4 hero-text">
            We are looking to find where talking points originate and
            proliforate. We take news articles from various sources and build
            word trees to visualize and link to news articles.
          </p>
          <SearchForm />
        </div>
        <div className="fluid-container">
          <CirclePackingChart />
        </div>
      </main>
    </>
  );
}
