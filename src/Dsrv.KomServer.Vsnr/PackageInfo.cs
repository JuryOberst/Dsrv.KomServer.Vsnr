using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ExtraStandard;
using ExtraStandard.Extra14;

namespace Dsrv.KomServer.Vsnr
{
    public sealed class PackageInfo
    {
        private readonly RequestClient _client;

        internal PackageInfo(PackageResponseType package, ExtraDataTransformHandler dataTransformHandler, RequestClient client)
        {
            _client = client;
            Flags = package.PackageHeader.GetFlags().ToList();
            ResponseTimestamp = package.PackageHeader.ResponseDetails.TimeStamp;
            ResponseId = package.PackageHeader.ResponseDetails.ResponseID.Value;
            if (!IsError)
            {
                var encoding = ExtraEncodingFactory.Dsrv.GetEncoding("I1");
                var data = ((Base64CharSequenceType) ((DataType1) package.PackageBody.Items[0]).Item).Value;
                foreach (var plugin in package.PackagePlugIns.Any)
                {
                    var dataSource = plugin as DataSourceType;
                    if (dataSource != null)
                    {
                        FileName = dataSource.DataContainer.name;
                        if (dataSource.DataContainer.createdSpecified)
                            FileCreated = dataSource.DataContainer.created;
                        if (!string.IsNullOrEmpty(dataSource.DataContainer.encoding))
                            encoding = ExtraEncodingFactory.Dsrv.GetEncoding(dataSource.DataContainer.encoding);
                    }
                    else
                    {
                        var dataTransforms = plugin as DataTransformsType;
                        if (dataTransforms != null)
                        {
                            data = dataTransformHandler.ReverseTransform(data, dataTransforms);
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                }
                File = encoding.GetString(data, 0, data.Length);
                FileData = data;
            }
        }

        public bool IsError => Flags.Any(x => x.IsError());
        public IReadOnlyCollection<FlagType> Flags { get; }
        public DateTimeOffset? FileCreated { get; }
        public string FileName { get; }
        public string ResponseId { get; }
        public DateTimeOffset ResponseTimestamp { get; }
        public string File { get; }
        public byte[] FileData { get; }

        public VsnrFile Decode()
        {
            return VsnrFile.Load(_client.StreamFactory, new StringReader(File));
        }
    }
}
