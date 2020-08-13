using System;

namespace Stl.Fusion.Bridge
{
    [Serializable]
    public class PublicationStateInfo
    {
        public PublicationRef PublicationRef { get; set; }
        public LTag Version { get; set; }
        public bool IsConsistent { get; set; }

        public PublicationStateInfo() { }
        public PublicationStateInfo(PublicationRef publicationRef)
            => PublicationRef = publicationRef;
        public PublicationStateInfo(PublicationRef publicationRef, LTag version, bool isConsistent)
        {
            PublicationRef = publicationRef;
            Version = version;
            IsConsistent = isConsistent;
        }
    }

    [Serializable]
    public class PublicationStateInfo<T> : PublicationStateInfo
    {
        public Result<T> Output { get; set; }

        public PublicationStateInfo() { }
        public PublicationStateInfo(PublicationRef publicationRef) : base(publicationRef) { }
        public PublicationStateInfo(PublicationStateInfo stateInfo, Result<T> output = default)
            : this(stateInfo.PublicationRef, stateInfo.Version, stateInfo.IsConsistent, output) { }
        public PublicationStateInfo(PublicationRef publicationRef, LTag version, bool isConsistent, Result<T> output = default)
            : base(publicationRef, version, isConsistent)
            => Output = output;
    }
}
