"use client";
import { useRouter } from "next/navigation";

export function SearchForm() {
  const router = useRouter();
  // Search form component
  // create a onSubmit function that will redirect to the search page with the search term as a query parameter
  const handleSearch = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    const query = formData.get("searchTerm")?.toString().trim() || "";
    if (!query) return;
    router.push(`/search/${encodeURIComponent(query)}`);
  };
  return (
    <form
      className="mb-4"
      onSubmit={(e) => {
        e.preventDefault();
        handleSearch(e);
      }}
    >
      <div className="input-group">
        <input
          className="form-control"
          type="search"
          placeholder="Search..."
          aria-label="Search"
          name="searchTerm"
        />
        <button className="btn btn-primary" type="submit">
          Search
        </button>
      </div>
    </form>
  );
}
