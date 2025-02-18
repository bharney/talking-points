"use client";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faSpinner } from "@fortawesome/free-solid-svg-icons";
import { IconProp } from "@fortawesome/fontawesome-svg-core";

export default function SearchForm() {
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);

  const handleSearch = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    const query = formData.get("searchTerm")?.toString().trim() || "";
    if (!query) return;

    setIsLoading(true);
    try {
      await new Promise((resolve) => setTimeout(resolve, 500));
      await router.push(`/search/${encodeURIComponent(query)}`);
    } catch (error) {
      console.error("Navigation failed:", error);
      setIsLoading(false);
    }
  };

  return (
    <form onSubmit={handleSearch}>
      <div className="input-group input-group-lg">
        <input
          type="search"
          className="form-control"
          placeholder="Search talking points..."
          aria-label="Search"
          name="searchTerm"
          required
          disabled={isLoading}
        />
        <button
          className="btn btn-primary px-4"
          type="submit"
          disabled={isLoading}
        >
          {isLoading ? (
            <FontAwesomeIcon icon={faSpinner as IconProp} spin />
          ) : (
            "Search"
          )}
        </button>
      </div>
    </form>
  );
}
