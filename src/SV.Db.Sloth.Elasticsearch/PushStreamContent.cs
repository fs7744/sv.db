using System.Net;

namespace SV.Db.Sloth.Elasticsearch;

public class PushStreamContent : HttpContent
{
    private readonly Func<Stream, HttpContent, TransportContext?, Task> func;

    public PushStreamContent(Func<Stream, HttpContent, TransportContext?, Task> func)
    {
        this.func = func;
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return func(stream, this, context);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = -1;
        return false;
    }
}
