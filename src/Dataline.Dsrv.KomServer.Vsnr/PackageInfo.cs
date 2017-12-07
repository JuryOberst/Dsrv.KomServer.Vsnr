// <copyright file="PackageInfo.cs" company="DATALINE GmbH &amp; Co. KG">
// Copyright (c) DATALINE GmbH &amp; Co. KG. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ExtraStandard;
using ExtraStandard.Extra14;

namespace Dsrv.KomServer.Vsnr
{
    /// <summary>
    /// Informationen über ein zurückgeliefertes eXTra-Paket
    /// </summary>
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
                var data = ((Base64CharSequenceType)((DataType1)package.PackageBody.Items[0]).Item).Value;
                foreach (var plugin in package.PackagePlugIns.Any)
                {
                    switch (plugin)
                    {
                        case DataSourceType dataSource:
                            FileName = dataSource.DataContainer.name;
                            if (dataSource.DataContainer.createdSpecified)
                            {
                                FileCreated = dataSource.DataContainer.created;
                            }

                            if (!string.IsNullOrEmpty(dataSource.DataContainer.encoding))
                            {
                                encoding = ExtraEncodingFactory.Dsrv.GetEncoding(dataSource.DataContainer.encoding);
                            }

                            break;
                        case DataTransformsType dataTransforms:
                            data = dataTransformHandler.ReverseTransform(data, dataTransforms);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }

                File = encoding.GetString(data, 0, data.Length);
                FileData = data;
            }
        }

        /// <summary>
        /// Holt einen Wert, der angibt, ob in das Paket mit Fehlern abgewiesen wurde
        /// </summary>
        public bool IsError => Flags.Any(x => x.IsError());

        /// <summary>
        /// Holt alle Kennzeichen dieses Pakets
        /// </summary>
        public IReadOnlyCollection<FlagType> Flags { get; }

        /// <summary>
        /// Holt den Zeitstempel an dem die Datei erstellt wurde
        /// </summary>
        public DateTimeOffset? FileCreated { get; }

        /// <summary>
        /// Holt den Namen der Datei in diesem Paket
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Holt die Antwort-Kennung über die die Abholung des Pakets quittiert werden kann
        /// </summary>
        public string ResponseId { get; }

        /// <summary>
        /// Holt den Zeitstempel der Rückmeldung
        /// </summary>
        public DateTimeOffset ResponseTimestamp { get; }

        /// <summary>
        /// Holt den Inhalt des Pakets als <see cref="string"/>
        /// </summary>
        public string File { get; }

        /// <summary>
        /// Holt den Inhalt des Pakets als <see cref="byte"/>-Array
        /// </summary>
        public byte[] FileData { get; }

        /// <summary>
        /// Dekodiert den Wert in <see cref="File"/> und liefert ein <see cref="VsnrFile"/> zurück
        /// </summary>
        /// <returns>Das <see cref="VsnrFile"/>, das anhand von <see cref="File"/> erstellt wurde</returns>
        public VsnrFile Decode()
        {
            return VsnrFile.Load(_client.StreamFactory, new StringReader(File));
        }
    }
}
