namespace KnowledgeTrackerDesktop.Core
{
    /// <summary>
    /// The kind of source a knowledge entry came from. The companion
    /// <c>ProjectOriginLocation</c> is interpreted according to this kind:
    /// an absolute folder path for <see cref="LocalFolder"/>, an http(s) URL
    /// for the other two.
    ///
    /// A missing kind (null) means the origin was not recorded. The values
    /// start at 1 so that a null/zero column is never mistaken for a real
    /// kind.
    /// </summary>
    public enum ProjectOriginKind
    {
        /// <summary>A folder on this computer.</summary>
        LocalFolder = 1,

        /// <summary>A source repository (GitHub or another host).</summary>
        GitRepository = 2,

        /// <summary>An example found on a web page.</summary>
        WebPage = 3,
    }
}
