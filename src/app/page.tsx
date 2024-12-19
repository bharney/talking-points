export default function Home() {
  return (
    <>
      <main className="container pt-5 pb-2">
        <div className="col-xxl-10">
          <h1 className="hero">Brian Harney&apos;s Software portfolio</h1>
          <div className="border hero-border border-light w-25 my-4"></div>
        </div>
        <div className="col-lg-10 col-xxl-8">
          <p className="mb-4 hero-text">
            My name is Brian Harney and I love building incredible software.
          </p>
        </div>
        <div className="bg-bloack text-white landing">
          <div className="container">
            <div className="row gy-4">
              <div className="col-md">
                <p>
                  Our increasingly connected world demands services that are
                  resiliant, fast, and always available. We&apos;ve has been
                  raising our expectations about web applications, and
                  rethinking how we share code and ideas. But sometimes it can
                  be difficult to understand all of the new technology because
                  the web has been changing at a mile a minute. I want to show
                  you some of the things I&apos;ve learned in my own experiences
                  building applications. I hope that these writings may help you
                  in your search for an answers/solutions to your own problems.
                </p>
              </div>
            </div>
          </div>
        </div>
      </main>
    </>
  );
}
