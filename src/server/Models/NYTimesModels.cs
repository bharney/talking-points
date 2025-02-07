
namespace talking_points
{

    public class MediaMetadata
    {
        public string url { get; set; }
        public string format { get; set; }
        public int height { get; set; }
        public int width { get; set; }
    }

    public class Media
    {
        public string type { get; set; }
        public string subtype { get; set; }
        public string caption { get; set; }
        public string copyright { get; set; }
        public int approved_for_syndication { get; set; }
        public List<MediaMetadata> media_metadata { get; set; }
    }

    public class Result
    {
        public string uri { get; set; }
        public string url { get; set; }
        public long id { get; set; }
        public long asset_id { get; set; }
        public string source { get; set; }
        public string published_date { get; set; }
        public string updated { get; set; }
        public string section { get; set; }
        public string subsection { get; set; }
        public string nytdsection { get; set; }
        public string adx_keywords { get; set; }
        public string column { get; set; }
        public string byline { get; set; }
        public string type { get; set; }
        public string title { get; set; }
        public string @abstract { get; set; }
        public List<string> des_facet { get; set; }
        public List<string> org_facet { get; set; }
        public List<string> per_facet { get; set; }
        public List<string> geo_facet { get; set; }
        public List<Media> media { get; set; }
        public int eta_id { get; set; }
    }

    public class Root
    {
        public string status { get; set; }
        public string copyright { get; set; }
        public int num_results { get; set; }
        public List<Result> results { get; set; }
    }

    public class ArticleResponse
    {
       public string status { get; set; }
       public string copyright { get; set; }
       public Response response { get; set; }
    }

    public class Response
    {
        public List<Doc> docs { get; set; }
        public Meta meta { get; set; }
    }

    public class Doc
    {
        public string Abstract { get; set; }
        public string WebUrl { get; set; }
        public string Snippet { get; set; }
        public string lead_paragraph { get; set; }
        public string Source { get; set; }
        public List<Multimedia> Multimedia { get; set; }
        public Headline Headline { get; set; }
        public List<Keyword> Keywords { get; set; }
        public string PubDate { get; set; }
        public string DocumentType { get; set; }
        public string NewsDesk { get; set; }
        public string SectionName { get; set; }
        public string SubsectionName { get; set; }
        public Byline Byline { get; set; }
        public string TypeOfMaterial { get; set; }
        public string Id { get; set; }
        public int WordCount { get; set; }
        public string Uri { get; set; }
    }

    public class Multimedia
    {
        public int Rank { get; set; }
        public string Subtype { get; set; }
        public string Caption { get; set; }
        public string Credit { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public Legacy Legacy { get; set; }
        public string CropName { get; set; }
    }

    public class Legacy
    {
        public string Xlarge { get; set; }
        public int Xlargewidth { get; set; }
        public int Xlargeheight { get; set; }
        public string Thumbnail { get; set; }
        public int Thumbnailwidth { get; set; }
        public int Thumbnailheight { get; set; }
    }

    public class Headline
    {
        public string Main { get; set; }
        public string Kicker { get; set; }
        public string ContentKicker { get; set; }
        public string PrintHeadline { get; set; }
        public string Name { get; set; }
        public string Seo { get; set; }
        public string Sub { get; set; }
    }

    public class Keyword
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public int Rank { get; set; }
        public string Major { get; set; }
    }

    public class Byline
    {
        public string Original { get; set; }
        public List<Person> Person { get; set; }
        public string Organization { get; set; }
    }

    public class Person
    {
        public string Firstname { get; set; }
        public string Middlename { get; set; }
        public string Lastname { get; set; }
        public string Qualifier { get; set; }
        public string Title { get; set; }
        public string Role { get; set; }
        public string Organization { get; set; }
        public int Rank { get; set; }
    }

    public class Meta
    {
        public int Hits { get; set; }
        public int Offset { get; set; }
        public int Time { get; set; }
    }
}
