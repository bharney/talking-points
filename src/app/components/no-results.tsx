export default function NoResults({ query }: { query: string }) {
  return (
    <div className="col-12">
      <div className="p-5 text-center bg-dark border rounded-3">
        <p className="fs-4 text-light">
          No results found for &quot;{query}&quot;
        </p>
      </div>
    </div>
  );
}
