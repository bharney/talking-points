// export async function generateStaticParams() {
//   const posts = await getPostsFromDatabase(); // Function to fetch posts
//   return posts.map((post) => ({
//     slug: post.slug,
//   }));
// }

export default async function Page() {
  return (
    <div className="p-5 mb-4 text-white">
      <h1>Article</h1>
    </div>
  );
}
